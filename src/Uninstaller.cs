using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ClassRollCallUninstaller
{
    internal static class UninstallerProgram
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UninstallerForm());
        }
    }

    internal sealed class UninstallerForm : Form
    {
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Label statusLabel = new Label();
        private readonly Label titleLabel = new Label();
        private readonly Button uninstallButton = new Button();
        private readonly Button cancelButton = new Button();
        private readonly Timer animationTimer = new Timer();

        private int progressValue;
        private bool deleting;

        public UninstallerForm()
        {
            Text = "卸载班级点名器";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(460, 230);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(255, 248, 250);

            titleLabel.Text = "删除班级点名器";
            titleLabel.Font = new Font(Font.FontFamily, 18F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(65, 49, 40);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(28, 24);
            Controls.Add(titleLabel);

            Label hint = new Label();
            hint.Text = "将删除主程序、name 名单文件夹、桌面快捷方式和卸载程序。";
            hint.ForeColor = Color.FromArgb(124, 91, 68);
            hint.AutoSize = true;
            hint.Location = new Point(32, 70);
            Controls.Add(hint);

            progress.Bounds = new Rectangle(32, 112, 394, 18);
            progress.Minimum = 0;
            progress.Maximum = 100;
            Controls.Add(progress);

            statusLabel.Text = "准备删除";
            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.FromArgb(214, 105, 46);
            statusLabel.Location = new Point(32, 142);
            Controls.Add(statusLabel);

            uninstallButton.Text = "开始删除";
            uninstallButton.Bounds = new Rectangle(236, 178, 90, 34);
            uninstallButton.Click += UninstallButtonClick;
            Controls.Add(uninstallButton);

            cancelButton.Text = "取消";
            cancelButton.Bounds = new Rectangle(336, 178, 90, 34);
            cancelButton.Click += delegate { Close(); };
            Controls.Add(cancelButton);

            animationTimer.Interval = 28;
            animationTimer.Tick += AnimationTimerTick;
        }

        private void UninstallButtonClick(object sender, EventArgs e)
        {
            if (deleting)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "确认删除班级点名器吗？\n如果 name 文件夹里有你自己保存的名单，也会一起删除。",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            deleting = true;
            uninstallButton.Enabled = false;
            cancelButton.Enabled = false;
            progressValue = 0;
            progress.Value = 0;
            statusLabel.Text = "删除动画启动中...";
            animationTimer.Start();
        }

        private void AnimationTimerTick(object sender, EventArgs e)
        {
            progressValue += 3;
            if (progressValue < 100)
            {
                progress.Value = progressValue;
                int dots = (progressValue / 9) % 4;
                statusLabel.Text = "正在删除" + new string('.', dots);
                return;
            }

            animationTimer.Stop();
            progress.Value = 100;
            try
            {
                DeleteInstalledFiles();
                statusLabel.Text = "删除完成，窗口即将关闭。";
                Timer closeTimer = new Timer();
                closeTimer.Interval = 900;
                closeTimer.Tick += delegate
                {
                    closeTimer.Stop();
                    ScheduleSelfDelete();
                    Application.Exit();
                };
                closeTimer.Start();
            }
            catch (Exception ex)
            {
                deleting = false;
                uninstallButton.Enabled = true;
                cancelButton.Enabled = true;
                statusLabel.Text = "删除失败";
                MessageBox.Show(this, "删除失败：\n" + ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void DeleteInstalledFiles()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DeleteShortcut(baseDir);
            DeleteFile(Path.Combine(baseDir, "班级点名器.exe"));
            DeleteFile(Path.Combine(baseDir, "install.info"));

            string nameDir = Path.Combine(baseDir, "name");
            if (Directory.Exists(nameDir))
            {
                Directory.Delete(nameDir, true);
            }
        }

        private static void DeleteShortcut(string baseDir)
        {
            string shortcut = ReadShortcutPath(Path.Combine(baseDir, "install.info"));
            if (shortcut.Length > 0)
            {
                DeleteFile(shortcut);
            }

            DeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "班级点名器.lnk"));
            DeleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "班级点名器.lnk"));
        }

        private static string ReadShortcutPath(string infoPath)
        {
            if (!File.Exists(infoPath))
            {
                return string.Empty;
            }

            foreach (string line in File.ReadAllLines(infoPath, Encoding.UTF8))
            {
                if (line.StartsWith("ShortcutPath=", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring("ShortcutPath=".Length).Trim();
                }
            }
            return string.Empty;
        }

        private static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        private static void ScheduleSelfDelete()
        {
            string exePath = Application.ExecutablePath;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string command =
                "/C ping 127.0.0.1 -n 2 > nul & del /f /q \"" + exePath + "\" & rd \"" + baseDir + "\" 2> nul";

            ProcessStartInfo info = new ProcessStartInfo("cmd.exe", command);
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(info);
        }
    }
}
