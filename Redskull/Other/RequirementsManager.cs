using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace Other
{
    internal class RequirementsManager
    {
        public static bool IsVCRedistInstalled()
        {
            // Visual C++ Redistributable for Visual Studio 2015, 2017, and 2019 check
            string regKeyPath = @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";

            using (var key = Registry.LocalMachine.OpenSubKey(regKeyPath))
            {
                if (key != null && key.GetValue("Installed") != null)
                {
                    object? installedValue = key.GetValue("Installed");
                    return installedValue != null && (int)installedValue == 1;
                }
            }

            return false;
        }

        public static bool IsMemoryIntegrityEnabled() // false if enabled true if disabled, you want it disabled
        {
            //credits to Themida
            string keyPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforceCodeIntegrity";
            string valueName = "Enabled";
            object? value = Registry.GetValue(keyPath, valueName, null);
            if (value != null && Convert.ToInt32(value) == 1)
            {
                LogManager.Log(LogManager.LogLevel.Warning, "เปิด Memory Integrity อยู่ กรุณาปิดก่อนใช้งาน Logitech Driver", true, 7000);
                return false;
            }
            else return true;
        }

        public static bool CheckForGhub()
        {
            try
            {
                Process? process = Process.GetProcessesByName("lghub").FirstOrDefault(); //gets the first process named "lghub"
                if (process == null)
                {
                    ShowLGHubNotRunningMessage();
                    return false;
                }

                string ghubfilepath = process.MainModule.FileName;
                if (ghubfilepath == null)
                {
                    LogManager.Log(LogManager.LogLevel.Error, "เกิดข้อผิดพลาด กรุณารันแบบผู้ดูแลระบบแล้วลองใหม่", true);
                    return false;
                }

                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(ghubfilepath);

                if (!versionInfo.ProductVersion.Contains("2021"))
                {
                    ShowLGHubImproperInstallMessage();
                    return false;
                }

                return true;
            }
            catch (AccessViolationException ex)
            {
                LogManager.Log(LogManager.LogLevel.Error, $"เกิดข้อผิดพลาด: {ex.Message}\nกรุณารันแบบผู้ดูแลระบบแล้วลองใหม่", true);
                return false;
            }
        }

        private static void ShowLGHubNotRunningMessage()
        {
            MessageBox.Show(
                "ยังไม่พบ LG HUB ที่กำลังทำงานอยู่\n\nเวอร์ชันนี้จะไม่ดาวน์โหลดหรือติดตั้งให้อัตโนมัติ กรุณาติดตั้งและเปิด LG HUB ด้วยตัวเองก่อน แล้วค่อยกลับมาเลือกวิธีนี้อีกครั้ง",
                "REDSKULL AIM AI - LG HUB Mouse Movement",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static void ShowLGHubImproperInstallMessage()
        {
            MessageBox.Show(
                "LG HUB ที่ตรวจพบไม่ตรงกับเวอร์ชันที่รองรับ\n\nเวอร์ชันนี้จะไม่ดาวน์โหลดหรือติดตั้งใหม่ให้อัตโนมัติ กรุณาจัดการไฟล์ LG HUB ในเครื่องด้วยตัวเองก่อน",
                "REDSKULL AIM AI - LG HUB Mouse Movement",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
