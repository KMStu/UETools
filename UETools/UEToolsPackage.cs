using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

// Have a look at https://github.com/mike-ward/VSColorOutput/ to perhaps support UE specific coloring
// Have a look at https://github.com/mayerwin/vs-customize-window-title for window renaming

namespace UETools
{
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
        public static Guid CommandSetGuid = new Guid("9002F6D2-F954-4B61-A6E7-273609026867");

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

            // Create event handler
            VisualStudio.VSEvents.InstantiateSingleton();

            // Perforce Commands
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0100, () => Task.Run(async () => Helper.P4Helper.AddOrEditAsync(await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0101, () => Task.Run(async () => Helper.P4Helper.ExecuteP4CommandAsync("revert {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0102, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCommandAsync("history {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0103, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCommandAsync("timelapse {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0104, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCommandAsync("prevdiff {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0105, () => Task.Run(async () => Helper.P4Helper.ExecuteP4VCCommandAsync("revisiongraph {0}", await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0106, () => Task.Run(async () => Helper.P4Helper.CopyP4PathToClipboardAsync(await Helper.VSHelper.GetOpenDocumentNameAsync())));
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0107, () => Task.Run(async () => Helper.P4Helper.OpenP4VAtAsync(await Helper.VSHelper.GetOpenDocumentNameAsync())));

            //await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0150, () => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4CommandAsync("edit {0}", filename)));
            //await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0151, () => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4CommandAsync("revert {0}", filename)));
            //await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0152, () => Helper.VSHelper.ForEachSelectedFile(filename => Helper.P4Helper.ExecuteP4VCommandAsync("prevdiff {0}", filename)));

            await Perforce.P4DiffAgainstCommand.InitializeAsync(this, CommandSetGuid, 0x0201);

            // Unreal Commands
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0300, () => Unreal.UnrealCommands.LaunchEditor(), (sender) => sender.Enabled = Helper.VSHelper.IsSolutionLoaded());
            await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x0301, () => Unreal.UnrealCommands.LaunchUnrealFrontend(), (sender) => sender.Enabled = Helper.VSHelper.IsSolutionLoaded());
            //await VisualStudio.CommandAction.InitializeAsync(this, CommandSetGuid, 0x302, () => Unreal.UnrealCommands.LaunchServer(), (sender) => sender.Enabled = Helper.VSHelper.IsSolutionLoaded());
            await Unreal.UnrealCommandLineArgs.InitializeAsync(this, CommandSetGuid, 0x0351);

            // Create debugger command line viewer / editor
            await VisualStudio.VSDebuggerCommandLine.InitializeAsync(this);

            // Test commands
            //await VisualStudio.CommandAction.InitializeAsync(
            //    this, CommandSetGuid, 0x0400,
            //    () =>
            //    {
            //        Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            //        var SolutionBuildManager = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsSolutionBuildManager)) as Microsoft.VisualStudio.Shell.Interop.IVsSolutionBuildManager2;
            //        IVsHierarchy ProjectHierarchy;
            //        if ( SolutionBuildManager.get_StartupProject(out ProjectHierarchy) == VSConstants.S_OK && ProjectHierarchy != null )
            //        {
            //            object ProjectObject;
            //            ProjectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out ProjectObject);
            //            var SelectedStartupProject = (EnvDTE.Project)ProjectObject;
            //            if (SelectedStartupProject != null)
            //            {
            //                var ActiveConfiguration = SelectedStartupProject.ConfigurationManager.ActiveConfiguration;
            //                if (ActiveConfiguration != null)
            //                {
            //                    var PropertyStorage = ProjectHierarchy as Microsoft.VisualStudio.Shell.Interop.IVsBuildPropertyStorage;
            //                    if (PropertyStorage != null)
            //                    {
            //                        string Text = "";

            //                        string ConfigurationName = string.Format("{0}|{1}", ActiveConfiguration.ConfigurationName, ActiveConfiguration.PlatformName);
            //                        if ( PropertyStorage.GetPropertyValue("LocalDebuggerCommandArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE, out Text) != VSConstants.S_OK )
            //                        {
            //                            if (PropertyStorage.GetPropertyValue("StartArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE, out Text) != VSConstants.S_OK)
            //                            {
            //                                Text = "";
            //                            }
            //                        }
            //                        Helper.VSHelper.OutputLine(Text);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //);

            // Create Title Renamer
            //await VisualStudio.VSTitleRename.InitializeAsync(this);
        }
        #endregion
    }
}
