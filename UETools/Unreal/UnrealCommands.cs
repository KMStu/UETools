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
