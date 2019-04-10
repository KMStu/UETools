﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace UETools.Perforce
{
    public class P4DiffAgainstCommand : OleMenuCommand
    {
        private List<string> P4Paths;
        private int BaseCommandID;

        public P4DiffAgainstCommand(CommandID rootID)
            : base(OnInvoked, OnChanged, OnBeforeQueryStatus, rootID)
        {
            BaseCommandID = rootID.ID;
            MatchedCommandId = rootID.ID;

            P4Paths = new List<string>();
            P4Paths.Add(@"//UE4/Main");
            P4Paths.Add(@"//UE4/Dev-Destruction");
            P4Paths.Add(@"//UE4/Dev-Niagara");
            P4Paths.Add(@"//UE4/Dev-Rendering");
            P4Paths.Sort();
        }

        public static async Task InitializeAsync(AsyncPackage package, Guid commandSet, int commandID)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            if (commandService == null)
                return;

            commandService.AddCommand(new P4DiffAgainstCommand(new CommandID(commandSet, commandID)));
        }

        public override bool DynamicItemMatch(int commandID)
        {
            if (!IsValidDynamicItem(commandID))
                return false;

            MatchedCommandId = commandID;

            return true;
        }

        private bool IsValidDynamicItem(int commandID)
        {
            return (commandID >= BaseCommandID) && (commandID < BaseCommandID + P4Paths.Count);
        }

        private static string ChangeP4DepotPath(string originalPath, string destinationPath)
        {
            string[] destinationFolders = destinationPath.Split('/');
            string[] originalFolders = originalPath.Split('/');
            if ( originalFolders.Length < destinationFolders.Length )
            {
                throw new ApplicationException(string.Format("Original Folder '{0}' can not be mapped to destination folder '{1}'", originalPath, destinationPath));
            }

            string remappedPath = "";
            for ( int i=0; i < originalFolders.Length; ++i )
            {
                remappedPath += i < destinationFolders.Length ? destinationFolders[i] : originalFolders[i];
                if (i < originalFolders.Length - 1)
                {
                    remappedPath += '/';
                }
            }

            return remappedPath;
        }

        private string ExecuteCommand(int index)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string output;
            try
            {
                string sourcePath = Helper.VSHelper.GetOpenDocumentName();
                //Path.GetTempPath()

                if (string.IsNullOrEmpty(sourcePath))
                {
                    return "Nothing to do, no open document";
                }

                // Get source file information
                var sourceFileStat = Helper.P4Helper.GetFileStat(sourcePath);
                if (sourceFileStat == null )
                {
                    return string.Format("Failed to get FileStat for '{0}'", sourcePath);
                }

                if (sourceFileStat.DepotFile.StartsWith(P4Paths[index]))
                {
                    return string.Format("DepotFile '{0}' is already in the path '{1}'", sourceFileStat.DepotFile, P4Paths[index]);
                }

                // Remap to destination
                string destinationDepotPath = ChangeP4DepotPath(sourceFileStat.DepotFile, P4Paths[index]);
                var destinationFileStat = Helper.P4Helper.GetFileStat(destinationDepotPath);
                if (destinationFileStat == null)
                {
                    return string.Format("Failed to get FileStat for '{0}'", destinationDepotPath);
                }

                int destinationRevision = destinationFileStat.HeadRevision;

                string destinationPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(sourcePath) + "#" + destinationRevision.ToString());

                // Print file into temporary folder
                //p4 print -o file1 //depot/file1#1
                //p4merge file1 file2
                if ( Helper.P4Helper.ExecuteCommand("p4", string.Format("print -o {0} {1}#{2}", destinationPath, destinationDepotPath, destinationRevision)) != 0 )
                {
                    return string.Format("Failed to print file");
                }

                if ( Helper.P4Helper.ExecuteCommand("p4merge", string.Format("{0} {1}", sourcePath, destinationPath)) != 0 )
                {
                    return string.Format("Failed to execute p4merge");
                }

                output = "Complete";
            }
            catch (Exception exception)
            {
                output = exception.Message;
            }
            return output;
        }

        private static void OnInvoked(object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            P4DiffAgainstCommand matchedCommand = (P4DiffAgainstCommand)sender;
            int commandIndex = matchedCommand.MatchedCommandId - matchedCommand.BaseCommandID;
            if ((commandIndex >= 0) && (commandIndex < matchedCommand.P4Paths.Count))
            {
                Helper.VSHelper.OutputString("Result: " + matchedCommand.ExecuteCommand(commandIndex) + Environment.NewLine);
            }
        }

        private static void OnChanged(object sender, EventArgs args)
        {
        }

        private static void OnBeforeQueryStatus(object sender, EventArgs args)
        {
            P4DiffAgainstCommand matchedCommand = (P4DiffAgainstCommand)sender;

            int commandIndex = matchedCommand.MatchedCommandId - matchedCommand.BaseCommandID;
            if ((commandIndex >= 0) && (commandIndex < matchedCommand.P4Paths.Count) )
            {
                matchedCommand.Enabled = true;
                matchedCommand.Text = matchedCommand.P4Paths[matchedCommand.MatchedCommandId - matchedCommand.BaseCommandID];
            }
            else
            {
                matchedCommand.Enabled = false;
                matchedCommand.Text = "No items";
            }
        }
    }
}
