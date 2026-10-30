using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace BeamMPServerManager
{
    public class TomlConfig
    {
        public Dictionary<string, Dictionary<string, string>> Sections;
        private List<string> _rawLines;

        public TomlConfig()
        {
            Sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _rawLines = new List<string>();
        }

        public void Load(string filePath)
        {
            Sections.Clear();
            _rawLines.Clear();

            string currentSection = "General";
            Sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(filePath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            _rawLines.AddRange(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!Sections.ContainsKey(currentSection))
                    {
                        Sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    continue;
                }

                int eqIdx = line.IndexOf('=');
                if (eqIdx > 0)
                {
                    string key = line.Substring(0, eqIdx).Trim();
                    string val = line.Substring(eqIdx + 1).Trim();

                    // Strip wrapping double quotes
                    if (val.StartsWith("\""))
                    {
                        int endQuote = val.LastIndexOf('\"');
                        if (endQuote > 0)
                        {
                            val = val.Substring(1, endQuote - 1);
                        }
                    }
                    else
                    {
                        // Strip trailing comments if any
                        int commentIdx = val.IndexOf('#');
                        if (commentIdx >= 0)
                        {
                            val = val.Substring(0, commentIdx).Trim();
                        }
                    }

                    if (!Sections.ContainsKey(currentSection))
                    {
                        Sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    Sections[currentSection][key] = val;
                }
            }
        }

        public string Get(string section, string key, string defaultValue)
        {
            Dictionary<string, string> sec;
            if (Sections.TryGetValue(section, out sec))
            {
                string val;
                if (sec.TryGetValue(key, out val))
                {
                    return val;
                }
            }
            return defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue)
        {
            string val = Get(section, key, defaultValue.ToString());
            int result;
            if (int.TryParse(val, out result))
            {
                return result;
            }
            return defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue)
        {
            string val = Get(section, key, defaultValue.ToString());
            bool result;
            if (bool.TryParse(val, out result))
            {
                return result;
            }
            return defaultValue;
        }

        public void Set(string section, string key, string value)
        {
            Dictionary<string, string> sec;
            if (!Sections.TryGetValue(section, out sec))
            {
                sec = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Sections[section] = sec;
            }
            sec[key] = value ?? "";
        }

        public void SetInt(string section, string key, int value)
        {
            Set(section, key, value.ToString());
        }

        public void SetBool(string section, string key, bool value)
        {
            Set(section, key, value ? "true" : "false");
        }

        public void Save(string filePath)
        {
            List<string> outputLines = new List<string>();

            if (File.Exists(filePath))
            {
                outputLines.AddRange(File.ReadAllLines(filePath, Encoding.UTF8));
            }
            else
            {
                // Create a standard template if file doesn't exist
                outputLines.Add("# This is the BeamMP-Server config file.");
                outputLines.Add("[General]");
                outputLines.Add("");
                outputLines.Add("[Misc]");
            }

            string currentSection = "General";
            HashSet<string> handledKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < outputLines.Count; i++)
            {
                string trimmed = outputLines[i].Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    continue;
                }

                if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                int eqIdx = outputLines[i].IndexOf('=');
                if (eqIdx > 0)
                {
                    string key = outputLines[i].Substring(0, eqIdx).Trim();
                    Dictionary<string, string> sec;
                    if (Sections.TryGetValue(currentSection, out sec))
                    {
                        string newVal;
                        if (sec.TryGetValue(key, out newVal))
                        {
                            string formatted = FormatTomlValue(key, newVal);
                            outputLines[i] = string.Format("{0} = {1}", key, formatted);
                            handledKeys.Add(currentSection + "." + key);
                        }
                    }
                }
            }

            // Append any new keys that were not in the file
            foreach (KeyValuePair<string, Dictionary<string, string>> secPair in Sections)
            {
                string secName = secPair.Key;
                foreach (KeyValuePair<string, string> kvp in secPair.Value)
                {
                    string fullKey = secName + "." + kvp.Key;
                    if (!handledKeys.Contains(fullKey))
                    {
                        // Find section header or append
                        int secIdx = FindSectionIndex(outputLines, secName);
                        string lineToAdd = string.Format("{0} = {1}", kvp.Key, FormatTomlValue(kvp.Key, kvp.Value));
                        if (secIdx >= 0)
                        {
                            outputLines.Insert(secIdx + 1, lineToAdd);
                        }
                        else
                        {
                            outputLines.Add(string.Format("[{0}]", secName));
                            outputLines.Add(lineToAdd);
                        }
                        handledKeys.Add(fullKey);
                    }
                }
            }

            // Write with UTF8 without BOM
            UTF8Encoding utf8NoBom = new UTF8Encoding(false);
            File.WriteAllLines(filePath, outputLines.ToArray(), utf8NoBom);
        }

        private static int FindSectionIndex(List<string> lines, string sectionName)
        {
            string target = string.Format("[{0}]", sectionName);
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static string FormatTomlValue(string key, string val)
        {
            if (val == null) return "\"\"";

            if (val.Equals("true", StringComparison.OrdinalIgnoreCase) || val.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return val.ToLowerInvariant();
            }

            long num;
            if (long.TryParse(val, out num))
            {
                // Keys that must be integers
                if (key.Equals("Port", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("MaxPlayers", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("MaxCars", StringComparison.OrdinalIgnoreCase))
                {
                    return val;
                }
            }

            return "\"" + val.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
