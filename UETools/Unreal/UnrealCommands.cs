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

        public static void RunBinary(string binaryName)
        {
            _ = Task.Run(
                async () =>
                {
                    string binaryPath = Path.Combine(await GetBinariesFolderAsync(), binaryName);
                    if (File.Exists(binaryPath))
                    {
                        try
                        {
                            Process.Start(binaryPath);
                            await Helper.VSHelper.OutputLineAsync("Launched '{0}'", binaryPath);
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

        public static void LaunchEditor()
        {
            RunBinary("UE4Editor.exe");
        }

        public static void LaunchUnrealFrontend()
        {
            RunBinary("UnrealFrontend.exe");
        }
    }
}
