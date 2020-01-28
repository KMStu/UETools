using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UETools.Unreal
{
    public static class UnrealCommands
    {
        public static async Task<string> GetBinariesFolderAsync()
        {
            string solutionPath = await Helper.VSHelper.GetSolutionPathAsync();
            string binariesPath = Path.Combine(solutionPath, "Engine", "Binaries", "Win64");
            return binariesPath;
        }

        public static void RunBinary(string binaryName, string arguments)
        {
            _ = Task.Run(
                async () =>
                {
                    string binaryPath = Path.Combine(await GetBinariesFolderAsync(), binaryName);
                    if (File.Exists(binaryPath))
                    {
                        try
                        {
                            if ( string.IsNullOrEmpty(arguments) )
                            {
                                Process.Start(binaryPath);
                            }
                            else
                            {
                                Process.Start(binaryPath, arguments);
                            }
                            await Helper.VSHelper.OutputLineAsync("Launched '{0}' with arguments '{1}'", binaryPath, string.IsNullOrEmpty(arguments) ? "" : arguments);
                        }
                        catch
                        {
                            await Helper.VSHelper.OutputLineAsync("Failed to launch executable '{0}'", binaryPath);
                        }
                    }
                    else
                    {
                        await Helper.VSHelper.OutputLineAsync("Failed to find executable '{0}'", binaryPath);
                    }
                }
            );
        }

        public static void RunBinary(string binaryName)
        {
            RunBinary(binaryName, null);
        }

        private static bool RecursiveFileExists(string Folder, string FileName, int CurrDepth)
        {
            // Does it exist?
            string UProjectPath = Path.Combine(Folder, FileName);
            if (File.Exists(UProjectPath))
                return true;

            // Recurse
            if ( CurrDepth < 3 )
            {
                foreach (string SubFolder in Directory.EnumerateDirectories(Folder))
                {
                    if (RecursiveFileExists(SubFolder, FileName, CurrDepth + 1))
                        return true;
                }
            }
            return false;
        }

        private static bool RecursiveFileExists(string Folder, string FileName)
        {
            return RecursiveFileExists(Folder, FileName, 0);
        }

		public static string GetStartupProjectName()
		{
			System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

			var SolutionBuildManager = VisualStudio.VSEvents.Instance?.SolutionBuildManager;

			IVsHierarchy ProjectHierarchy;
			if (SolutionBuildManager.get_StartupProject(out ProjectHierarchy) == VSConstants.S_OK && ProjectHierarchy != null)
			{
				object ProjectObject;
				ProjectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out ProjectObject);
				var SelectedStartupProject = (EnvDTE.Project)ProjectObject;
				if (SelectedStartupProject != null)
				{
					return SelectedStartupProject.Name;
				}
			}
			return null;
		}

		public static void LaunchEditor()
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.VerifyAccess();

            // Attempt to run with a valid .uproject
			string UProjectName = null;
			string StartupProjectName = GetStartupProjectName();
			if (StartupProjectName != null)
            {
				UProjectName = StartupProjectName + ".uproject";

                string UProjectPath = Path.Combine(Helper.VSHelper.GetSolutionPath(), StartupProjectName, UProjectName);
                if ( File.Exists(UProjectPath) || RecursiveFileExists(Helper.VSHelper.GetSolutionPath(), UProjectName) )
                {
					Helper.VSHelper.OutputLine("Found project " + UProjectName);
					RunBinary("UE4Editor.exe", StartupProjectName);
                    return;
                }

				Helper.VSHelper.OutputLine("Could not find project " + UProjectName);
            }

			// Run without a .uproject
			string MessageContents;
			if ( string.IsNullOrEmpty(UProjectName) )
			{
				MessageContents = "No Project Selected.";
			}
			else
			{
				MessageContents = "Failed to find UProject for '" + UProjectName + "'.";
			}

			if ( Helper.VSHelper.MessageBoxYesNo("Continue?", MessageContents + Environment.NewLine + "Do you want to launch without a project?") )
			{
				RunBinary("UE4Editor.exe");
			}
		}

        public static void LaunchUnrealFrontend()
        {
            RunBinary("UnrealFrontend.exe");
        }

		public static void GenerateGUIDToClipboard()
        {
            var guid = Guid.NewGuid().ToByteArray();
            var sb = new StringBuilder();
            sb.Append("(0x");
            for ( int i=0; i < guid.Length; ++i )
            {
                if ((i % 4) == 0 && (i != 0))
                    sb.Append(", 0x");
                sb.Append(guid[i].ToString("X2"));
            }
            sb.Append(")");
            System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
    }
}
