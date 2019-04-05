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
    public sealed class CommandAction
    {
        Action<object, EventArgs> ActionToPerform { get; set; }

        private CommandAction(AsyncPackage package, OleMenuCommandService commandService, Guid commandSet, int commandID, Action<object, EventArgs> action)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            ActionToPerform = action;

            var menuCommandID = new CommandID(commandSet, commandID);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package, Guid commandSet, int commandID, Action<object, EventArgs> action)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            new CommandAction(package, commandService, commandSet, commandID, action);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                ActionToPerform(sender, e);
            }
            catch
            {
            }
        }
    }
}
