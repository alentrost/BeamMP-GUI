using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace BeamMPServerManager
{
    public enum ServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping
    }

    public class ServerManager
    {
        private Process _process;
        private readonly object _lock = new object();
        private DateTime _startTime;

        public event Action<string, bool> LogReceived;
        public event Action<ServerStatus> StatusChanged;

        public ServerStatus Status { get; private set; }

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _process != null && !_process.HasExited;
                }
            }
        }

        public TimeSpan Uptime
        {
            get
            {
                if (IsRunning && _startTime != DateTime.MinValue)
                {
                    return DateTime.Now - _startTime;
                }
                return TimeSpan.Zero;
            }
        }

        public int ProcessId
        {
            get
            {
                lock (_lock)
                {
                    if (_process != null && !_process.HasExited)
                    {
                        try { return _process.Id; } catch { }
                    }
                    return 0;
                }
            }
        }

        public ServerManager()
        {
            Status = ServerStatus.Stopped;
        }

        public bool StartServer(string exePath, string workingDir, out string errorMessage)
        {
            errorMessage = string.Empty;

            lock (_lock)
            {
                if (IsRunning)
                {
                    errorMessage = "Server is already running.";
                    return false;
                }

                if (!File.Exists(exePath))
                {
                    errorMessage = string.Format("BeamMP-Server executable not found at:\n{0}", exePath);
                    return false;
                }

                try
                {
                    SetStatus(ServerStatus.Starting);

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = exePath;
                    psi.WorkingDirectory = workingDir;
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.RedirectStandardInput = true;
                    psi.CreateNoWindow = true;
                    psi.StandardOutputEncoding = Encoding.UTF8;
                    psi.StandardErrorEncoding = Encoding.UTF8;

                    _process = new Process();
                    _process.StartInfo = psi;
                    _process.EnableRaisingEvents = true;

                    _process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            RaiseLog(e.Data, false);
                        }
                    };

                    _process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            RaiseLog(e.Data, true);
                        }
                    };

                    _process.Exited += (s, e) =>
                    {
                        lock (_lock)
                        {
                            int exitCode = -1;
                            try { exitCode = _process.ExitCode; } catch { }
                            RaiseLog(string.Format("[SYSTEM] Server process terminated (Exit code: {0}).", exitCode), false);
                            _process = null;
                            _startTime = DateTime.MinValue;
                            SetStatus(ServerStatus.Stopped);
                        }
                    };

                    bool started = _process.Start();
                    if (!started)
                    {
                        errorMessage = "Failed to launch BeamMP-Server.exe process.";
                        SetStatus(ServerStatus.Stopped);
                        return false;
                    }

                    _startTime = DateTime.Now;
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();

                    SetStatus(ServerStatus.Running);
                    RaiseLog(string.Format("[SYSTEM] BeamMP Server started successfully (PID: {0}).", _process.Id), false);
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = "Error launching server: " + ex.Message;
                    SetStatus(ServerStatus.Stopped);
                    return false;
                }
            }
        }

        public void StopServer()
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                lock (_lock)
                {
                    if (_process == null || _process.HasExited)
                    {
                        SetStatus(ServerStatus.Stopped);
                        return;
                    }

                    SetStatus(ServerStatus.Stopping);
                    RaiseLog("[SYSTEM] Gracefully stopping BeamMP Server...", false);

                    try
                    {
                        // Send exit command to stdin
                        _process.StandardInput.WriteLine("exit");
                    }
                    catch { }
                }

                // Wait up to 3 seconds for exit
                bool exited = false;
                for (int i = 0; i < 30; i++)
                {
                    lock (_lock)
                    {
                        if (_process == null || _process.HasExited)
                        {
                            exited = true;
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }

                lock (_lock)
                {
                    if (!exited && _process != null && !_process.HasExited)
                    {
                        try
                        {
                            RaiseLog("[SYSTEM] Server did not stop in time, forcing termination...", true);
                            _process.Kill();
                        }
                        catch { }
                    }
                }
            }));
            t.IsBackground = true;
            t.Start();
        }

        public bool SendCommand(string command)
        {
            lock (_lock)
            {
                if (_process != null && !_process.HasExited)
                {
                    try
                    {
                        _process.StandardInput.WriteLine(command);
                        RaiseLog(string.Format("> {0}", command), false);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        RaiseLog(string.Format("[ERROR] Failed to send command: {0}", ex.Message), true);
                    }
                }
                return false;
            }
        }

        public static bool IsPortAvailable(int port)
        {
            try
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
                foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        return false;
                    }
                }

                System.Net.IPEndPoint[] tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
                foreach (System.Net.IPEndPoint ep in tcpListeners)
                {
                    if (ep.Port == port)
                    {
                        return false;
                    }
                }

                System.Net.IPEndPoint[] udpListeners = ipGlobalProperties.GetActiveUdpListeners();
                foreach (System.Net.IPEndPoint ep in udpListeners)
                {
                    if (ep.Port == port)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        private void SetStatus(ServerStatus newStatus)
        {
            Status = newStatus;
            Action<ServerStatus> handler = StatusChanged;
            if (handler != null)
            {
                handler(newStatus);
            }
        }

        private void RaiseLog(string line, bool isError)
        {
            Action<string, bool> handler = LogReceived;
            if (handler != null)
            {
                handler(line, isError);
            }
        }
    }
}
