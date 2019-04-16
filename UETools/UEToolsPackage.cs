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
    [ProvideOptionPage(typeof(Options.OptionsPage), "UE Tools Options", "General", 101, 106, true)]
    [Guid(UEToolsPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    public sealed class UEToolsPackage : AsyncPackage
    {
        public const string PackageGuidString = "BA13DE52-3F65-4B6C-BC96-DDAB9C99FF88";

        public UEToolsPackage()
        {
            Options.Options.Instantiate(this);
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

            // Perforce Commands
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0100, () => Task.Run(async () => Helper.P4Helper.AddOrEditAsync(await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0101, () => Task.Run(async () => Helper.P4Helper.ExecuteP4CommandAsync("revert {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0102, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCommandAsync("history {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0103, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCommandAsync("timelapse {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0104, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCommandAsync("prevdiff {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0105, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCCommandAsync("revisiongraph {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0106, () => Task.Run(async () => Helper.P4Helper.CopyP4PathToClipboardAsync(await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0107, () => Task.Run(async () => Helper.P4Helper.OpenP4VAtAsync(await Helper.VSHelper.GetOpenDocumentNameAsync())));

            //await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0150, () => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4CommandAsync("edit {0}", filename)));
            //await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0151, () => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4CommandAsync("revert {0}", filename)));
            //await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0152, () => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4VCommandAsync("prevdiff {0}", filename)));

            await Perforce.P4DiffAgainstCommand.InitializeAsync(this, commandSetGuid, 0x0201);

            // Unreal Commands
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0300, () => Unreal.UnrealCommands.LaunchEditor(), (sender) => sender.Enabled = Helper.VSHelper.IsSolutionLoaded());
            await VisualStudio.CommandAction.InitializeAsync(this, commandSetGuid, 0x0301, () => Unreal.UnrealCommands.LaunchUnrealFrontend(), (sender) => sender.Enabled = Helper.VSHelper.IsSolutionLoaded());

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
