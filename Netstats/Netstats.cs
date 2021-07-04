using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;

namespace uk.JohnCook.dotnet.EditableCMD.Plugins
{
    /// <summary>
    /// NETSTATS command
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Netstats : ICommandInput
    {
        #region Plugin Implementation Details
        /// <inheritdoc cref="ICommandInput.Name"/>
        public string Name => "Netstats";
        /// <inheritdoc cref="ICommandInput.Description"/>
        public string Description => "Handles command NETSTATS - prints something network-related.";
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
        public string[]? CommandsHandled => new string[] { "netstats" };
        #endregion

        private string regexCommandString = string.Empty;
        private ConsoleState? state;

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
        /// Event handler for the NETSTATS command.
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
                !(e.Key.ConsoleKey == ConsoleKey.Enter) || // The key pressed was not Enter
                state.EditMode // Edit mode is enabled
                )
            {
                return;
            }
            // If current input matches NETSTATS, we are handling the event
            else if (!string.IsNullOrEmpty(regexCommandString) && Regex.Match(state.Input.Text.ToString().Trim(), regexCommandString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Success)
            {
                e.Handled = true;
            }
            // In all other cases we are not handling the event
            else
            {
                return;
            }

            Console.WriteLine("\niSpy on my Wi-Fi something beginning with iP.\n");
            ConsoleOutput.WritePrompt(state, false);
        }
    }
}
