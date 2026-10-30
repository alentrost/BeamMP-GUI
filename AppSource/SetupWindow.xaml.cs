using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace BeamMP_GUI
{
    public partial class SetupWindow : Window
    {
        private readonly bool _launchedFromApp;

        public SetupWindow(bool launchedFromApp = false)
        {
            InitializeComponent();
            _launchedFromApp = launchedFromApp;

            // Load existing settings if available, or auto-detect
            AppSettings settings = AppSettings.Load();
            string initialPath = settings.ServerDirectory;
            if (string.IsNullOrEmpty(initialPath) || !Directory.Exists(initialPath))
            {
                initialPath = ShortcutHelper.DetectDefaultServerPath();
            }

            txtServerPath.Text = initialPath;
            chkDesktop.IsChecked = settings.DesktopShortcut;
            chkStartMenu.IsChecked = settings.StartMenuShortcut;

            if (_launchedFromApp)
            {
                btnInstall.Content = "💾   Save & Update Shortcuts";
                chkLaunchAfter.Visibility = Visibility.Collapsed;
            }

            UpdateValidationStatus();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Select your BeamMP Server folder";
                if (Directory.Exists(txtServerPath.Text))
                {
                    fbd.SelectedPath = txtServerPath.Text;
                }

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtServerPath.Text = fbd.SelectedPath;
                }
            }
        }

        private void TxtServerPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateValidationStatus();
        }

        private void UpdateValidationStatus()
        {
            string path = txtServerPath.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                borderStatus.Background = new SolidColorBrush(Color.FromRgb(42, 34, 24));
                borderStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                lblStatus.Text = "⚠ Please choose a server directory.";
                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                return;
            }

            string serverExe = Path.Combine(path, "BeamMP-Server.exe");
            if (File.Exists(serverExe))
            {
                borderStatus.Background = new SolidColorBrush(Color.FromRgb(21, 62, 43));
                borderStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                lblStatus.Text = "✓ BeamMP-Server.exe detected in this folder!";
                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
            }
            else
            {
                borderStatus.Background = new SolidColorBrush(Color.FromRgb(42, 34, 24));
                borderStatus.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                lblStatus.Text = "ℹ BeamMP-Server.exe not detected yet (will install here)";
                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_launchedFromApp)
            {
                Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            string targetDir = txtServerPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Please specify a valid folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string targetExe = Path.Combine(targetDir, "BeamMP-GUI.exe");

                lblInstallProgress.Text = "Installing BeamMP-GUI...";

                // If running from somewhere else, copy to target folder
                if (!currentExe.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(currentExe, targetExe, true);
                }

                // Create Desktop Shortcut if checked
                if (chkDesktop.IsChecked == true)
                {
                    string err;
                    ShortcutHelper.CreateShortcut(targetExe, targetDir, ShortcutHelper.GetDesktopPath(), "BeamMP-GUI", out err);
                }

                // Create Start Menu Shortcut if checked
                if (chkStartMenu.IsChecked == true)
                {
                    string err;
                    ShortcutHelper.CreateShortcut(targetExe, targetDir, ShortcutHelper.GetStartMenuPath(), "BeamMP-GUI", out err);
                }

                // Save settings
                AppSettings settings = new AppSettings();
                settings.ServerDirectory = targetDir;
                settings.IsInstalled = true;
                settings.DesktopShortcut = chkDesktop.IsChecked == true;
                settings.StartMenuShortcut = chkStartMenu.IsChecked == true;
                settings.Save();

                if (_launchedFromApp)
                {
                    MessageBox.Show("Shortcuts updated successfully!", "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                    return;
                }

                if (chkLaunchAfter.IsChecked == true)
                {
                    if (currentExe.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                    {
                        // Open main window directly
                        BeamMPServerManager.MainWindow mainWin = new BeamMPServerManager.MainWindow();
                        mainWin.Show();
                        Close();
                    }
                    else
                    {
                        // Launch installed executable from server folder
                        ProcessStartInfo psi = new ProcessStartInfo(targetExe);
                        psi.WorkingDirectory = targetDir;
                        Process.Start(psi);
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    MessageBox.Show("Installation complete!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Installation error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                lblInstallProgress.Text = "Error during setup.";
            }
        }
    }
}
