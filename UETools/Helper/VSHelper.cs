using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UETools.Helper
{
    public static class VSHelper
    {
        private static readonly Guid GUID_UnrealOutputWindow = new Guid(0x916c9b39u, (ushort)0xa101u, (ushort)0x4271u, 0xb3, 0xec, 0xb7, 0xaf, 0xb5, 0xb9, 0xf4, 0x2c);

        private static IVsOutputWindowPane GetOutputWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //var dte = Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            //dte.ToolWindows.OutputWindow.ActivePane.OutputString(GetOpenDocumentName());
            //dte.ToolWindows.OutputWindow.ActivePane.Activate();

            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outWindow == null)
                return null;

            Guid generalPaneGuid = GUID_UnrealOutputWindow;
            IVsOutputWindowPane generalPane = null;
            outWindow.GetPane(ref generalPaneGuid, out generalPane);
            if (generalPane == null)
            {
                outWindow.CreatePane(generalPaneGuid, "UE Toolbar", 1, 1);
                outWindow.GetPane(ref generalPaneGuid, out generalPane);
            }
            return generalPane;
        }

        public static void OutputString(string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outWindow = GetOutputWindow();
            outWindow?.OutputString(value);
            outWindow?.Activate();
        }

        public static string GetOpenDocumentName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            if (dte == null)
                return null;
            return dte.ActiveDocument != null ? dte.ActiveDocument.FullName : null;
        }

        public static void ForEachSelectedFile(Action<string> action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
            if (dte == null)
                return;

            foreach (SelectedItem selectedItem in dte.SelectedItems)
            {
                var fullPath = selectedItem?.ProjectItem?.Properties?.Item("FullPath")?.Value as string;
                if (fullPath != null)
                {
                    action(fullPath);
                }
            }
        }
    }
}
