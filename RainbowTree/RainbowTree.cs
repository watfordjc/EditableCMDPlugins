using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

/// <summary>
/// EditableCMD Plugin RainbowTree
/// </summary>
namespace RainbowTree
{
    /// <summary>
    /// Class for handling Command RTree
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class RainbowTree : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "RainbowTree";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles command RTREE, a rainbow themed TREE command with artificial lag.";
        /// <inheritdoc cref="ICommandInput.AuthorName"/>
        public string AuthorName => "John Cook";
        /// <inheritdoc cref="ICommandInput.AuthorTwitchUsername"/>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <inheritdoc cref="ICommandInput.KeysHandled"/>
        public ConsoleKey[]? KeysHandled => new ConsoleKey[] { ConsoleKey.Enter };
        /// <inheritdoc cref="ICommandInput.NormalModeHandled"/>
        public bool NormalModeHandled => true;
        /// <inheritdoc cref="ICommandInput.EditModeHandled"/>
        public bool EditModeHandled => false;
        /// <inheritdoc cref="ICommandInput.MarkModeHandled"/>
        public bool MarkModeHandled => false;
        /// <inheritdoc cref="ICommandInput.CommandsHandled"/>
        public string[]? CommandsHandled => new string[] { "rtree" };
        #endregion

        private string regexCommandString = string.Empty;
        private ConsoleState? state;
        private volatile CancellationTokenSource? cancellationTokenSource;
        private volatile bool stopRunning = false;
        private ConsoleCancelEventHandler? cancelEventHandler = null;
        private ConsoleColor originalForeground;
        private ConsoleColor originalBackground;
        private CommandPrompt? commandPrompt;
        private Encoding? previousEncoding;

        /// <inheritdoc cref="ICommandInput.Init(ConsoleState)"/>
        [MemberNotNull(nameof(state))]
        public void Init(ConsoleState state)
        {
            // Add all commands listed in CommandsHandled to the regex string for matching if this plugin handles the command.
            if (KeysHandled?.Contains(ConsoleKey.Enter) == true && CommandsHandled?.Length > 0)
            {
                regexCommandString = string.Concat("^(", string.Join('|', CommandsHandled), ")$");
            }
            this.state = state;
        }

        /// <summary>
        /// Event handler for the RTREE command.
        /// </summary>
        /// <inheritdoc cref="ICommandInput.ProcessCommand(object?, NativeMethods.ConsoleKeyEventArgs)" path="param"/>
        public void ProcessCommand(object? sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Call Init() again if state isn't set
            if (state == null)
            {
                Init(e.State);
            }
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                KeysHandled?.Contains(e.Key.ConsoleKey) != true || // The key pressed wasn't one we handle
                state.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // We're handling the event if these conditions are met
            else if (!string.IsNullOrEmpty(regexCommandString) && Regex.Match(state.Input.Text.ToString().Trim(), regexCommandString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // Split the command from any parameters such as TREE's /A, /F, and /?
            string[] treeWithParams = state.Input.Text.ToString().Split(' ', 2);

            // Add event handler for Ctrl+C and Ctrl+Break, reset stopRunning to false
            cancelEventHandler = new ConsoleCancelEventHandler(ConsoleCancelEventHandler);
            Console.CancelKeyPress += cancelEventHandler;
            cancellationTokenSource = new();
            stopRunning = false;

            // Random instance for creating integers for random delays
            Random random = new();

            #region Create CommandPrompt instance with suitable output encoding for tree command
            // Store the current output encoding
            previousEncoding = Console.OutputEncoding;
            // Set encoding for sub-process to code page 858 (code page 850 + Euro symbol)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            state.OutputEncoding = Encoding.GetEncoding(858);
            // Create a CommandPrompt instance which sets up a thread using the parameters supplied
            commandPrompt = new(state, string.Format("tree{0}", treeWithParams.Length == 1 ? "" : string.Concat(" ", treeWithParams[1])), false, false, true, false);
            // Restore output encoding
            state.OutputEncoding = previousEncoding;
            #endregion

            // Store the current console colours
            originalForeground = Console.ForegroundColor;
            originalBackground = Console.BackgroundColor;

            #region Colour changing timer
            // Create timer for changing output colour - do NOT set interval too low
            bool changeColor = false;
            using System.Timers.Timer timer = new(3000);
            void onElapsed(object? sender, System.Timers.ElapsedEventArgs e)
            {
                changeColor = true;
            }
            timer.Elapsed += onElapsed;
            #endregion

            // Change color on first line of output
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;
            // Print newline after command being entered
            Console.WriteLine();

            #region Local methods for CommandPrompt events

            // Local function called when the process/thread is started
            void onStart(object? sender, bool started)
            {
                // Start colour changing timer
                timer.Start();
            }

            // Local method that will get called whenever CommandPrompt receives a line of output
            [MethodImpl(MethodImplOptions.Synchronized)]
            void onNewOutput(object? sender, int newLineCount)
            {
                if (stopRunning)
                {
                    return;
                }
                // The newline must be written after a colour change (if applicable) so the colour changes don't span two lines
                Console.Write(commandPrompt.Output.Dequeue());
                // If the timer fired, cycle to the next colour on the next line of output
                if (changeColor)
                {
                    switch (Console.BackgroundColor)
                    {
                        case ConsoleColor.DarkRed:
                            Console.BackgroundColor = ConsoleColor.DarkYellow;
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case ConsoleColor.DarkYellow:
                            Console.BackgroundColor = ConsoleColor.Yellow;
                            Console.ForegroundColor = ConsoleColor.Black;
                            break;
                        case ConsoleColor.Yellow:
                            Console.BackgroundColor = ConsoleColor.DarkGreen;
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case ConsoleColor.DarkGreen:
                            Console.BackgroundColor = ConsoleColor.DarkBlue;
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case ConsoleColor.DarkBlue:
                            Console.BackgroundColor = ConsoleColor.DarkMagenta;
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        default:
                            Console.BackgroundColor = ConsoleColor.DarkRed;
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }
                    // Do NOT change colour on every line
                    changeColor = false;
                }
                // If the newline appears before a colour change, the next line will be: [next line string in new colour][remainder of buffer width in previous colour][\n]
                Console.WriteLine();

                // Add a random millisecond delay between lines - 90% chance there is no delay
                if (random.Next(0, 999) < 100)
                {
                    // Random delay between 1*5 (5 ms) and 19*5 (95 ms)
                    cancellationTokenSource.Token.WaitHandle.WaitOne(random.Next(1, 19) * 5);
                }
            }

            // Local method that will get called when CommandPrompt's thread has finished executing
            void onComplete(object? sender, bool completed)
            {
                commandPrompt.Started -= onStart;
                commandPrompt.NewOutput -= onNewOutput;
                commandPrompt.Completed -= onComplete;
                Console.CancelKeyPress -= cancelEventHandler;
                cancelEventHandler = null;
                timer.Stop();
                return;
            }

            #endregion

            // Add the above local methods as event handlers to the CommandPrompt instance
            commandPrompt.Started += onStart;
            commandPrompt.NewOutput += onNewOutput;
            commandPrompt.Completed += onComplete;
            // Start the CommandPrompt thread and block until it exits
            commandPrompt.Start();
            commandPrompt.WaitForExit();
            // If applicable, yield to Ctrl+C bubbling up to ConsoleState and setting CmdRunning=false:
            while (state.CmdRunning)
            {
                if (!Thread.Yield())
                {
                    Thread.Sleep(1);
                    break;
                }
                Thread.Sleep(1);
            }
            // Reset console colours, and print a newline
            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
            Console.WriteLine();
            // If the cursor isn't where it should be, ^C might be on our new line. If so, print another newline.
            if (Console.CursorLeft != 0)
            {
                Console.WriteLine();
            }
            // Print a new prompt for input
            ConsoleOutput.WritePrompt(state, true);
        }

        /// <summary>
        /// Handle console cancel events (CTRL+C, CTRL+Break)
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">arguments</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void ConsoleCancelEventHandler(object? sender, ConsoleCancelEventArgs e)
        {
            // Stop the output dequeueing
            stopRunning = true;
            cancellationTokenSource?.Cancel();
            // Stop (abort) the CommandPrompt thread/process
            commandPrompt?.Stop();
            // TODO: Determine if Thread.Yield
            // Let Ctrl+C bubble up
            //  - CommandPrompt instances set CmdRunning=false if they complete execution.
            //  - ConsoleOutput.WritePrompt sets CmdRunning=false after printing the new prompt.
            e.Cancel = false;
        }
    }
}
