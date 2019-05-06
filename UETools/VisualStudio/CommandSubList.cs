using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace UETools.VisualStudio
{
    public struct CommandDetails
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public bool Checked { get; set; }
    }

    public class CommandSubList : OleMenuCommand
    {
        private int BaseCommandID;

        private Func<CommandDetails[]> GetCommandList;
        private Func<CommandDetails, Task> ExecuteCommand;

        private CommandDetails[] CachedCommandDetails;

        public CommandSubList(CommandID rootID)
            : base(OnInvoked, OnChanged, OnBeforeQueryStatus, rootID)
        {
            BaseCommandID = rootID.ID;
            MatchedCommandId = rootID.ID;
        }

        public static async Task InitializeAsync(AsyncPackage package, Guid commandSet, int commandID, Func<CommandDetails[]> GetCommandList, Func<CommandDetails, Task> ExecuteCommand)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            if (commandService == null)
                return;

            var command = new CommandSubList(new CommandID(commandSet, commandID));
            command.GetCommandList = GetCommandList;
            command.ExecuteCommand = ExecuteCommand;
            commandService.AddCommand(command);
        }

        public override bool DynamicItemMatch(int commandID)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (!IsValidDynamicItem(commandID))
            {
                MatchedCommandId = 0;
                return false;
            }

            MatchedCommandId = commandID;

            return true;
        }

        private bool IsValidDynamicItem(int commandID)
        {
            return (commandID >= BaseCommandID) && (commandID < BaseCommandID + CachedCommandDetails.Length);
        }

        private static void OnInvoked(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            CommandSubList matchedCommand = (CommandSubList)sender;

            bool isRootItem = matchedCommand.MatchedCommandId == 0;
            int itemIndex = isRootItem ? 0 : matchedCommand.MatchedCommandId - matchedCommand.BaseCommandID;

            if ((itemIndex >= 0) && (itemIndex < matchedCommand.CachedCommandDetails.Length))
            {
                _ = Task.Run(
                    async () =>
                    {
                        await matchedCommand.ExecuteCommand(matchedCommand.CachedCommandDetails[itemIndex]);
                    }
                );
            }
        }

        private static void OnChanged(object sender, EventArgs args)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs args)
        {
            CommandSubList matchedCommand = (CommandSubList)sender;

            bool isRootItem = matchedCommand.MatchedCommandId == 0;
            int itemIndex = isRootItem ? 0 : matchedCommand.MatchedCommandId - matchedCommand.BaseCommandID;

            if (itemIndex == 0)
            {
                matchedCommand.CachedCommandDetails = matchedCommand.GetCommandList();
            }

            if ((itemIndex >= 0) && (itemIndex < matchedCommand.CachedCommandDetails.Length))
            {
                matchedCommand.Enabled = true & matchedCommand.CachedCommandDetails[itemIndex].Enabled;
                matchedCommand.Text = matchedCommand.CachedCommandDetails[itemIndex].Name;
                matchedCommand.Checked = matchedCommand.CachedCommandDetails[itemIndex].Checked;
            }
            else
            {
                matchedCommand.Enabled = false;
                matchedCommand.Text = "No items";
            }

            matchedCommand.MatchedCommandId = 0;
        }
    }
}
