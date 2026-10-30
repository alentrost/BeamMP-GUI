using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace BeamMPServerManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool forceSetup = false;
            if (e.Args != null)
            {
                foreach (string arg in e.Args)
                {
                    if (arg.Equals("--setup", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("--install", StringComparison.OrdinalIgnoreCase))
                    {
                        forceSetup = true;
                        break;
                    }
                }
            }

            // If the executable itself is named *Installer* or *Setup*, always run setup
            try
            {
                string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                if (exeName.IndexOf("installer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    exeName.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    forceSetup = true;
                }
            }
            catch { }

            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            bool serverExeInCurrentDir = File.Exists(Path.Combine(currentDir, "BeamMP-Server.exe"));
            BeamMP_GUI.AppSettings settings = BeamMP_GUI.AppSettings.Load();

            // If forced setup, or not installed yet, or BeamMP-Server.exe is not in current folder:
            if (forceSetup || !settings.IsInstalled || !serverExeInCurrentDir)
            {
                var setupWin = new BeamMP_GUI.SetupWindow(false);
                setupWin.Show();
            }
            else
            {
                var mainWin = new MainWindow();
                mainWin.Show();
            }
        }
    }
}
