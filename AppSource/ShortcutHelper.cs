using System;
using System.IO;

namespace BeamMP_GUI
{
    public static class ShortcutHelper
    {
        public static bool CreateShortcut(string targetExePath, string workingDir, string shortcutLocation, string shortcutName, out string error)
        {
            error = string.Empty;
            try
            {
                if (!Directory.Exists(shortcutLocation))
                {
                    Directory.CreateDirectory(shortcutLocation);
                }

                string linkPath = Path.Combine(shortcutLocation, shortcutName + ".lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    error = "WScript.Shell COM object not found.";
                    return false;
                }

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(linkPath);
                shortcut.TargetPath = targetExePath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Description = "BeamMP Server Manager & Configuration Suite";
                shortcut.IconLocation = targetExePath + ",0";
                shortcut.Save();

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string GetDesktopPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        public static string GetStartMenuPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        }

        public static string DetectDefaultServerPath()
        {
            // 1. Check current directory
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(currentDir, "BeamMP-Server.exe")))
            {
                return currentDir;
            }

            // 2. Check MyDocuments \ BeamMP Server
            string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string docsBeamMP = Path.Combine(myDocs, "BeamMP Server");
            if (File.Exists(Path.Combine(docsBeamMP, "BeamMP-Server.exe")))
            {
                return docsBeamMP;
            }

            // 3. Check OneDrive Documents if applicable
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string oneDriveDocs = Path.Combine(userProfile, "OneDrive", "Documents", "BeamMP Server");
            if (File.Exists(Path.Combine(oneDriveDocs, "BeamMP-Server.exe")))
            {
                return oneDriveDocs;
            }

            // Fallback to Documents\BeamMP Server
            return Directory.Exists(docsBeamMP) ? docsBeamMP : (Directory.Exists(oneDriveDocs) ? oneDriveDocs : myDocs);
        }
    }
}
