using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace ClassRollCallInstaller
{
    internal static class InstallerProgram
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }

    internal sealed class InstallerForm : Form
    {
        private readonly TextBox pathBox = new TextBox();
        private readonly CheckBox shortcutCheck = new CheckBox();
        private readonly Button browseButton = new Button();
        private readonly Button installButton = new Button();
        private readonly Button closeButton = new Button();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Label statusLabel = new Label();
        private readonly Timer animationTimer = new Timer();

        private int progressValue;
        private bool installing;

        public InstallerForm()
        {
            Text = "班级点名器安装程序";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(560, 300);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(248, 252, 255);

            Label title = new Label();
            title.Text = "安装班级点名器";
            title.Font = new Font(Font.FontFamily, 20F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(43, 59, 82);
            title.AutoSize = true;
            title.Location = new Point(28, 24);
            Controls.Add(title);

            Label hint = new Label();
            hint.Text = "选择安装位置，安装器会自动放入主程序、name 名单文件夹和卸载程序。";
            hint.ForeColor = Color.FromArgb(92, 111, 139);
            hint.AutoSize = true;
            hint.Location = new Point(32, 68);
            Controls.Add(hint);

            Label pathLabel = new Label();
            pathLabel.Text = "安装位置";
            pathLabel.AutoSize = true;
            pathLabel.Location = new Point(32, 108);
            Controls.Add(pathLabel);

            pathBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "班级点名器");
            pathBox.Bounds = new Rectangle(32, 132, 410, 28);
            Controls.Add(pathBox);

            browseButton.Text = "选择...";
            browseButton.Bounds = new Rectangle(454, 131, 76, 30);
            browseButton.Click += BrowseButtonClick;
            Controls.Add(browseButton);

            shortcutCheck.Text = "创建桌面快捷方式";
            shortcutCheck.Checked = true;
            shortcutCheck.AutoSize = true;
            shortcutCheck.Location = new Point(32, 176);
            Controls.Add(shortcutCheck);

            progress.Bounds = new Rectangle(32, 212, 498, 16);
            progress.Minimum = 0;
            progress.Maximum = 100;
            Controls.Add(progress);

            statusLabel.Text = "准备安装";
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.FromArgb(73, 132, 205);
            statusLabel.Location = new Point(32, 238);
            Controls.Add(statusLabel);

            installButton.Text = "开始安装";
            installButton.Bounds = new Rectangle(340, 252, 90, 34);
            installButton.Click += InstallButtonClick;
            Controls.Add(installButton);

            closeButton.Text = "关闭";
            closeButton.Bounds = new Rectangle(440, 252, 90, 34);
            closeButton.Click += delegate { Close(); };
            Controls.Add(closeButton);

            animationTimer.Interval = 25;
            animationTimer.Tick += AnimationTimerTick;
        }

        private void BrowseButtonClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择安装位置";
                dialog.SelectedPath = Directory.Exists(pathBox.Text)
                    ? pathBox.Text
                    : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string selected = dialog.SelectedPath;
                    if (!string.Equals(Path.GetFileName(selected), "班级点名器", StringComparison.OrdinalIgnoreCase))
                    {
                        selected = Path.Combine(selected, "班级点名器");
                    }
                    pathBox.Text = selected;
                }
            }
        }

        private void InstallButtonClick(object sender, EventArgs e)
        {
            if (installing)
            {
                return;
            }

            string target = pathBox.Text.Trim();
            if (target.Length == 0)
            {
                MessageBox.Show(this, "请先选择安装位置。", "无法安装", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            installing = true;
            progressValue = 0;
            progress.Value = 0;
            installButton.Enabled = false;
            browseButton.Enabled = false;
            pathBox.Enabled = false;
            shortcutCheck.Enabled = false;
            statusLabel.Text = "正在安装...";
            animationTimer.Start();

            try
            {
                InstallTo(target, shortcutCheck.Checked);
                progress.Value = 100;
                statusLabel.Text = "安装完成，可以从安装位置或桌面快捷方式打开。";
                MessageBox.Show(this, "安装完成。", "班级点名器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "安装失败";
                MessageBox.Show(this, "安装失败：\n" + ex.Message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                animationTimer.Stop();
                installing = false;
                installButton.Enabled = true;
                browseButton.Enabled = true;
                pathBox.Enabled = true;
                shortcutCheck.Enabled = true;
            }
        }

        private void AnimationTimerTick(object sender, EventArgs e)
        {
            if (progressValue < 92)
            {
                progressValue += 2;
                progress.Value = progressValue;
            }
        }

        private void InstallTo(string target, bool createShortcut)
        {
            Directory.CreateDirectory(target);
            Directory.CreateDirectory(Path.Combine(target, "name"));

            string appPath = Path.Combine(target, "班级点名器.exe");
            string uninstallerPath = Path.Combine(target, "卸载班级点名器.exe");
            string defaultNamesPath = Path.Combine(target, "name", "默认名单_1-50.txt");
            string sampleNamesPath = Path.Combine(target, "name", "示例名单.txt");

            WriteResource("Payload.App.exe", appPath);
            WriteResource("Payload.Uninstaller.exe", uninstallerPath);
            WriteResource("Payload.DefaultNames.txt", defaultNamesPath);
            WriteResource("Payload.SampleNames.txt", sampleNamesPath);

            string shortcutPath = string.Empty;
            if (createShortcut)
            {
                shortcutPath = CreateDesktopShortcut(appPath, target);
            }

            string infoPath = Path.Combine(target, "install.info");
            StringBuilder info = new StringBuilder();
            info.AppendLine("AppName=班级点名器");
            info.AppendLine("InstallPath=" + target);
            info.AppendLine("ShortcutPath=" + shortcutPath);
            info.AppendLine("InstalledAt=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            File.WriteAllText(infoPath, info.ToString(), Encoding.UTF8);
        }

        private static void WriteResource(string resourceName, string outputPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream input = assembly.GetManifestResourceStream(resourceName))
            {
                if (input == null)
                {
                    throw new InvalidOperationException("安装资源缺失：" + resourceName);
                }

                using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(output);
                }
            }
        }

        private static string CreateDesktopShortcut(string appPath, string workingDirectory)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, "班级点名器.lnk");

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return string.Empty;
            }

            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { appPath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { appPath });
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "班级点名器" });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            return shortcutPath;
        }
    }
}
