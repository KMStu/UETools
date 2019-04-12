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

        internal void ValidateSettings()
        {
            if ((P4Paths == null) || (P4Paths.Length == 0))
            {
                P4Paths = new string[] { @"//UE4/Main", @"//UE4/Dev-Destruction", @"//UE4/Dev-Niagara", @"//UE4/Dev-Rendering" };
            }
            else
            {
                P4Paths = P4Paths.Select(a => a.Trim().TrimEnd(new char[] { '/' })).Where(a => !string.IsNullOrEmpty(a)).ToArray();
            }
            Array.Sort(P4Paths);
        }

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();

            ValidateSettings();
        }
    }
}
