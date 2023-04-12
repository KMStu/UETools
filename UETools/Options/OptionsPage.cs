using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace UETools.Options
{
    public class OptionsPage : DialogPage
    {
        [Category("Perforce")]
        [DisplayName("Paths")]
        [Description("List of P4 paths that will be used to compare against, etc.")]
        public string[] P4Paths { get; set; }

        [Category("Unreal")]
        [DisplayName("Command Line Arugments")]
        [Description("List of command line arguments to easy toggle on / off")]
        public string[] UECommandLineArgs { get; set; }

		[Category("Unreal")]
		[DisplayName("Supported Platforms")]
		[Description("List of support platforms, note that they may not all work for your project")]
		public string[] UEPlatformNames { get; set; }

		internal void ValidateSettings()
        {
            // P4 Paths
            if ((P4Paths == null) || (P4Paths.Length == 0))
            {
                P4Paths = new string[] { @"//UE5/Main", @"//UE5/Release-5.0", @"//UE5/Release-5.1", @"//UE5/Release-5.2" };
            }
            else
            {
                P4Paths = P4Paths.Select(a => a.Trim().TrimEnd(new char[] { '/' })).Where(a => !string.IsNullOrEmpty(a)).ToArray();
            }
            Array.Sort(P4Paths);

            // Command line arguments
            if ((UECommandLineArgs == null) || (UECommandLineArgs.Length == 0))
            {
                UECommandLineArgs = new string[] { @"d3ddebug", @"onethread", @"norhithread", "execcmds=\"stat unit,stat namedevents\"", @"filehostip" };
            }
            Array.Sort(UECommandLineArgs);

			// Platforms
			if ( (UEPlatformNames == null) || (UEPlatformNames.Length == 0) )
			{
				UEPlatformNames = new string[] { @"Win64", @"PS4", @"XboxOne", @"Switch" };
			}
			Array.Sort(UEPlatformNames);
		}

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();

            ValidateSettings();
        }
    }
}
