using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace UETools.VisualStudio
{
    public sealed class CommandAction : OleMenuCommand
    {
        private Action OnInvoke { get; set; }
        private Action<CommandAction> OnQueryStatus { get; set; }

        private CommandAction(AsyncPackage package, OleMenuCommandService commandService, CommandID commandID, Action onInvoke, Action<CommandAction> onQueryStatus)
            : base(OnInvoked, null, OnBeforeQueryStatus, commandID)
        {
            OnInvoke = onInvoke;
            OnQueryStatus = onQueryStatus;
        }
        private CommandAction(AsyncPackage package, OleMenuCommandService commandService, CommandID commandID, Action onInvoke)
            : base(OnInvoked, commandID)
        {
            OnInvoke = onInvoke;
        }

        public static async Task InitializeAsync(AsyncPackage package, Guid commandSet, int commandID, Action onInvoke, Action<CommandAction> onQueryStatus)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            if (commandService != null)
            {
                commandService.AddCommand(new CommandAction(package, commandService, new CommandID(commandSet, commandID), onInvoke, onQueryStatus));
            }
        }

        public static async Task InitializeAsync(AsyncPackage package, Guid commandSet, int commandID, Action onInvoke)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            if (commandService != null)
            {
                commandService.AddCommand(new CommandAction(package, commandService, new CommandID(commandSet, commandID), onInvoke));
            }
        }

        private static void OnInvoked(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                CommandAction ca = (CommandAction)sender;
                ca.OnInvoke();
            }
            catch
            {
            }
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                CommandAction ca = (CommandAction)sender;
                ca.OnQueryStatus(ca);
            }
            catch
            {
            }
        }
    }
}
