using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace UETools.Unreal
{
	public static class UnrealCommandLaunchCOTF
	{
		public static async Task InitializeAsync(AsyncPackage package, Guid commandSetGuid, int commandID)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			await VisualStudio.CommandSubList.InitializeAsync(
				package,
				commandSetGuid,
				commandID,
				() =>
				{
					string commandLine = VisualStudio.VSDebuggerCommandLine.Instance.GetCommandLine();
					bool bEnabled = VisualStudio.VSDebuggerCommandLine.Instance.IsProjectLoaded();
					return Array.ConvertAll(Options.Options.UEPlatformNames, n => new VisualStudio.CommandDetails { Name = n, Enabled = bEnabled, Checked = false, });
				},
				async (d) =>
				{
					string ProjectName = UnrealCommands.GetStartupProjectName();
					string PlatformName = d.Name;

                    if ( UETools.Helper.VSHelper.MessageBoxYesNo("Launch COTF?", "Launch CookOnTheFly Server for '" + ProjectName + "' on '" + PlatformName + "'") )
                    {
                        string COTFFormat = @"{0} -run=cook -cookonthefly -target={1}";
                        string CommandLine = string.Format(COTFFormat, ProjectName, PlatformName);

                        UnrealCommands.RunBinary("UE4Editor.exe", CommandLine);
                    }
                    await Task.CompletedTask;
                }
			);
		}
	}
}