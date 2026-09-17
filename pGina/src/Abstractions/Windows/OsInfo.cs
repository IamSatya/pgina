/*
	Copyright (c) 2011, pGina Team
	All rights reserved.

	Redistribution and use in source and binary forms, with or without
	modification, are permitted provided that the following conditions are met:
		* Redistributions of source code must retain the above copyright
		  notice, this list of conditions and the following disclaimer.
		* Redistributions in binary form must reproduce the above copyright
		  notice, this list of conditions and the following disclaimer in the
		  documentation and/or other materials provided with the distribution.
		* Neither the name of the pGina Team nor the names of its contributors 
		  may be used to endorse or promote products derived from this software without 
		  specific prior written permission.

	THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
	ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
	WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY
	DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
	(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
	LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
	(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
	SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace Abstractions.Windows
{
    public class OsInfo
    {
        public static bool IsVistaOrLater()
        {
            OperatingSystem sys = System.Environment.OSVersion;
            return sys.Platform == PlatformID.Win32NT && sys.Version.Major >= 6;
        }

        public static bool IsWindows10OrLater()
        {
            if (!IsWindows()) return false;
            OperatingSystem sys = System.Environment.OSVersion;
            if (sys.Platform == PlatformID.Win32NT && sys.Version.Major >= 10)
                return true;

            // Fallback to registry check in case of OS compatibility shims
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object majorVal = key.GetValue("CurrentMajorVersionNumber");
                        if (majorVal is int major && major >= 10)
                            return true;
                    }
                }
            }
            catch
            {
                // Ignore registry read errors
            }

            return false;
        }

        public static bool IsWindows11OrLater()
        {
            if (!IsWindows10OrLater()) return false;
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object buildVal = key.GetValue("CurrentBuildNumber");
                        if (buildVal != null && int.TryParse(buildVal.ToString(), out int buildNumber))
                        {
                            return buildNumber >= 22000;
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry read errors
            }

            return false;
        }

        public static bool IsWindows()
        {
            OperatingSystem sys = System.Environment.OSVersion;
            return sys.Platform == PlatformID.Win32NT;
        }

        public static bool Is64Bit()
        {
            return Environment.Is64BitOperatingSystem;
        }

        public static string OsDescription()
        {
            string osName = System.Environment.OSVersion.VersionString;
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string prodName = key.GetValue("ProductName") as string;
                        string displayVersion = key.GetValue("DisplayVersion") as string;
                        string build = key.GetValue("CurrentBuildNumber") as string;
                        if (!string.IsNullOrEmpty(prodName))
                        {
                            osName = $"{prodName} (Version: {displayVersion ?? "N/A"}, Build: {build ?? "N/A"})";
                        }
                    }
                }
            }
            catch
            {
                // Fallback to VersionString
            }

            return string.Format("OS: {0} (64-bit: {1}) Runtime: {2} Culture: {3}",
                osName, Is64Bit(), System.Environment.Version, CultureInfo.InstalledUICulture.EnglishName);
        }
    }
}
