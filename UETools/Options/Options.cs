using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace UETools.Options
{
    public class Options
    {
        public Options(AsyncPackage package)
        {
            Package = package;
        }

        public static void Instantiate(AsyncPackage package)
        {
            if (_Instance != null)
                throw new ApplicationException("Options instance has already been created!");
            _Instance = new Options(package);
        }

        private AsyncPackage Package { get; set; }

        private static Options _Instance = null;
        internal static Options Instance { get { return _Instance; } }

        public static string[] P4Paths { get { OptionsPage page = (OptionsPage)_Instance.Package.GetDialogPage(typeof(OptionsPage)); return page.P4Paths; } }
        public static string[] UECommandLineArgs { get { OptionsPage page = (OptionsPage)_Instance.Package.GetDialogPage(typeof(OptionsPage)); return page.UECommandLineArgs; } }
		public static string[] UEPlatformNames { get { OptionsPage page = (OptionsPage)_Instance.Package.GetDialogPage(typeof(OptionsPage)); return page.UEPlatformNames; } }
	}
}
