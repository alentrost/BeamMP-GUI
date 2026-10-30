using System;
using System.IO;
using System.Text;

namespace BeamMP_GUI
{
    public class AppSettings
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeamMP-GUI");
        private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

        public string ServerDirectory { get; set; }
        public bool IsInstalled { get; set; }
        public bool DesktopShortcut { get; set; }
        public bool StartMenuShortcut { get; set; }

        public AppSettings()
        {
            ServerDirectory = "";
            IsInstalled = false;
            DesktopShortcut = true;
            StartMenuShortcut = true;
        }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string content = File.ReadAllText(SettingsFile, Encoding.UTF8);
                    settings.ServerDirectory = ExtractJsonValue(content, "ServerDirectory");
                    settings.IsInstalled = ExtractJsonBool(content, "IsInstalled", false);
                    settings.DesktopShortcut = ExtractJsonBool(content, "DesktopShortcut", true);
                    settings.StartMenuShortcut = ExtractJsonBool(content, "StartMenuShortcut", true);
                }
            }
            catch { }
            return settings;
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                {
                    Directory.CreateDirectory(SettingsDir);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine(string.Format("  \"ServerDirectory\": \"{0}\",", (ServerDirectory ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")));
                sb.AppendLine(string.Format("  \"IsInstalled\": {0},", IsInstalled ? "true" : "false"));
                sb.AppendLine(string.Format("  \"DesktopShortcut\": {0},", DesktopShortcut ? "true" : "false"));
                sb.AppendLine(string.Format("  \"StartMenuShortcut\": {0}", StartMenuShortcut ? "true" : "false"));
                sb.AppendLine("}");

                File.WriteAllText(SettingsFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\":";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int start = json.IndexOf('\"', idx + pattern.Length);
                if (start >= 0)
                {
                    int end = json.IndexOf('\"', start + 1);
                    if (end > start)
                    {
                        return json.Substring(start + 1, end - start - 1).Replace("\\\\", "\\");
                    }
                }
            }
            return "";
        }

        private static bool ExtractJsonBool(string json, string key, bool def)
        {
            string pattern = "\"" + key + "\":";
            int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int start = idx + pattern.Length;
                int end = json.IndexOfAny(new char[] { ',', '}', '\r', '\n' }, start);
                if (end > start)
                {
                    string val = json.Substring(start, end - start).Trim().ToLowerInvariant();
                    if (val == "true") return true;
                    if (val == "false") return false;
                }
            }
            return def;
        }
    }
}
