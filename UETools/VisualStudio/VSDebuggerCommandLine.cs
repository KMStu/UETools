using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace UETools.VisualStudio
{
    public class VSDebuggerCommandLine
    {
        private const int VSComboID = 0x0400;
        private const int VSComboListID = 0x0401;

        private VSDebuggerCommandLine()
        {
        }

        public static VSDebuggerCommandLine Instance { get; private set; }
        public IVsSolutionBuildManager2 SolutionBuildManager { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            Instance = new VSDebuggerCommandLine();
            Instance.SolutionBuildManager = ServiceProvider.GlobalProvider?.GetService((typeof(SVsSolutionBuildManager))) as IVsSolutionBuildManager2;

            // Create command handlers
            OleMenuCommandService commandService = await package?.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance.ComboCommand = new OleMenuCommand(Instance.ComboHandler, new CommandID(UEToolsPackage.CommandSetGuid, VSComboID));
            Instance.ComboCommandList = new OleMenuCommand(Instance.ComboListHandler, new CommandID(UEToolsPackage.CommandSetGuid, VSComboListID));

            Instance.ComboCommand.Enabled = false;
            Instance.ComboCommandList.Enabled = false;

            commandService.AddCommand(Instance.ComboCommand);
            commandService.AddCommand(Instance.ComboCommandList);

            // Hook into events so we can handle which command line we should be using
            VSEvents.Instance.OnSolutionOpened += () => Instance.UpdateCommandLineCombo(true);
            VSEvents.Instance.OnSolutionClosed += () => Instance.UpdateCommandLineCombo(false);
            VSEvents.Instance.OnStartupProjectChanged += (p) => Instance.UpdateCommandLineCombo(p != null);
            //VSEvents.Instance.OnStartupProjectPropertyChanged += Instance.UpdateCommandLineCombo(true);
            //VSEvents.Instance.OnStartupProjectConfigChanged += Instance.UpdateCommandLineCombo(true);

            // If the solution is already loaded, set up the command line right away
            var solService = await package?.GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
            bool isSolOpen = (bool)value;
            if (isSolOpen)
            {
                Instance.UpdateCommandLineCombo(true);
            }
        }

        private OleMenuCommand ComboCommand;
        private OleMenuCommand ComboCommandList;
        private List<string> ComboList = new List<string>();
        private const int ComboListMax = 16;
        private string EditedString = null;

        private void ComboHandler(object Sender, EventArgs Args)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            try
            {
                var OleArgs = (OleMenuCmdEventArgs)Args;

                // Combo box is setting a string (i.e. we picked from the MRU list)
                string InString = OleArgs.InValue as string;
                if ( InString != null )
                {
                    EditedString = null;
                    SetCommandLine(InString);
                }
                // Combo box is editing the current string
                else if ( OleArgs.OutValue != IntPtr.Zero )
                {
                    string displayText = null;
                    if ( OleArgs.InValue != null )
                    {
                        object[] InArray = OleArgs.InValue as object[];
                        if (InArray != null && InArray.Length > 0)
                        {
                            displayText = InArray.Last() as string;
                        }
                    }

                    if ( displayText == null )
                    {
                        // We are committing the currently edited text
                        if (EditedString != null)
                        {
                            SetCommandLine(EditedString);
                            displayText = EditedString;
                            EditedString = null;
                        }
                        else
                        {
                            displayText = GetCommandLine();
                        }
                    }
                    // We are editing the text (typeing)
                    else
                    {
                        EditedString = displayText;
                    }


                    Marshal.GetNativeVariantForObject(displayText, OleArgs.OutValue);
                }
            }
            catch ( Exception e )
            {
                Helper.VSHelper.OutputLine(e.ToString());
            }
        }

        private void ComboListHandler(object Sender, EventArgs Args)
        {
            var OleArgs = (OleMenuCmdEventArgs)Args;

            Marshal.GetNativeVariantForObject(ComboList.ToArray(), OleArgs.OutValue);
        }

        void UpdateCommandLineCombo(bool bHasStartupProject)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            ComboCommand.Enabled = bHasStartupProject;
            ComboCommandList.Enabled = bHasStartupProject;

            SetCommandLineMRU(GetCommandLine());
        }

        private string MakeConfigurationName(EnvDTE.Configuration configuration)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            var platformName = string.Compare(configuration.PlatformName, "Any CPU") == 0 ? "AnyCPU" : configuration.PlatformName;
            return string.Format("{0}|{1}", configuration.ConfigurationName, platformName);
        }

        public bool IsProjectLoaded()
        {
            return ComboCommand.Enabled;
        }

        public string GetCommandLine()
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            string debuggerCommandLine = "";

            IVsHierarchy ProjectHierarchy;
            if (SolutionBuildManager.get_StartupProject(out ProjectHierarchy) == VSConstants.S_OK && ProjectHierarchy != null)
            {
                object ProjectObject;
                ProjectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out ProjectObject);
                var SelectedStartupProject = (EnvDTE.Project)ProjectObject;
                if (SelectedStartupProject != null)
                {
                    var ActiveConfiguration = SelectedStartupProject.ConfigurationManager.ActiveConfiguration;
                    if (ActiveConfiguration != null)
                    {
                        var PropertyStorage = ProjectHierarchy as IVsBuildPropertyStorage;
                        if (PropertyStorage != null)
                        {
                            string ConfigurationName = MakeConfigurationName(ActiveConfiguration);
                            if (PropertyStorage.GetPropertyValue("LocalDebuggerCommandArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE, out debuggerCommandLine) != VSConstants.S_OK)
                            {
                                if (PropertyStorage.GetPropertyValue("StartArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE, out debuggerCommandLine) != VSConstants.S_OK)
                                {
                                    debuggerCommandLine = "";
                                }
                            }
                        }
                    }
                }
            }

            return debuggerCommandLine;
        }

        private void SetCommandLineMRU(string NewCommandLine)
        {
            ComboList.RemoveAll(s => string.Compare(s, NewCommandLine) == 0);
            ComboList.Insert(0, NewCommandLine);
            if (ComboList.Count > ComboListMax)
            {
                ComboList.RemoveAt(ComboList.Count - 1);
            }
        }

        public void SetCommandLine(string NewCommandLine)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            // Update out MRU combo list
            SetCommandLineMRU(NewCommandLine);

            // Set on the project
            IVsHierarchy ProjectHierarchy;
            if (SolutionBuildManager.get_StartupProject(out ProjectHierarchy) == VSConstants.S_OK && ProjectHierarchy != null)
            {
                object ProjectObject;
                ProjectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out ProjectObject);
                var SelectedStartupProject = (EnvDTE.Project)ProjectObject;
                if (SelectedStartupProject != null)
                {
                    var ActiveConfiguration = SelectedStartupProject.ConfigurationManager.ActiveConfiguration;
                    if (ActiveConfiguration != null)
                    {
                        var PropertyStorage = ProjectHierarchy as IVsBuildPropertyStorage;
                        if (PropertyStorage != null)
                        {
                            string ConfigurationName = MakeConfigurationName(ActiveConfiguration);

                            // Remove property
                            if ( string.IsNullOrEmpty(NewCommandLine) )
                            {
                                PropertyStorage.RemoveProperty("LocalDebuggerCommandArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE);
                                PropertyStorage.RemoveProperty("RemoteDebuggerCommandArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE);
                            }
                            // Set property
                            else
                            {
                                PropertyStorage.SetPropertyValue("LocalDebuggerCommandArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE, NewCommandLine);
                                PropertyStorage.SetPropertyValue("RemoteDebuggerCommandArguments", ConfigurationName, (uint)_PersistStorageType.PST_USER_FILE, NewCommandLine);
                            }
                        }
                    }
                }
            }
        }
    }
}
