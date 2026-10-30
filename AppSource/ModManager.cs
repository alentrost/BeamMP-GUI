using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BeamMPServerManager
{
    public class ModItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; }

        public string DisplaySize
        {
            get
            {
                if (SizeBytes < 1024)
                    return string.Format("{0} B", SizeBytes);
                if (SizeBytes < 1024 * 1024)
                    return string.Format("{0:F1} KB", SizeBytes / 1024.0);
                return string.Format("{0:F2} MB", SizeBytes / (1024.0 * 1024.0));
            }
        }

        public string DisplayDate
        {
            get
            {
                return LastModified.ToString("yyyy-MM-dd HH:mm");
            }
        }
    }

    public class ModManager
    {
        public string ModsDirectory { get; private set; }

        public ModManager(string baseDir)
        {
            ModsDirectory = Path.Combine(baseDir, "Resources", "Client");
        }

        public void EnsureDirectoryExists()
        {
            if (!Directory.Exists(ModsDirectory))
            {
                Directory.CreateDirectory(ModsDirectory);
            }
        }

        public bool OpenModsFolder()
        {
            try
            {
                EnsureDirectoryExists();
                Process.Start("explorer.exe", ModsDirectory);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<ModItem> GetInstalledMods()
        {
            List<ModItem> mods = new List<ModItem>();
            EnsureDirectoryExists();

            try
            {
                DirectoryInfo di = new DirectoryInfo(ModsDirectory);
                FileInfo[] files = di.GetFiles("*.zip", SearchOption.TopDirectoryOnly);

                foreach (FileInfo fi in files)
                {
                    ModItem item = new ModItem();
                    item.FileName = fi.Name;
                    item.FullPath = fi.FullName;
                    item.SizeBytes = fi.Length;
                    item.LastModified = fi.LastWriteTime;
                    mods.Add(item);
                }
            }
            catch
            {
            }

            return mods;
        }

        public long GetTotalSizeBytes(List<ModItem> mods)
        {
            long total = 0;
            foreach (ModItem m in mods)
            {
                total += m.SizeBytes;
            }
            return total;
        }

        public bool InstallMod(string sourceFilePath, out string error)
        {
            error = string.Empty;
            try
            {
                EnsureDirectoryExists();
                string fileName = Path.GetFileName(sourceFilePath);
                string destPath = Path.Combine(ModsDirectory, fileName);
                File.Copy(sourceFilePath, destPath, true);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool DeleteMod(string modPath, out string error)
        {
            error = string.Empty;
            try
            {
                if (File.Exists(modPath))
                {
                    File.Delete(modPath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
