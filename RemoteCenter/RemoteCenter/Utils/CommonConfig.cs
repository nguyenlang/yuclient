using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteCenter.Utils
{
    public class CommonConfig
    {
        public const string PrefixCmd = "shell am start -a android.intent.action.VIEW -d ";
        public const string AdbFilePath = @"D:\Softwares\platform-tools\adb.exe";

        public const string StopYoutube = "shell am force-stop com.google.android.youtube";
    }
}
