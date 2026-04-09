using Other;
using System.IO;
using System.Security.Principal;
using System.Windows;

namespace MouseMovementLibraries.ddxoftSupport
{
    internal class DdxoftMain
    {
        public static ddxoftMouse ddxoftInstance = new();
        private static readonly string ddxoftpath = "ddxoft.dll";

        public static async Task<bool> DLLLoading()
        {
            try
            {
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    MessageBox.Show("The ddxoft Virtual Input Driver requires REDSKULL AIM AI to be run as an administrator, please close REDSKULL AIM AI and run it as administrator to use this movement method.", "REDSKULL AIM AI");
                    return false;
                }

                if (!File.Exists(ddxoftpath))
                {
                    LogManager.Log(
                        LogManager.LogLevel.Warning,
                        $"{ddxoftpath} is missing. This build will not download it automatically. Please place {ddxoftpath} next to the executable and re-select ddxoft Virtual Input Driver.",
                        true);
                    return false;
                }

                if (ddxoftInstance.Load(ddxoftpath) != 1 || ddxoftInstance.btn!(0) != 1)
                {
                    MessageBox.Show("The ddxoft virtual input driver is not compatible with your PC, please try a different Mouse Movement Method.", "REDSKULL AIM AI");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load ddxoft virtual input driver.\n\n" + ex, "REDSKULL AIM AI");
                return false;
            }
        }

        public static async Task<bool> Load() => await DLLLoading();
    }
}
