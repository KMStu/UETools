using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UETools.Helper
{
    public class P4FileStat
    {
        public string DepotFile { get; set; }
        public string ClientFile { get; set; }
        public int HeadRevision { get; set; }
        public int HaveRevision { get; set; }
    }

    public class P4Settings
    {
        public string Client { get; set; }
        public string Port { get; set; }
        public string User { get; set; }
    }

    public static class P4Helper
    {
        public static P4FileStat GetFileStat(string filename)
        {
            try
            {
                string output;
                if (filename.StartsWith("//"))
                {
                    if (Helper.ProcessHelper.RunProcess("p4", "fstat " + filename, out output) != 0)
                    {
                        return null;
                    }
                }
                else
                {
                    if (Helper.ProcessHelper.RunProcess("p4", "fstat " + Path.GetFileName(filename), Path.GetDirectoryName(filename), out output) != 0)
                    {
                        return null;
                    }
                }

                //... depotFile //UE4/Release-4.22/GenerateProjectFiles.bat
                //... clientFile D:\stu.mckenna-4-22\GenerateProjectFiles.bat
                //... isMapped
                //... headAction branch
                //... headType text+x
                //... headTime 1548987099
                //... headRev 1
                //... headChange 4862694
                //... headModTime 1481225333
                //... haveRev 1

                var findDepotFile = new System.Text.RegularExpressions.Regex(@"depotFile([^\n]+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                var findClientFile = new System.Text.RegularExpressions.Regex(@"clientFile([^\n]+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                var findHeadRevision = new System.Text.RegularExpressions.Regex(@"headRev([^\n]+)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                var findHaveRevision = new System.Text.RegularExpressions.Regex(@"haveRev([^\n]+)$", System.Text.RegularExpressions.RegexOptions.Multiline);

                P4FileStat fileStat = new P4FileStat();
                fileStat.DepotFile = findDepotFile.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim();
                fileStat.HeadRevision = int.Parse(findHeadRevision.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim());

                var clientMatch = findClientFile.Match(output);
                if (clientMatch.Groups.Count > 1)
                {
                    fileStat.ClientFile = findClientFile.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim();
                    fileStat.HaveRevision = int.Parse(findHaveRevision.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim());
                }
                return fileStat;
            }
            catch
            {
                // Do nothing failed
            }

            return null;
        }

        public static P4Settings GetSettings(string filename)
        {
            try
            {
                string output;
                if (Helper.ProcessHelper.RunProcess("p4", "set", Path.GetDirectoryName(filename), out output) == 0)
                {
                    //P4CLIENT=someclient (set)
                    //P4CONFIG=P4CONFIG (set) (config 'noconfig')
                    //P4EDITOR=C:\windows\SysWOW64\notepad.exe (set)
                    //P4PORT=someport:1667 (set)
                    //P4USER=someuser (set)
                    //P4_proxy.sea.epicgames.net:1667_CHARSET=none (set)

                    var findClient = new System.Text.RegularExpressions.Regex(@"P4CLIENT=([^\n(]+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                    var findPort = new System.Text.RegularExpressions.Regex(@"P4PORT=([^\n(]+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                    var findUser = new System.Text.RegularExpressions.Regex(@"P4USER=([^\n(]+)", System.Text.RegularExpressions.RegexOptions.Multiline);

                    P4Settings settings = new P4Settings();
                    settings.Client = findClient.Match(output).Groups[1].ToString().Trim();
                    settings.Port = findPort.Match(output).Groups[1].ToString().Trim();
                    settings.User = findUser.Match(output).Groups[1].ToString().Trim();
                    return settings;
                }
            }
            catch
            {
                // Do nothing failed
            }

            return null;
        }
   
        public static void CopyP4PathToClipboard(string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            try
            {
                if (string.IsNullOrEmpty(documentPath))
                {
                    output = "Nothing to do, no open document";
                }
                else
                {
                    P4FileStat fileStat = GetFileStat(documentPath);
                    if ( fileStat != null )
                    {
                        System.Windows.Forms.Clipboard.SetText(fileStat.DepotFile);
                        output = string.Format("Copied '{0}' to clipboard", fileStat.DepotFile);
                    }
                    else
                    {
                        output = string.Format("Failed to get FileStat for '{0}'", documentPath);
                    }
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            Helper.VSHelper.OutputString("Result: " + output + Environment.NewLine);
        }

        public static void OpenP4VAt(string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            try
            {
                if (string.IsNullOrEmpty(documentPath))
                {
                    output = "Nothing to do, no open document";
                }
                else
                {
                    P4Settings settings = GetSettings(documentPath);
                    P4FileStat fileStat = GetFileStat(documentPath);
                    if (settings != null && fileStat != null)
                    {
                        string arguments = string.Format("-p {0} -c {1} -u {2} -s {3}", settings.Port, settings.Client, settings.User, fileStat.DepotFile);
                        Helper.VSHelper.OutputString("ExecuteCommand: p4v " + arguments + Environment.NewLine);
                        Helper.ProcessHelper.RunProcess("p4v", arguments, Path.GetDirectoryName(documentPath), out output);
                    }
                    else
                    {
                        output = string.Format("Failed to get FileStat / P4Settings for '{0}'", documentPath);
                    }
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            Helper.VSHelper.OutputString("Result: " + output + Environment.NewLine);
        }

        public static int ExecuteCommand(string executable, string arguments, string workingDirectory)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            int returnCode = 1;
            try
            {
                Helper.VSHelper.OutputString("ExecuteCommand: " + executable + " " + arguments + Environment.NewLine);
                if (workingDirectory == null)
                {
                    returnCode = Helper.ProcessHelper.RunProcess(executable, arguments, out output);
                }
                else
                {
                    returnCode = Helper.ProcessHelper.RunProcess(executable, arguments, workingDirectory, out output);
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            Helper.VSHelper.OutputString("Result: " + output + Environment.NewLine);
            return returnCode;
        }

        public static int ExecuteCommand(string executable, string arguments)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ExecuteCommand(executable, arguments, null);
        }

        public static int ExecuteP4Command(string format, string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(documentPath))
            {
                Helper.VSHelper.OutputString("Result: Nothing to do, no open document" + Environment.NewLine);
                return 1;
            }

            return ExecuteCommand("p4", string.Format(format, documentPath), Path.GetDirectoryName(documentPath));
        }

        public static int ExecuteP4VCCommand(string format, string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(documentPath))
            {
                Helper.VSHelper.OutputString("Result: Nothing to do, no open document" + Environment.NewLine);
                return 1;
            }

            return ExecuteCommand("p4vc", string.Format(format, documentPath), Path.GetDirectoryName(documentPath));
        }

        public static int ExecuteP4VCommand(string format, string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(documentPath))
            {
                Helper.VSHelper.OutputString("Result: Nothing to do, no open document" + Environment.NewLine);
                return 1;
            }

            P4Settings settings = GetSettings(documentPath);
            if (settings == null)
            {
                Helper.VSHelper.OutputString("Result: Failed to get file information" + Environment.NewLine);
                return 1;
            }

            string arguments = string.Format("-p {0} -c {1} -u {2} -cmd \"{3}\"", settings.Port, settings.Client, settings.User, string.Format(format, documentPath));
            return ExecuteCommand("p4v", arguments, Path.GetDirectoryName(documentPath));

        }
    }
}
