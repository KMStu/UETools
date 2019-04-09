using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace UETools.Perforce
{
    public class P4DiffAgainstCommand : OleMenuCommand
    {
        private List<string> P4Paths;
        private int BaseCommandID;

        public P4DiffAgainstCommand(CommandID rootID)
            : base(OnInvoked, OnChanged, OnBeforeQueryStatus, rootID)
        {
            BaseCommandID = rootID.ID;
            MatchedCommandId = rootID.ID;

            P4Paths = new List<string>();
            P4Paths.Add(@"\\UE4\Dev-Destruction");
            P4Paths.Add(@"\\UE4\Dev-Niagara");
            P4Paths.Add(@"\\UE4\Dev-Rendering");
            P4Paths.Sort();
        }

        public static async Task InitializeAsync(AsyncPackage package, Guid commandSet, int commandID)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            if (commandService == null)
                return;

            commandService.AddCommand(new P4DiffAgainstCommand(new CommandID(commandSet, commandID)));
        }

        public override bool DynamicItemMatch(int commandID)
        {
            if (!IsValidDynamicItem(commandID))
                return false;

            MatchedCommandId = commandID;

            return true;
        }

        private bool IsValidDynamicItem(int commandID)
        {
            return (commandID >= BaseCommandID) && (commandID < BaseCommandID + P4Paths.Count);
        }

        private static void OnInvoked(object sender, EventArgs args)
        {
        }

        private static void OnChanged(object sender, EventArgs args)
        {
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs args)
        {
            P4DiffAgainstCommand matchedCommand = (P4DiffAgainstCommand)sender;
            bool bIsRootItem = matchedCommand.MatchedCommandId == 0;
            if ( !bIsRootItem )
            {
                matchedCommand.Enabled = true;
                matchedCommand.Visible = true;
                matchedCommand.Text = matchedCommand.P4Paths[matchedCommand.MatchedCommandId - matchedCommand.BaseCommandID];
            }
        }
    }
}
