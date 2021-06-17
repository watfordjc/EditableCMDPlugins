using System;
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
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "RainbowTree";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        public string Description => "Handles command RTREE, a rainbow themed TREE command with artificial lag.";
        /// <summary>
        /// Author's name (can be <see cref="string.Empty"/>)
        /// </summary>
        public string AuthorName => "John Cook";
        /// <summary>
        /// Author's Twitch username (can be <see cref="string.Empty"/>)
        /// </summary>
        public string AuthorTwitchUsername => "WatfordJC";
        /// <summary>
        /// An array of the keys handled by the plugin. For commands, this should be <see cref="ConsoleKey.Enter"/>.
        /// </summary>
        public ConsoleKey[] KeysHandled => new ConsoleKey[] { ConsoleKey.Enter };
        /// <summary>
        /// Whether the plugin handles keys/commands input in normal mode (such as a command entered at the prompt).
        /// </summary>
        public bool NormalModeHandled => true;
        /// <summary>
        /// Whether the plugin handles keys input in edit mode.
        /// </summary>
        public bool EditModeHandled => false;
        /// <summary>
        /// Whether the plugin handles keys input in mark mode.
        /// </summary>
        public bool MarkModeHandled => false;
        /// <summary>
        /// An array of commands handled by the plugin, in lowercase.
        /// </summary>
        public string[] CommandsHandled => new string[] { "rtree" };
        #endregion

        private string regexCommandString = string.Empty;
        private ConsoleState state;
        private volatile CancellationTokenSource cancellationTokenSource;
        private volatile bool stopRunning = false;
        private ConsoleCancelEventHandler cancelEventHandler = null;
        private ConsoleColor originalForeground;
        private ConsoleColor originalBackground;
        private CommandPrompt commandPrompt;
        private Encoding previousEncoding;

        /// <summary>
        /// Called when adding an implementation of the interface to the list of event handlers. Approximately equivalent to a constructor.
        /// </summary>
        /// <param name="state">The <see cref="ConsoleState"/> for the current console session.</param>
        public void Init(ConsoleState state)
        {
            // Add all commands listed in CommandsHandled to the regex string for matching if this plugin handles the command.
            if (KeysHandled.Contains(ConsoleKey.Enter) && CommandsHandled.Length > 0)
            {
                regexCommandString = string.Concat("^(", string.Join('|', CommandsHandled), ")( .*)?$");
            }
            this.state = state;
        }

        /// <summary>
        /// Event handler for a <see cref="KeyPress"/>, or a command <c> if (<paramref name="e"/>.Key == <see cref="ConsoleKey.Enter"/>)</c>.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">The ConsoleKeyEventArgs for the event</param>
        public void ProcessCommand(object sender, NativeMethods.ConsoleKeyEventArgs e)
        {
            // Return early if we're not interested in the event
            if (e.Handled || // Event has already been handled
                !e.Key.KeyDown || // A key was not pressed
                !KeysHandled.Contains(e.Key.ConsoleKey) || // The key pressed wasn't one we handle
                e.State.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // We're handling the event if these conditions are met
            else if (!string.IsNullOrEmpty(regexCommandString) && Regex.Match(e.State.Input.Text.ToString().Trim(), regexCommandString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            // Split the command from any parameters such as TREE's /A, /F, and /?
            string[] treeWithParams = e.State.Input.Text.ToString().Split(' ', 2);

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
            e.State.OutputEncoding = Encoding.GetEncoding(858);
            // Create a CommandPrompt instance which sets up a thread using the parameters supplied
            commandPrompt = new(e.State, string.Format("tree{0}", treeWithParams.Length == 1 ? "" : string.Concat(" ", treeWithParams[1])), false, false, true, false);
            // Restore output encoding
            e.State.OutputEncoding = previousEncoding;
            #endregion

            // Store the current console colours
            originalForeground = Console.ForegroundColor;
            originalBackground = Console.BackgroundColor;

            #region Colour changing timer
            // Create timer for changing output colour - do NOT set interval too low
            bool changeColor = false;
            using System.Timers.Timer timer = new(3000);
            void onElapsed(object sender, System.Timers.ElapsedEventArgs e)
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
            void onStart(object sender, bool started)
            {
                // Start colour changing timer
                timer.Start();
            }

            // Local method that will get called whenever CommandPrompt receives a line of output
            [MethodImpl(MethodImplOptions.Synchronized)]
            void onNewOutput(object sender, int newLineCount)
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
            void onComplete(object sender, bool completed)
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
            ConsoleOutput.WritePrompt(e.State, true);
        }

        /// <summary>
        /// Handle console cancel events (CTRL+C, CTRL+Break)
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">arguments</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void ConsoleCancelEventHandler(object sender, ConsoleCancelEventArgs e)
        {
            // Stop the output dequeueing
            stopRunning = true;
            cancellationTokenSource.Cancel();
            // Stop (abort) the CommandPrompt thread/process
            commandPrompt.Stop();
            // Let Ctrl+C bubble up - a CommandPrompt
            e.Cancel = false;
        }
    }
}
