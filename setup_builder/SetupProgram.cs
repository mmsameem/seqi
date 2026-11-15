using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SEQI.Setup
{
    public class SetupForm : Form
    {
        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label descLabel;

        private Label destLabel;
        private TextBox txtDest;
        private Button btnBrowse;

        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkLaunch;

        private ProgressBar progressBar;
        private Label statusLabel;

        private Button btnInstall;
        private Button btnCancel;
        private Button btnFinish;

        private BackgroundWorker worker;
        private string targetDir;

        public SetupForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "SEQI - Quality Improvement System Setup";
            this.Size = new Size(580, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 246, 248);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
                {
                    if (stream != null)
                        this.Icon = new Icon(stream);
                }
            }
            catch { }

            // 1. Header Banner
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 85;
            headerPanel.BackColor = Color.FromArgb(18, 20, 24);

            titleLabel = new Label();
            titleLabel.Text = "SEQI - School Quality Assurance System";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(212, 175, 55); // Gold
            titleLabel.Location = new Point(20, 18);
            titleLabel.AutoSize = true;

            subtitleLabel = new Label();
            subtitleLabel.Text = "Standalone Desktop Application Setup Wizard";
            subtitleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            subtitleLabel.ForeColor = Color.FromArgb(180, 184, 192);
            subtitleLabel.Location = new Point(21, 46);
            subtitleLabel.AutoSize = true;

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);
            this.Controls.Add(headerPanel);

            // 2. Main content area
            descLabel = new Label();
            descLabel.Text = "This wizard will install the complete SEQI Institutional Evaluation System on your computer. All school audit data, evaluations, and photos will be stored securely on this local machine.";
            descLabel.Location = new Point(25, 105);
            descLabel.Size = new Size(515, 45);
            this.Controls.Add(descLabel);

            // 3. Destination folder
            destLabel = new Label();
            destLabel.Text = "Installation Folder:";
            destLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            destLabel.Location = new Point(25, 160);
            destLabel.AutoSize = true;
            this.Controls.Add(destLabel);

            txtDest = new TextBox();
            txtDest.Text = @"C:\SEQI";
            txtDest.Location = new Point(25, 185);
            txtDest.Size = new Size(410, 25);
            this.Controls.Add(txtDest);

            btnBrowse = new Button();
            btnBrowse.Text = "Browse...";
            btnBrowse.Location = new Point(445, 183);
            btnBrowse.Size = new Size(95, 28);
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Select Destination Folder for SEQI";
                    fbd.SelectedPath = txtDest.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtDest.Text = fbd.SelectedPath;
                    }
                }
            };
            this.Controls.Add(btnBrowse);

            // 4. Options
            chkDesktop = new CheckBox();
            chkDesktop.Text = "Create a Desktop shortcut (recommended)";
            chkDesktop.Checked = true;
            chkDesktop.Location = new Point(28, 230);
            chkDesktop.Size = new Size(450, 22);
            this.Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox();
            chkStartMenu.Text = "Create a Start Menu shortcut";
            chkStartMenu.Checked = true;
            chkStartMenu.Location = new Point(28, 255);
            chkStartMenu.Size = new Size(450, 22);
            this.Controls.Add(chkStartMenu);

            chkLaunch = new CheckBox();
            chkLaunch.Text = "Launch SEQI immediately after installation finishes";
            chkLaunch.Checked = true;
            chkLaunch.Location = new Point(28, 280);
            chkLaunch.Size = new Size(450, 22);
            this.Controls.Add(chkLaunch);

            // 5. Progress indicators (initially hidden)
            progressBar = new ProgressBar();
            progressBar.Location = new Point(28, 320);
            progressBar.Size = new Size(512, 24);
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;
            this.Controls.Add(progressBar);

            statusLabel = new Label();
            statusLabel.Text = "";
            statusLabel.Location = new Point(28, 350);
            statusLabel.Size = new Size(512, 20);
            statusLabel.ForeColor = Color.FromArgb(70, 75, 85);
            statusLabel.Visible = false;
            this.Controls.Add(statusLabel);

            // 6. Action buttons
            btnInstall = new Button();
            btnInstall.Text = "Install";
            btnInstall.Size = new Size(100, 32);
            btnInstall.Location = new Point(335, 390);
            btnInstall.BackColor = Color.FromArgb(212, 175, 55);
            btnInstall.ForeColor = Color.Black;
            btnInstall.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(95, 32);
            btnCancel.Location = new Point(445, 390);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            btnFinish = new Button();
            btnFinish.Text = "Finish";
            btnFinish.Size = new Size(100, 32);
            btnFinish.Location = new Point(445, 390);
            btnFinish.BackColor = Color.FromArgb(212, 175, 55);
            btnFinish.ForeColor = Color.Black;
            btnFinish.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnFinish.FlatStyle = FlatStyle.Flat;
            btnFinish.Visible = false;
            btnFinish.Click += BtnFinish_Click;
            this.Controls.Add(btnFinish);

            // 7. Background Worker
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            targetDir = txtDest.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Please choose a valid destination folder.", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Disable configuration controls
            txtDest.Enabled = false;
            btnBrowse.Enabled = false;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            chkLaunch.Enabled = false;
            btnInstall.Visible = false;
            btnCancel.Enabled = false;

            progressBar.Visible = true;
            statusLabel.Visible = true;
            statusLabel.Text = "Preparing installation...";

            worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            worker.ReportProgress(10, "Creating destination directories...");
            Directory.CreateDirectory(targetDir);

            worker.ReportProgress(25, "Extracting application package (this may take a minute)...");
            using (Stream zipStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SEQI.Payload.zip"))
            {
                if (zipStream == null)
                {
                    throw new Exception("Application payload could not be located inside the installer.");
                }

                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    int total = archive.Entries.Count;
                    int count = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string fullPath = Path.Combine(targetDir, entry.FullName);
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(fullPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            entry.ExtractToFile(fullPath, true);
                        }
                        count++;
                    }
                }
            }

            worker.ReportProgress(80, "Configuring local environment...");
            // Ensure .env exists in targetDir
            string envFile = Path.Combine(targetDir, ".env");
            if (!File.Exists(envFile))
            {
                string envExample = Path.Combine(targetDir, ".env.example");
                if (File.Exists(envExample))
                {
                    try { File.Copy(envExample, envFile); } catch { }
                }
                else
                {
                    string content = "NODE_ENV=production\nPORT=3000\nJWT_SECRET=seqi-app-" + Guid.NewGuid().ToString("N") + "\nDATABASE_PATH=database.db\nUPLOAD_DIR=uploads\nDEFAULT_ADMIN_USERNAME=admin\nDEFAULT_ADMIN_PASSWORD=admin123\n";
                    File.WriteAllText(envFile, content);
                }
            }

            worker.ReportProgress(90, "Creating application shortcuts...");
            string exePath = Path.Combine(targetDir, "SEQI.exe");
            string icoPath = Path.Combine(targetDir, "app.ico");

            if (chkDesktop.Checked)
            {
                CreateShortcut(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "SEQI - School Quality System.lnk",
                    exePath,
                    targetDir,
                    icoPath,
                    "SEQI - Institutional Quality Assurance System"
                );
            }

            if (chkStartMenu.Checked)
            {
                string startDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "SEQI");
                Directory.CreateDirectory(startDir);
                CreateShortcut(
                    startDir,
                    "SEQI - School Quality System.lnk",
                    exePath,
                    targetDir,
                    icoPath,
                    "SEQI - Institutional Quality Assurance System"
                );
            }

            // Create uninstaller batch
            string uninstallerPath = Path.Combine(targetDir, "Uninstall.bat");
            string uninstallScript = string.Format(
                "@echo off\r\necho Uninstalling SEQI...\r\ntaskkill /F /IM node.exe >nul 2>nul\r\ndel /f /q \"{0}\\SEQI - School Quality System.lnk\" >nul 2>nul\r\nrmdir /s /q \"{1}\\SEQI\" >nul 2>nul\r\necho Shortcuts removed.\r\necho To complete removal, delete this folder: {2}\r\npause\r\n",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                targetDir
            );
            try { File.WriteAllText(uninstallerPath, uninstallScript); } catch { }

            worker.ReportProgress(100, "Installation complete!");
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState != null)
            {
                statusLabel.Text = e.UserState.ToString();
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 100;

            if (e.Error != null)
            {
                MessageBox.Show("An error occurred during installation:\n" + e.Error.Message, "Installation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCancel.Enabled = true;
                return;
            }

            statusLabel.Text = "SEQI has been successfully installed on your computer!";
            statusLabel.ForeColor = Color.FromArgb(0, 120, 40);

            descLabel.Text = "Installation is complete! You can now launch SEQI from your Desktop or Start Menu.";

            btnCancel.Visible = false;
            btnFinish.Visible = true;
        }

        private void BtnFinish_Click(object sender, EventArgs e)
        {
            if (chkLaunch.Checked)
            {
                string exePath = Path.Combine(targetDir, "SEQI.exe");
                if (File.Exists(exePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = targetDir });
                    }
                    catch { }
                }
            }
            this.Close();
        }

        private static void CreateShortcut(string folder, string linkName, string targetPath, string workingDir, string iconPath, string desc)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(Path.Combine(folder, linkName));
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.Description = desc;
                    if (File.Exists(iconPath))
                    {
                        shortcut.IconLocation = iconPath + ",0";
                    }
                    shortcut.Save();
                }
            }
            catch { }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }
}

