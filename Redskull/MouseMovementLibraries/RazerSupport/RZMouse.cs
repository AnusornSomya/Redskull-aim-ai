using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using Visuality;

namespace MouseMovementLibraries.RazerSupport
{
    internal class RZMouse
    {
        private const string rzctlpath = "rzctl.dll";

        [DllImport(rzctlpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool init();

        [DllImport(rzctlpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mouse_move(int x, int y, bool starting_point);

        [DllImport(rzctlpath, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mouse_click(int up_down);

        private static readonly List<string> Razer_HID = [];

        public static async Task<bool> Load()
        {
            if (!await EnsureRazerSynapseInstalled())
            {
                return false;
            }

            if (!File.Exists(rzctlpath))
            {
                new NoticeBar("rzctl.dll is missing. This build will not download it automatically. Place rzctl.dll next to the executable and re-select Razer Synapse.", 5000).Show();
                return false;
            }

            if (!DetectRazerDevices())
            {
                new NoticeBar("No Razer device detected. This method is unusable.", 5000).Show();
                return false;
            }

            try
            {
                return init();
            }
            catch (BadImageFormatException)
            {
                new NoticeBar("rzctl.dll is incompatible with this system. Replace it manually with a compatible file and try again.", 5000).Show();
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize Razer mode.\n{ex.Message}\n\nThis build will not download missing dependencies automatically. Please verify rzctl.dll and Razer Synapse manually.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        private static bool DetectRazerDevices()
        {
            Razer_HID.Clear();
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Manufacturer LIKE 'Razer%'");
            var devices = searcher.Get().Cast<ManagementBaseObject>();

            Razer_HID.AddRange(devices.Select(d => d["DeviceID"]?.ToString() ?? string.Empty));
            return Razer_HID.Count > 0;
        }

        private static async Task<bool> EnsureRazerSynapseInstalled()
        {
            if (Process.GetProcessesByName("RazerAppEngine").Any())
            {
                return true;
            }

            if (!IsRazerSynapseInstalled())
            {
                MessageBox.Show(
                    "Razer Synapse is not installed.\n\nThis build will not download it automatically. Please install it manually and try again.",
                    "REDSKULL AIM AI - Razer Synapse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            MessageBox.Show(
                "Razer Synapse is installed but not running.\n\nPlease open it manually and then re-select this movement method.",
                "REDSKULL AIM AI - Razer Synapse",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        private static bool IsRazerSynapseInstalled()
        {
            return Directory.Exists(@"C:\Program Files\Razer") ||
                   Directory.Exists(@"C:\Program Files (x86)\Razer") ||
                   Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Razer") != null;
        }
    }
}
