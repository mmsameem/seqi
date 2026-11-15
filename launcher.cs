using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace SEQI.Launcher
{
    static class Program
    {
        private static Process serverProcess = null;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(appDir);

            // 1. Verify Node.js is installed
            if (!IsNodeInstalled())
            {
                MessageBox.Show(
                    "Node.js (version 18 or newer) was not found on this computer.\n\nPlease install Node.js from https://nodejs.org and launch SEQI again.",
                    "SEQI - Node.js Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // 2. Ensure .env exists
            EnsureConfigFile(appDir);

            // 3. Register cleanup on exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => CleanupServer();

            // 4. Check if SEQI server is already running on port 3000
            bool alreadyRunning = IsServerAlive("http://localhost:3000/api/health");

            if (!alreadyRunning)
            {
                // Start background Node server
                StartNodeServer(appDir);

                // Wait up to 15 seconds for server to be responsive
                bool ready = false;
                for (int i = 0; i < 30; i++)
                {
                    Thread.Sleep(500);
                    if (IsServerAlive("http://localhost:3000/api/health"))
                    {
                        ready = true;
                        break;
                    }
                }

                if (!ready)
                {
                    MessageBox.Show(
                        "SEQI server failed to start within 15 seconds.\nPlease verify node_modules and database.db, or check stderr.log.",
                        "SEQI - Server Start Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    CleanupServer();
                    return;
                }
            }

            // 5. Launch dedicated App-mode window
            LaunchAppWindow("http://localhost:3000", alreadyRunning);
        }

        private static bool IsNodeInstalled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("node", "-v")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(3000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureConfigFile(string appDir)
        {
            string envPath = Path.Combine(appDir, ".env");
            if (!File.Exists(envPath))
            {
                string examplePath = Path.Combine(appDir, ".env.example");
                if (File.Exists(examplePath))
                {
                    try { File.Copy(examplePath, envPath); } catch { }
                }
                else
                {
                    try
                    {
                        string content = "NODE_ENV=production\nPORT=3000\nJWT_SECRET=seqi-desktop-" + Guid.NewGuid().ToString("N") + "\nDATABASE_PATH=database.db\nUPLOAD_DIR=uploads\nDEFAULT_ADMIN_USERNAME=admin\nDEFAULT_ADMIN_PASSWORD=admin123\n";
                        File.WriteAllText(envPath, content);
                    }
                    catch { }
                }
            }
        }

        private static void StartNodeServer(string appDir)
        {
            string serverScript = Path.Combine(appDir, "server.js");
            if (!File.Exists(serverScript))
            {
                serverScript = Path.Combine(appDir, "dist-server", "server.js");
            }

            ProcessStartInfo psi = new ProcessStartInfo("node", "\"" + serverScript + "\"")
            {
                WorkingDirectory = appDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                serverProcess = Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start background server process: " + ex.Message, "SEQI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool IsServerAlive(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 1000;
                request.Method = "GET";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void LaunchAppWindow(string url, bool wasAlreadyRunning)
        {
            string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(edgePath))
            {
                edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            }

            string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (!File.Exists(chromePath))
            {
                chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
            }

            string browserExe = null;
            if (File.Exists(edgePath))
            {
                browserExe = edgePath;
            }
            else if (File.Exists(chromePath))
            {
                browserExe = chromePath;
            }

            if (browserExe != null)
            {
                // App mode: frameless browser with no address bar or tabs
                string args = string.Format("--app=\"{0}\" --window-size=1280,840", url);
                ProcessStartInfo psi = new ProcessStartInfo(browserExe, args);
                try
                {
                    Process browserProcess = Process.Start(psi);
                    if (browserProcess != null && !wasAlreadyRunning && serverProcess != null)
                    {
                        // Wait until the user closes the app window, then clean up background server
                        browserProcess.WaitForExit();
                        CleanupServer();
                    }
                    return;
                }
                catch { }
            }

            // Fallback: Default system browser
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        private static void CleanupServer()
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                try
                {
                    serverProcess.Kill();
                    serverProcess.Dispose();
                }
                catch { }
                finally
                {
                    serverProcess = null;
                }
            }
        }
    }
}

