using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BeamMPServerManager
{
    public partial class MainWindow : Window
    {
        private readonly string _baseDir;
        private readonly string _configFilePath;
        private readonly string _serverExePath;

        private readonly ServerManager _serverManager;
        private readonly TomlConfig _config;
        private readonly ModManager _modManager;

        private readonly DispatcherTimer _uptimeTimer;
        private readonly DispatcherTimer _saveNoticeTimer;

        private bool _isAuthKeyMasked = true;
        private string _actualAuthKey = "";
        private int _consoleLines = 0;

        // Vector Eye Icon SVG Data Paths
        private const string IconEyeOpen = "M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9Z";
        private const string IconEyeClosed = "M11.83,9L15,12.16C15,12.11 15,12.05 15,12A3,3 0 0,0 12,9C11.94,9 11.89,9 11.83,9M7.53,9.8L9.08,11.35C9.03,11.56 9,11.77 9,12A3,3 0 0,0 12,15C12.22,15 12.44,14.97 12.65,14.92L14.2,16.47C13.53,16.8 12.79,17 12,17A5,5 0 0,1 7,12C7,11.21 7.2,10.47 7.53,9.8M2,4.27L4.28,6.55L4.73,7C3.08,8.3 1.78,10 1,12C2.73,16.39 7,19.5 12,19.5C13.55,19.5 15.03,19.2 16.38,18.66L16.81,19.08L19.73,22L21,20.72L3.27,3M12,7A5,5 0 0,1 17,12C17,12.64 16.87,13.26 16.64,13.82L19.57,16.75C21.07,15.5 22.27,13.86 23,12C21.27,7.61 17,4.5 12,4.5C10.73,4.5 9.5,4.74 8.36,5.17L10.18,7C10.74,6.87 11.36,6.8 12,7Z";

        private static readonly Dictionary<string, string> MapPresets = new Dictionary<string, string>
        {
            { "Italy", "/levels/italy/info.json" },
            { "West Coast, USA", "/levels/west_coast_usa/info.json" },
            { "East Coast, USA", "/levels/east_coast_usa/info.json" },
            { "Gridmap v2", "/levels/gridmap_v2/info.json" },
            { "Utah", "/levels/utah/info.json" },
            { "Jungle Rock Island", "/levels/jungle_rock_island/info.json" },
            { "Industrial Site", "/levels/industrial/info.json" },
            { "Hirochi Raceway", "/levels/hirochi_raceway/info.json" },
            { "Derby Arenas", "/levels/derby/info.json" },
            { "Small Island", "/levels/small_island/info.json" },
            { "Automation Test Track", "/levels/automation_test_track/info.json" },
            { "Cliff", "/levels/cliff/info.json" },
            { "Custom Map Path...", "" }
        };

        public MainWindow()
        {
            InitializeComponent();

            _baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configFilePath = Path.Combine(_baseDir, "ServerConfig.toml");
            _serverExePath = Path.Combine(_baseDir, "BeamMP-Server.exe");

            _serverManager = new ServerManager();
            _config = new TomlConfig();
            _modManager = new ModManager(_baseDir);

            lblFooterPath.Text = _baseDir;

            // Setup ServerManager events
            _serverManager.LogReceived += OnServerLogReceived;
            _serverManager.StatusChanged += OnServerStatusChanged;

            // Setup timers
            _uptimeTimer = new DispatcherTimer();
            _uptimeTimer.Interval = TimeSpan.FromSeconds(1);
            _uptimeTimer.Tick += UptimeTimer_Tick;
            _uptimeTimer.Start();

            _saveNoticeTimer = new DispatcherTimer();
            _saveNoticeTimer.Interval = TimeSpan.FromSeconds(3);
            _saveNoticeTimer.Tick += (s, e) =>
            {
                bannerSavedNotice.Visibility = Visibility.Collapsed;
                _saveNoticeTimer.Stop();
            };

            // Setup Map presets combo
            InitMapPresets();

            // Load configuration
            LoadConfigurationFromFile();

            // Refresh mods
            RefreshModsList();

            // Show dashboard by default
            ShowView("Dashboard");

            Closing += MainWindow_Closing;
        }

        private void InitMapPresets()
        {
            comboMapPresets.Items.Clear();
            foreach (var kvp in MapPresets)
            {
                comboMapPresets.Items.Add(kvp.Key);
            }
        }

        // ================= VIEW SWITCHING =================
        private void ShowView(string viewName)
        {
            viewDashboard.Visibility = (viewName == "Dashboard") ? Visibility.Visible : Visibility.Collapsed;
            viewConfig.Visibility = (viewName == "Config") ? Visibility.Visible : Visibility.Collapsed;
            viewMods.Visibility = (viewName == "Mods") ? Visibility.Visible : Visibility.Collapsed;

            // Update tab button highlights
            btnNavDashboard.Foreground = (viewName == "Dashboard") ? new SolidColorBrush(Color.FromRgb(255, 102, 0)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
            btnNavConfig.Foreground = (viewName == "Config") ? new SolidColorBrush(Color.FromRgb(255, 102, 0)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
            btnNavMods.Foreground = (viewName == "Mods") ? new SolidColorBrush(Color.FromRgb(255, 102, 0)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowView("Dashboard");
        }

        private void NavConfig_Click(object sender, RoutedEventArgs e)
        {
            ShowView("Config");
        }

        private void NavMods_Click(object sender, RoutedEventArgs e)
        {
            ShowView("Mods");
            RefreshModsList();
        }

        // ================= CONFIGURATION MANAGEMENT =================
        private void LoadConfigurationFromFile()
        {
            _config.Load(_configFilePath);

            // Populate form fields
            cfgName.Text = _config.Get("General", "Name", "BeamMP Server");
            cfgDescription.Text = _config.Get("General", "Description", "BeamMP Server Description");
            cfgTags.Text = _config.Get("General", "Tags", "Freeroam");

            string mapPath = _config.Get("General", "Map", "/levels/italy/info.json");
            cfgMap.Text = mapPath;
            SelectMapPresetForPath(mapPath);

            cfgMaxPlayers.Text = _config.GetInt("General", "MaxPlayers", 8).ToString();
            cfgMaxCars.Text = _config.GetInt("General", "MaxCars", 1).ToString();

            _actualAuthKey = _config.Get("General", "AuthKey", "");
            UpdateAuthKeyDisplay();

            cfgPort.Text = _config.GetInt("General", "Port", 30814).ToString();
            cfgIP.Text = _config.Get("General", "IP", "::");
            cfgResourceFolder.Text = _config.Get("General", "ResourceFolder", "Resources");

            cfgPrivate.IsChecked = _config.GetBool("General", "Private", false);
            cfgAllowGuests.IsChecked = _config.GetBool("General", "AllowGuests", true);
            cfgInformationPacket.IsChecked = _config.GetBool("General", "InformationPacket", true);
            cfgLogChat.IsChecked = _config.GetBool("General", "LogChat", true);
            cfgDebug.IsChecked = _config.GetBool("General", "Debug", false);

            cfgImScaredOfUpdates.IsChecked = _config.GetBool("Misc", "ImScaredOfUpdates", true);
            cfgUpdateReminderTime.Text = _config.Get("Misc", "UpdateReminderTime", "30s");

            UpdateDashboardSummary();
        }

        private void SelectMapPresetForPath(string mapPath)
        {
            string matchedPreset = "Custom Map Path...";
            foreach (var kvp in MapPresets)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && kvp.Value.Equals(mapPath, StringComparison.OrdinalIgnoreCase))
                {
                    matchedPreset = kvp.Key;
                    break;
                }
            }
            comboMapPresets.SelectedItem = matchedPreset;
        }

        private void ComboMapPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboMapPresets.SelectedItem == null) return;
            string selected = comboMapPresets.SelectedItem.ToString();
            string presetPath;
            if (MapPresets.TryGetValue(selected, out presetPath) && !string.IsNullOrEmpty(presetPath))
            {
                cfgMap.Text = presetPath;
            }
        }

        private void UpdateAuthKeyDisplay()
        {
            if (_isAuthKeyMasked)
            {
                if (string.IsNullOrEmpty(_actualAuthKey))
                {
                    cfgAuthKey.Text = "";
                }
                else
                {
                    cfgAuthKey.Text = new string('•', Math.Min(36, _actualAuthKey.Length));
                }
                iconAuthEye.Data = Geometry.Parse(IconEyeClosed);
                iconAuthEye.Fill = new SolidColorBrush(Color.FromRgb(139, 148, 158));
                btnToggleAuthKeyMask.ToolTip = "Show AuthKey";
            }
            else
            {
                cfgAuthKey.Text = _actualAuthKey;
                iconAuthEye.Data = Geometry.Parse(IconEyeOpen);
                iconAuthEye.Fill = new SolidColorBrush(Color.FromRgb(255, 102, 0));
                btnToggleAuthKeyMask.ToolTip = "Hide AuthKey";
            }
        }

        private void ToggleAuthKeyMask_Click(object sender, RoutedEventArgs e)
        {
            if (_isAuthKeyMasked)
            {
                _isAuthKeyMasked = false;
            }
            else
            {
                // Capture edited plaintext before masking
                _actualAuthKey = cfgAuthKey.Text.Trim();
                _isAuthKeyMasked = true;
            }
            UpdateAuthKeyDisplay();
        }

        private void OpenKeymaster_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://keymaster.beammp.com/");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Keymaster website: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            // If raw TOML is currently showing, save from raw editor
            if (gridRawTomlEditor.Visibility == Visibility.Visible)
            {
                try
                {
                    File.WriteAllText(_configFilePath, txtRawToml.Text, Encoding.UTF8);
                    LoadConfigurationFromFile();
                    ShowSavedBanner();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving raw TOML: " + ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // Sync auth key if currently unmasked
            if (!_isAuthKeyMasked)
            {
                _actualAuthKey = cfgAuthKey.Text.Trim();
            }

            // Validation
            int port;
            if (!int.TryParse(cfgPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Please enter a valid Port number (1 - 65535).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cfgPort.Focus();
                return;
            }

            int maxPlayers;
            if (!int.TryParse(cfgMaxPlayers.Text.Trim(), out maxPlayers) || maxPlayers < 1 || maxPlayers > 128)
            {
                MessageBox.Show("Max Players must be a number between 1 and 128.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cfgMaxPlayers.Focus();
                return;
            }

            int maxCars;
            if (!int.TryParse(cfgMaxCars.Text.Trim(), out maxCars) || maxCars < 1 || maxCars > 32)
            {
                MessageBox.Show("Max Cars Per Player must be between 1 and 32.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cfgMaxCars.Focus();
                return;
            }

            // Save to config object
            _config.Set("General", "Name", cfgName.Text.Trim());
            _config.Set("General", "Description", cfgDescription.Text.Trim());
            _config.Set("General", "Tags", cfgTags.Text.Trim());
            _config.Set("General", "Map", cfgMap.Text.Trim());
            _config.SetInt("General", "MaxPlayers", maxPlayers);
            _config.SetInt("General", "MaxCars", maxCars);
            _config.Set("General", "AuthKey", _actualAuthKey);
            _config.SetInt("General", "Port", port);
            _config.Set("General", "IP", cfgIP.Text.Trim());
            _config.Set("General", "ResourceFolder", cfgResourceFolder.Text.Trim());

            _config.SetBool("General", "Private", cfgPrivate.IsChecked == true);
            _config.SetBool("General", "AllowGuests", cfgAllowGuests.IsChecked == true);
            _config.SetBool("General", "InformationPacket", cfgInformationPacket.IsChecked == true);
            _config.SetBool("General", "LogChat", cfgLogChat.IsChecked == true);
            _config.SetBool("General", "Debug", cfgDebug.IsChecked == true);

            _config.SetBool("Misc", "ImScaredOfUpdates", cfgImScaredOfUpdates.IsChecked == true);
            _config.Set("Misc", "UpdateReminderTime", cfgUpdateReminderTime.Text.Trim());

            try
            {
                _config.Save(_configFilePath);
                UpdateDashboardSummary();
                ShowSavedBanner();

                // If server is currently running, offer restart
                if (_serverManager.IsRunning)
                {
                    MessageBoxResult res = MessageBox.Show(
                        "Configuration saved successfully!\n\nThe BeamMP Server is currently running. Would you like to restart the server now so the changes take effect?",
                        "Server Running",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        RestartServer();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving configuration to file:\n" + ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowSavedBanner()
        {
            bannerSavedNotice.Visibility = Visibility.Visible;
            _saveNoticeTimer.Stop();
            _saveNoticeTimer.Start();
        }

        private void ReloadConfig_Click(object sender, RoutedEventArgs e)
        {
            LoadConfigurationFromFile();
            if (gridRawTomlEditor.Visibility == Visibility.Visible && File.Exists(_configFilePath))
            {
                txtRawToml.Text = File.ReadAllText(_configFilePath, Encoding.UTF8);
            }
            MessageBox.Show("Configuration reloaded from ServerConfig.toml.", "Reloaded", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ToggleRawView_Click(object sender, RoutedEventArgs e)
        {
            if (gridRawTomlEditor.Visibility == Visibility.Collapsed)
            {
                // Switch to Raw View
                if (File.Exists(_configFilePath))
                {
                    txtRawToml.Text = File.ReadAllText(_configFilePath, Encoding.UTF8);
                }
                scrollConfigForm.Visibility = Visibility.Collapsed;
                gridRawTomlEditor.Visibility = Visibility.Visible;
                btnToggleRawView.Content = "📋  View Form Editor";
            }
            else
            {
                // Switch to Form View
                scrollConfigForm.Visibility = Visibility.Visible;
                gridRawTomlEditor.Visibility = Visibility.Collapsed;
                btnToggleRawView.Content = "🗎  View Raw TOML";
            }
        }

        private void UpdateDashboardSummary()
        {
            lblDashServerName.Text = _config.Get("General", "Name", "BeamMP Server");
            lblDashPort.Text = _config.Get("General", "Port", "30814");
            lblDashPlayers.Text = _config.Get("General", "MaxPlayers", "8") + " Clients";

            string map = _config.Get("General", "Map", "/levels/italy/info.json");
            string mapFriendly = map;
            foreach (var kvp in MapPresets)
            {
                if (kvp.Value.Equals(map, StringComparison.OrdinalIgnoreCase))
                {
                    mapFriendly = kvp.Key;
                    break;
                }
            }
            lblDashMap.Text = mapFriendly;

            string authKey = _config.Get("General", "AuthKey", "");
            if (string.IsNullOrEmpty(authKey) || authKey.Length < 10)
            {
                lblDashKeyStatus.Text = "⚠ Auth Key: Unset / Missing!";
                lblDashKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            else
            {
                lblDashKeyStatus.Text = "🔑 Auth Key: Configured (✓)";
                lblDashKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
        }

        // ================= SERVER CONTROLS & COMMANDS =================
        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_serverExePath))
            {
                MessageBox.Show(
                    string.Format("BeamMP-Server.exe was not found in:\n{0}\n\nPlease verify that BeamMP-Server.exe is in the server folder.", _baseDir),
                    "Executable Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Check AuthKey warning
            string authKey = _config.Get("General", "AuthKey", "");
            if (string.IsNullOrEmpty(authKey))
            {
                MessageBoxResult keyRes = MessageBox.Show(
                    "You have not configured an AuthKey yet!\nBeamMP requires an AuthKey to authenticate with the master server.\n\nDo you want to open the Configuration Editor to set your key now?",
                    "AuthKey Missing",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (keyRes == MessageBoxResult.Yes)
                {
                    ShowView("Config");
                    return;
                }
            }

            string err;
            bool success = _serverManager.StartServer(_serverExePath, _baseDir, out err);
            if (!success)
            {
                MessageBox.Show(err, "Failed to Start Server", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            _serverManager.StopServer();
        }

        private void RestartServer_Click(object sender, RoutedEventArgs e)
        {
            RestartServer();
        }

        private void RestartServer()
        {
            AppendConsoleLine("[SYSTEM] Restarting BeamMP Server...", false);
            _serverManager.StopServer();

            DispatcherTimer restartTimer = new DispatcherTimer();
            restartTimer.Interval = TimeSpan.FromMilliseconds(500);
            int attempts = 0;
            restartTimer.Tick += (s, args) =>
            {
                attempts++;
                if (!_serverManager.IsRunning || attempts > 10)
                {
                    restartTimer.Stop();
                    string err;
                    _serverManager.StartServer(_serverExePath, _baseDir, out err);
                }
            };
            restartTimer.Start();
        }

        private void SendCommand_Click(object sender, RoutedEventArgs e)
        {
            ExecuteUserCommand();
        }

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteUserCommand();
            }
        }

        private void ExecuteUserCommand()
        {
            string cmd = txtCommandInput.Text.Trim();
            if (string.IsNullOrEmpty(cmd)) return;

            if (!_serverManager.IsRunning)
            {
                AppendConsoleLine("[SYSTEM] Cannot send command: Server is not currently running.", true);
                txtCommandInput.Clear();
                return;
            }

            _serverManager.SendCommand(cmd);
            txtCommandInput.Clear();
        }

        // ================= CONSOLE COMMAND PRESETS =================
        private void PresetStatus_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("status", true);
        }

        private void PresetList_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("list", true);
        }

        private void PresetReloadMods_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("reloadmods", true);
        }

        private void PresetVersion_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("version", true);
        }

        private void PresetHelp_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("help", true);
        }

        private void PresetSay_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("say ", false);
        }

        private void PresetKick_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOrPreFillCommand("kick ", false);
        }

        private void ExecuteOrPreFillCommand(string cmd, bool autoExecute)
        {
            if (autoExecute)
            {
                if (!_serverManager.IsRunning)
                {
                    AppendConsoleLine("[SYSTEM] Cannot execute '" + cmd + "': Server is not currently running.", true);
                    return;
                }
                _serverManager.SendCommand(cmd);
            }
            else
            {
                txtCommandInput.Text = cmd;
                txtCommandInput.Focus();
                txtCommandInput.CaretIndex = txtCommandInput.Text.Length;
            }
        }

        private void ClearConsole_Click(object sender, RoutedEventArgs e)
        {
            txtConsoleOutput.Clear();
            _consoleLines = 0;
            txtConsoleLineCount.Text = "0 lines";
        }

        private void CopyConsole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(txtConsoleOutput.Text);
                MessageBox.Show("Console logs copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not copy to clipboard: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnServerLogReceived(string logLine, bool isError)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                AppendConsoleLine(logLine, isError);
            }));
        }

        private void AppendConsoleLine(string logLine, bool isError)
        {
            _consoleLines++;
            txtConsoleLineCount.Text = string.Format("{0} lines", _consoleLines);

            txtConsoleOutput.AppendText(logLine + Environment.NewLine);

            if (chkAutoScroll.IsChecked == true)
            {
                scrollConsole.ScrollToEnd();
            }
        }

        private void OnServerStatusChanged(ServerStatus status)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                UpdateServerStatusUI(status);
            }));
        }

        private void UpdateServerStatusUI(ServerStatus status)
        {
            switch (status)
            {
                case ServerStatus.Running:
                    btnStartServer.Visibility = Visibility.Collapsed;
                    panelRunningControls.Visibility = Visibility.Visible;

                    ellipseStatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    txtHeaderStatus.Text = string.Format("RUNNING (PID: {0})", _serverManager.ProcessId);
                    txtHeaderStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    borderStatusPill.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    lblFooterStatus.Text = "Server active";
                    lblFooterStatus.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    break;

                case ServerStatus.Starting:
                    btnStartServer.Visibility = Visibility.Visible;
                    btnStartServer.IsEnabled = false;
                    btnStartServer.Content = "STARTING...";
                    panelRunningControls.Visibility = Visibility.Collapsed;

                    ellipseStatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    txtHeaderStatus.Text = "STARTING...";
                    txtHeaderStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    borderStatusPill.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    break;

                case ServerStatus.Stopping:
                    panelRunningControls.Visibility = Visibility.Visible;
                    btnStopServer.IsEnabled = false;
                    btnStopServer.Content = "STOPPING...";

                    ellipseStatusDot.Fill = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    txtHeaderStatus.Text = "STOPPING...";
                    txtHeaderStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    break;

                case ServerStatus.Stopped:
                default:
                    btnStartServer.Visibility = Visibility.Visible;
                    btnStartServer.IsEnabled = true;
                    btnStartServer.Content = "▶   START SERVER";
                    btnStopServer.IsEnabled = true;
                    btnStopServer.Content = "⏹   STOP SERVER";
                    panelRunningControls.Visibility = Visibility.Collapsed;

                    ellipseStatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    txtHeaderStatus.Text = "OFFLINE";
                    txtHeaderStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    borderStatusPill.BorderBrush = new SolidColorBrush(Color.FromRgb(53, 58, 69));
                    txtHeaderUptime.Text = "";
                    lblFooterStatus.Text = "Ready";
                    lblFooterStatus.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                    break;
            }
        }

        private void UptimeTimer_Tick(object sender, EventArgs e)
        {
            if (_serverManager.IsRunning)
            {
                TimeSpan uptime = _serverManager.Uptime;
                txtHeaderUptime.Text = string.Format("| Uptime: {0:D2}:{1:D2}:{2:D2}", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);
            }
            else
            {
                txtHeaderUptime.Text = "";
            }
        }

        // ================= MODS MANAGEMENT =================
        private void OpenModsFolder_Click(object sender, RoutedEventArgs e)
        {
            _modManager.OpenModsFolder();
        }

        private void RefreshMods_Click(object sender, RoutedEventArgs e)
        {
            RefreshModsList();
        }

        private void RefreshModsList()
        {
            List<ModItem> mods = _modManager.GetInstalledMods();
            lstMods.ItemsSource = null;
            lstMods.ItemsSource = mods;

            long totalBytes = _modManager.GetTotalSizeBytes(mods);
            double totalMb = totalBytes / (1024.0 * 1024.0);

            string countStr = string.Format("{0} Mod{1}", mods.Count, mods.Count == 1 ? "" : "s");
            string sizeStr = string.Format("{0:F2} MB", totalMb);

            lblModsSummaryCount.Text = countStr;
            lblModsSummarySize.Text = sizeStr;

            lblDashModCount.Text = countStr + " Installed";
            lblDashModSize.Text = "Total size: " + sizeStr;
        }

        private void DeleteMod_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null) return;
            string path = btn.Tag.ToString();
            string name = Path.GetFileName(path);

            MessageBoxResult res = MessageBox.Show(
                string.Format("Are you sure you want to delete mod '{0}'?", name),
                "Delete Mod",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                string err;
                if (_modManager.DeleteMod(path, out err))
                {
                    RefreshModsList();
                }
                else
                {
                    MessageBox.Show("Failed to delete mod: " + err, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewMods_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ViewMods_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                int installed = 0;
                foreach (string file in files)
                {
                    if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        string err;
                        if (_modManager.InstallMod(file, out err))
                        {
                            installed++;
                        }
                    }
                }

                RefreshModsList();
                if (installed > 0)
                {
                    MessageBox.Show(string.Format("Installed {0} mod(s) successfully!", installed), "Mods Installed", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void OpenShortcutsSetup_Click(object sender, RoutedEventArgs e)
        {
            var setupWin = new BeamMP_GUI.SetupWindow(true);
            setupWin.Owner = this;
            setupWin.ShowDialog();
        }

        // ================= WINDOW CLOSING =================
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_serverManager.IsRunning)
            {
                MessageBoxResult res = MessageBox.Show(
                    "The BeamMP Server is currently running.\n\nDo you want to stop the server before exiting?",
                    "BeamMP Server Still Running",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (res == MessageBoxResult.Yes)
                {
                    _serverManager.StopServer();
                }
            }
        }
    }
}
