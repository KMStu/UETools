using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace UETools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(UEToolsPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    public sealed class UEToolsPackage : AsyncPackage
    {
        public const string PackageGuidString = "44838C29-D7C6-415E-A60D-E48E7D9102F4";

        public UEToolsPackage()
        {
        }

        #region Package Members


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\VisualStudio\" + (Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2).Version + @"\General");
            //key.SetValue("EnableVSIPLogging", 1);

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandSetGuid = new Guid("9002F6D2-F954-4B61-A6E7-273609026867");
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0100, (o, e) => Helper.P4Helper.ExecuteP4Command("edit {0}", Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0101, (o, e) => Helper.P4Helper.ExecuteP4Command("revert {0}", Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0102, (o, e) => Helper.P4Helper.ExecuteP4VCommand("history {0}", Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0103, (o, e) => Helper.P4Helper.ExecuteP4VCommand("timelapse {0}", Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0104, (o, e) => Helper.P4Helper.ExecuteP4VCommand("prevdiff {0}", Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0105, (o, e) => Helper.P4Helper.ExecuteP4VCCommand("revisiongraph {0}", Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0106, (o, e) => Helper.P4Helper.CopyP4PathToClipboard(Helper.VSHelper.GetOpenDocumentName()));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0107, (o, e) => Helper.P4Helper.OpenP4VAt(Helper.VSHelper.GetOpenDocumentName()));

            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0150, (o, e) => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4Command("edit {0}", filename)));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0151, (o, e) => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4Command("revert {0}", filename)));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0152, (o, e) => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4VCommand("prevdiff {0}", filename)));

            await Perforce.P4DiffAgainstCommand.InitializeAsync(this, commandSetGuid, 0x0201);

            // Add handlers so that we can rename the toolbar
            //var dte = (EnvDTE80.DTE2)GetGlobalService(typeof(EnvDTE.DTE));
            //dte.Events.SolutionEvents.AfterClosing += OnIdeSolutionEvent;
            //dte.Events.SolutionEvents.Opened += OnIdeSolutionEvent;
            //dte.Events.SolutionEvents.Renamed += OnIdeSolutionEvent;
        }

        private void OnIdeSolutionEvent(string oldname)
        {
            UpdateWindowTitle();
        }

        private void OnIdeSolutionEvent()
        {
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            var dte = (EnvDTE80.DTE2)GetGlobalService(typeof(EnvDTE.DTE));
            if (dte.MainWindow != null)
            {
                //System.Windows.Application.Current.MainWindow.Title = "Testing!";
                //System.Windows.Forms.Application.
            }
        }

        #endregion
    }
}
