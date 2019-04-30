using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace UETools.Unreal
{
    public static class UnrealCommandLineArgs
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
                    return Array.ConvertAll(Options.Options.UECommandLineArgs, n => new VisualStudio.CommandDetails { Name = n, Enabled = bEnabled, Checked = commandLine.Contains("-" + n), });
                },
                async (d) =>
                {
                    string command = "-" + d.Name;
                    string commandLine = VisualStudio.VSDebuggerCommandLine.Instance.GetCommandLine();
                    if (d.Checked)
                    {
                        await Helper.VSHelper.OutputLineAsync("Removing " + command);
                        int index = commandLine.IndexOf(command);
                        if (index >= 0)
                        {
                            if ((index > 0) && (commandLine[index - 1] == ' '))
                            {
                                commandLine = commandLine.Remove(index - 1, command.Length + 1);
                            }
                            else
                            {
                                commandLine = commandLine.Remove(index, command.Length);
                            }
                        }
                    }
                    else
                    {
                        await Helper.VSHelper.OutputLineAsync("Adding " + command);
                        if ((commandLine.Length > 0) && !commandLine.EndsWith(" "))
                            commandLine += ' ';
                        commandLine += command;
                    }
                    VisualStudio.VSDebuggerCommandLine.Instance.SetCommandLine(commandLine);
                }
            );
        }
    }
}