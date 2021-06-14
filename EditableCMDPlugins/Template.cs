using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using uk.JohnCook.dotnet.EditableCMDLibrary.Commands;
using uk.JohnCook.dotnet.EditableCMDLibrary.ConsoleSessions;
using uk.JohnCook.dotnet.EditableCMDLibrary.InputProcessing;
using uk.JohnCook.dotnet.EditableCMDLibrary.Interop;
using uk.JohnCook.dotnet.EditableCMDLibrary.Logging;
using uk.JohnCook.dotnet.EditableCMDLibrary.Utils;

/// <summary>
/// EditableCMD Plugin EditableCMDPlugins
/// </summary>
namespace EditableCMDPlugins
{
    /// <summary>
    /// Class for handling Command Template
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class Template : ICommandInput
    {
        #region Plugin Implementation Details
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "Template";
        /// <summary>
        /// Summary of the plugin's functionality.
        /// </summary>
        /// TODO: Change plugin description
        public string Description => "Handles command TEMPLATE, doing nothing.";
        /// <summary>
        /// Author's name (can be <see cref="string.Empty"/>)
        /// </summary>
        /// TODO: Change plugin creator's name
        public string AuthorName => "John";
        /// <summary>
        /// Author's Twitch username (can be <see cref="string.Empty"/>)
        /// </summary>
        /// TODO: Set plugin creator's Twitch username
        public string AuthorTwitchUsername => string.Empty;
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
        /// TODO: Change command(s) the plugin handles
        public string[] CommandsHandled => new string[] { "template" };
        #endregion

        private string regexCommandString = string.Empty;

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

            // TODO: Change plugin functionality

            // Do nothing
            return;
        }
    }
}
