using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Task = System.Threading.Tasks.Task;

namespace UETools.VisualStudio
{
    public class VSTitleRename
    {
        private VSTitleRename()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            Dte = (EnvDTE80.DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));

            Dte.Events.DebuggerEvents.OnEnterBreakMode += (dbgEventReason reason, ref dbgExecutionAction executionAction) => UpdateWindowTitle();
            Dte.Events.DebuggerEvents.OnEnterRunMode += (a) => UpdateWindowTitle();
            Dte.Events.DebuggerEvents.OnEnterDesignMode += (a) => UpdateWindowTitle();
            Dte.Events.DebuggerEvents.OnContextChanged += (a, b, c, d) => UpdateWindowTitle();
            Dte.Events.DocumentEvents.DocumentOpened += (a) => UpdateWindowTitle();
            Dte.Events.DocumentEvents.DocumentClosing += (a) => UpdateWindowTitle();
            Dte.Events.SolutionEvents.AfterClosing += () => UpdateWindowTitle();
            Dte.Events.SolutionEvents.Opened += () => UpdateWindowTitle();
            Dte.Events.SolutionEvents.Renamed += (a) => UpdateWindowTitle();
            Dte.Events.WindowEvents.WindowCreated += (a) => UpdateWindowTitle();
            Dte.Events.WindowEvents.WindowClosing += (a) => UpdateWindowTitle();
            Dte.Events.WindowEvents.WindowActivated += (a, b) => UpdateWindowTitle();

            //this.ResetTitleTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            //this.ResetTitleTimer.Tick += this.UpdateWindowTitleAsync;
            //this.ResetTitleTimer.Start();
        }

        private EnvDTE80.DTE2 Dte { get; set; }

        private static VSTitleRename Instance { get; set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            Instance = new VSTitleRename();
        }


        private void UpdateWindowTitle()
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (Dte.MainWindow != null)
                {
                    string solutionPath = Dte.Solution.FileName;
                    string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
                    string solutionDir = Path.GetDirectoryName(solutionPath);

                    string caption = Dte.MainWindow.Caption;
                    if ( caption.StartsWith(solutionName) )
                    {
                        string newTitle = "*Testing*" + caption.Substring(solutionName.Length);

                        System.Windows.Application.Current.MainWindow.Title = newTitle;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
