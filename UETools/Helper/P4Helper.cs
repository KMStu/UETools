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
            string output;
            if (Helper.ProcessHelper.RunProcess("p4", "fstat " + Path.GetFileName(filename), Path.GetDirectoryName(filename), out output) == 0)
            {
                try
                {
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
                    fileStat.ClientFile = findClientFile.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim();
                    fileStat.HeadRevision = int.Parse(findHeadRevision.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim());
                    fileStat.HaveRevision = int.Parse(findHaveRevision.Match(output).Groups[1].ToString().TrimEnd('\r', '\n').Trim());
                    return fileStat;
                }
                catch
                {
                    // Do nothing failed
                }
            }

            return null;
        }

        public static P4Settings GetSettings(string filename)
        {
            string output;
            if (Helper.ProcessHelper.RunProcess("p4", "set", Path.GetDirectoryName(filename), out output) == 0)
            {
                try
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
                catch
                {
                    // Do nothing failed
                }
            }

            return null;
        }

        public static void ExecuteP4Command(string format, string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            try
            {
                if (documentPath == null)
                {
                    output = "Nothing to do, no open document";
                }
                else
                {
                    string arguments = string.Format(format, documentPath);
                    string workingDirectory = Path.GetDirectoryName(documentPath);
                    Helper.VSHelper.OutputString("ExecuteCommand: p4 " + arguments + Environment.NewLine);
                    Helper.ProcessHelper.RunProcess("p4", arguments, workingDirectory, out output);
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            Helper.VSHelper.OutputString("Result: " + output + Environment.NewLine);
        }

        public static void ExecuteP4VCCommand(string format, string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            try
            {
                if (documentPath == null)
                {
                    output = "Nothing to do, no open document";
                }
                else
                {
                    string arguments = string.Format(format, documentPath);
                    string workingDirectory = Path.GetDirectoryName(documentPath);
                    Helper.VSHelper.OutputString("ExecuteCommand: p4 " + arguments + Environment.NewLine);
                    Helper.ProcessHelper.RunProcess("p4vc", arguments, workingDirectory, out output);
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            Helper.VSHelper.OutputString("Result: " + output + Environment.NewLine);
        }

        public static void ExecuteP4VCommand(string format, string documentPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            try
            {
                if (documentPath == null)
                {
                    output = "Nothing to do, no open document";
                }
                else
                {
                    P4Settings settings = GetSettings(documentPath);
                    if (settings != null)
                    {
                        string workingDirectory = Path.GetDirectoryName(documentPath);

                        string arguments = string.Format("-p {0} -c {1} -u {2} -cmd \"{3}\"", settings.Port, settings.Client, settings.User, string.Format(format, documentPath));
                        Helper.VSHelper.OutputString("ExecuteCommand: p4v " + arguments + Environment.NewLine);
                        Helper.ProcessHelper.RunProcess("p4v", arguments, workingDirectory, out output);
                    }
                    else
                    {
                        output = "Failed to get file information";
                    }
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            Helper.VSHelper.OutputString("Result: " + output + Environment.NewLine);
        }
    }
}
