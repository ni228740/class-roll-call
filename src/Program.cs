using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClassRollCall
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(new RollCallState()));
        }
    }

    internal enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    internal sealed class ThemeInfo
    {
        public readonly string Name;
        public readonly Season Season;
        public readonly Color Top;
        public readonly Color Bottom;
        public readonly Color Card;
        public readonly Color CardAlt;
        public readonly Color Border;
        public readonly Color Text;
        public readonly Color Muted;
        public readonly Color Accent;
        public readonly Color Accent2;
        public readonly Color SoftAccent;

        public ThemeInfo(string name, Season season, Color top, Color bottom, Color card, Color cardAlt,
            Color border, Color text, Color muted, Color accent, Color accent2, Color softAccent)
        {
            Name = name;
            Season = season;
            Top = top;
            Bottom = bottom;
            Card = card;
            CardAlt = cardAlt;
            Border = border;
            Text = text;
            Muted = muted;
            Accent = accent;
            Accent2 = accent2;
            SoftAccent = softAccent;
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly ThemeInfo[] All =
        {
            new ThemeInfo(
                "春日樱花",
                Season.Spring,
                Color.FromArgb(255, 242, 248),
                Color.FromArgb(226, 248, 240),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(255, 246, 250),
                Color.FromArgb(244, 190, 213),
                Color.FromArgb(60, 46, 62),
                Color.FromArgb(126, 103, 126),
                Color.FromArgb(226, 92, 142),
                Color.FromArgb(67, 168, 138),
                Color.FromArgb(255, 214, 231)),
            new ThemeInfo(
                "夏日海风",
                Season.Summer,
                Color.FromArgb(230, 249, 255),
                Color.FromArgb(255, 244, 205),
                Color.FromArgb(255, 255, 250),
                Color.FromArgb(236, 251, 255),
                Color.FromArgb(149, 213, 226),
                Color.FromArgb(36, 63, 78),
                Color.FromArgb(89, 116, 128),
                Color.FromArgb(15, 151, 176),
                Color.FromArgb(239, 169, 55),
                Color.FromArgb(192, 238, 246)),
            new ThemeInfo(
                "秋日枫糖",
                Season.Autumn,
                Color.FromArgb(255, 245, 227),
                Color.FromArgb(238, 237, 255),
                Color.FromArgb(255, 253, 248),
                Color.FromArgb(255, 247, 235),
                Color.FromArgb(226, 183, 128),
                Color.FromArgb(65, 49, 40),
                Color.FromArgb(124, 91, 68),
                Color.FromArgb(214, 105, 46),
                Color.FromArgb(93, 105, 184),
                Color.FromArgb(255, 218, 170)),
            new ThemeInfo(
                "冬日初雪",
                Season.Winter,
                Color.FromArgb(238, 247, 255),
                Color.FromArgb(245, 242, 255),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(241, 247, 255),
                Color.FromArgb(179, 204, 231),
                Color.FromArgb(43, 59, 82),
                Color.FromArgb(92, 111, 139),
                Color.FromArgb(73, 132, 205),
                Color.FromArgb(188, 113, 173),
                Color.FromArgb(215, 235, 255))
        };
    }

    internal sealed class RollCallState
    {
        private readonly Random random = new Random();
        private readonly List<string> names = new List<string>();
        private readonly List<string> remaining = new List<string>();
        private readonly List<string> history = new List<string>();

        public RollCallState()
        {
            SetNames(DefaultNames());
            WithReplacement = true;
        }

        public bool WithReplacement { get; set; }

        public IList<string> Names
        {
            get { return names.AsReadOnly(); }
        }

        public IList<string> History
        {
            get { return history.AsReadOnly(); }
        }

        public int RemainingCount
        {
            get { return WithReplacement ? names.Count : remaining.Count; }
        }

        public void SetNames(IEnumerable<string> nextNames)
        {
            List<string> cleaned = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            foreach (string raw in nextNames)
            {
                string name = (raw ?? string.Empty).Trim();
                if (name.Length == 0 || seen.Contains(name))
                {
                    continue;
                }
                cleaned.Add(name);
                seen.Add(name);
            }

            if (cleaned.Count == 0)
            {
                cleaned.AddRange(DefaultNames());
            }

            if (names.SequenceEqual(cleaned))
            {
                return;
            }

            names.Clear();
            names.AddRange(cleaned);
            ResetRemaining();
        }

        public void ResetRemaining()
        {
            remaining.Clear();
            remaining.AddRange(names);
        }

        public void ClearHistory()
        {
            history.Clear();
        }

        public string PeekRandom()
        {
            IList<string> pool = WithReplacement ? names : remaining;
            if (pool.Count == 0)
            {
                return WithReplacement ? "空名单" : "已点完";
            }
            return pool[random.Next(pool.Count)];
        }

        public string DrawOne()
        {
            string selected;
            if (!TryDraw(out selected))
            {
                return "已经全部点完";
            }
            return selected;
        }

        public string PickLuckyStar()
        {
            if (names.Count == 0)
            {
                return "空名单";
            }

            string selected = names[random.Next(names.Count)];
            history.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  幸运星：" + selected);
            TrimHistory();
            return selected;
        }

        public List<string> DrawMany(int count)
        {
            List<string> results = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string selected;
                if (!TryDraw(out selected))
                {
                    break;
                }
                results.Add(selected);
            }
            return results;
        }

        public List<List<string>> BuildGroups(int groupSize)
        {
            List<string> shuffled = new List<string>(names);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            List<List<string>> groups = new List<List<string>>();
            for (int i = 0; i < shuffled.Count; i += groupSize)
            {
                groups.Add(shuffled.GetRange(i, Math.Min(groupSize, shuffled.Count - i)));
            }
            return groups;
        }

        private bool TryDraw(out string selected)
        {
            selected = string.Empty;
            if (names.Count == 0)
            {
                return false;
            }

            if (WithReplacement)
            {
                selected = names[random.Next(names.Count)];
            }
            else
            {
                if (remaining.Count == 0)
                {
                    return false;
                }
                int index = random.Next(remaining.Count);
                selected = remaining[index];
                remaining.RemoveAt(index);
            }

            history.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + selected);
            TrimHistory();
            return true;
        }

        private void TrimHistory()
        {
            while (history.Count > 80)
            {
                history.RemoveAt(history.Count - 1);
            }
        }

        public static List<string> DefaultNames()
        {
            List<string> defaults = new List<string>();
            for (int i = 1; i <= 50; i++)
            {
                defaults.Add(i.ToString());
            }
            return defaults;
        }

        public static List<string> ParseNames(string text)
        {
            char[] separators = { '\r', '\n', ',', '，', ';', '；', '\t' };
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            foreach (string part in (text ?? string.Empty).Split(separators, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = part.Trim();
                if (name.Length == 0 || seen.Contains(name))
                {
                    continue;
                }
                result.Add(name);
                seen.Add(name);
            }
            return result;
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly RollCallState state;
        private readonly Timer rollTimer = new Timer();
        private readonly Timer countdownTimer = new Timer();
        private Font resultFontLarge;
        private Font resultFontMedium;
        private Font resultFontSmall;

        private ThemeInfo theme = ThemeInfo.All[0];
        private int rollFrame;
        private int countdownValue;
        private bool rolling;
        private bool loadingList;

        private Label titleLabel;
        private Label subtitleLabel;
        private ComboBox themeCombo;
        private Button miniButton;
        private Button exitButton;

        private CardPanel settingsPanel;
        private CardPanel drawPanel;
        private CardPanel funPanel;

        private Label listLabel;
        private ComboBox listCombo;
        private Button refreshButton;
        private Button importButton;
        private Button saveButton;
        private Button defaultButton;
        private Button applyButton;
        private Label namesLabel;
        private TextBox namesBox;
        private Label countLabel;

        private Label drawTitleLabel;
        private RadioButton withReplacementRadio;
        private RadioButton withoutReplacementRadio;
        private Label resultLabel;
        private Label poolLabel;
        private Button startButton;
        private Button resetButton;

        private Label funTitleLabel;
        private Label multiLabel;
        private NumericUpDown multiCount;
        private Button multiButton;
        private Label groupLabel;
        private NumericUpDown groupSize;
        private Button groupButton;
        private Button luckyButton;
        private Button countdownButton;
        private Label historyLabel;
        private ListBox historyList;
        private Button clearHistoryButton;

        public MainForm(RollCallState rollCallState)
        {
            state = rollCallState;
            Text = "班级点名器";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            Size = new Size(1000, 660);
            Font = new Font("Microsoft YaHei UI", 9F);
            resultFontLarge = new Font(Font.FontFamily, 48F, FontStyle.Bold);
            resultFontMedium = new Font(Font.FontFamily, 34F, FontStyle.Bold);
            resultFontSmall = new Font(Font.FontFamily, 24F, FontStyle.Bold);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            CreateControls();
            EnsureDataFiles();
            RefreshListCombo();
            SetNamesBox(RollCallState.DefaultNames());
            ApplyTheme(ThemeInfo.All[0]);
            LayoutControls();
            UpdateStats();

            rollTimer.Interval = 40;
            rollTimer.Tick += RollTimerTick;
            countdownTimer.Interval = 760;
            countdownTimer.Tick += CountdownTimerTick;

            Resize += delegate { LayoutControls(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            SeasonPainter.Paint(e.Graphics, ClientRectangle, theme, false);
        }

        private void CreateControls()
        {
            titleLabel = new Label();
            titleLabel.Text = "班级点名器";
            titleLabel.AutoSize = true;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.Font = new Font(Font.FontFamily, 22F, FontStyle.Bold);

            subtitleLabel = new Label();
            subtitleLabel.Text = "随机、公平、轻量；适合课堂投屏时快速点名";
            subtitleLabel.AutoSize = true;
            subtitleLabel.BackColor = Color.Transparent;
            subtitleLabel.Font = new Font(Font.FontFamily, 9.5F);

            themeCombo = new ComboBox();
            themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            themeCombo.Items.AddRange(ThemeInfo.All);
            themeCombo.SelectedIndex = 0;
            themeCombo.SelectedIndexChanged += delegate
            {
                ThemeInfo selected = themeCombo.SelectedItem as ThemeInfo;
                if (selected != null)
                {
                    ApplyTheme(selected);
                }
            };

            miniButton = new Button();
            miniButton.Text = "缩小模式";
            miniButton.Click += delegate { OpenMiniMode(); };

            exitButton = new Button();
            exitButton.Text = "退出程序";
            exitButton.Click += delegate { Application.Exit(); };

            settingsPanel = new CardPanel();
            drawPanel = new CardPanel();
            funPanel = new CardPanel();

            listLabel = MakeLabel("选择名单");
            listCombo = new ComboBox();
            listCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            listCombo.SelectedIndexChanged += delegate
            {
                if (!loadingList)
                {
                    LoadSelectedList();
                }
            };

            refreshButton = new Button();
            refreshButton.Text = "刷新";
            refreshButton.Click += delegate { RefreshListCombo(); };

            importButton = new Button();
            importButton.Text = "导入 txt";
            importButton.Click += delegate { ImportNames(); };

            saveButton = new Button();
            saveButton.Text = "保存名单";
            saveButton.Click += delegate { SaveNames(); };

            defaultButton = new Button();
            defaultButton.Text = "默认 1-50";
            defaultButton.Click += delegate
            {
                SetNamesBox(RollCallState.DefaultNames());
                ApplyNamesFromBox(true);
            };

            applyButton = new Button();
            applyButton.Text = "应用编辑";
            applyButton.Click += delegate { ApplyNamesFromBox(true); };

            namesLabel = MakeLabel("编辑名单（一行一个，也支持逗号分隔）");
            namesBox = new TextBox();
            namesBox.Multiline = true;
            namesBox.ScrollBars = ScrollBars.Vertical;
            namesBox.BorderStyle = BorderStyle.FixedSingle;
            namesBox.Font = new Font("Microsoft YaHei UI", 10F);
            namesBox.TextChanged += delegate
            {
                if (!loadingList)
                {
                    UpdateNamePreview();
                }
            };

            countLabel = MakeLabel(string.Empty);

            drawTitleLabel = MakeLabel("今日登场", 13F, FontStyle.Bold);
            withReplacementRadio = new RadioButton();
            withReplacementRadio.Text = "放回点名";
            withReplacementRadio.Checked = true;
            withReplacementRadio.CheckedChanged += delegate
            {
                if (withReplacementRadio.Checked)
                {
                    state.WithReplacement = true;
                    UpdateStats();
                }
            };

            withoutReplacementRadio = new RadioButton();
            withoutReplacementRadio.Text = "不放回点名";
            withoutReplacementRadio.CheckedChanged += delegate
            {
                if (withoutReplacementRadio.Checked)
                {
                    state.WithReplacement = false;
                    state.ResetRemaining();
                    UpdateStats();
                }
            };

            resultLabel = new Label();
            resultLabel.Text = "准备开始";
            resultLabel.TextAlign = ContentAlignment.MiddleCenter;
            resultLabel.BackColor = Color.Transparent;
            resultLabel.Font = new Font(Font.FontFamily, 46F, FontStyle.Bold);

            poolLabel = MakeLabel(string.Empty, 10F, FontStyle.Regular);
            poolLabel.TextAlign = ContentAlignment.MiddleCenter;

            startButton = new Button();
            startButton.Text = "开始点名";
            startButton.Font = new Font(Font.FontFamily, 14F, FontStyle.Bold);
            startButton.Click += delegate { StartRoll(); };

            resetButton = new Button();
            resetButton.Text = "重置未点名单";
            resetButton.Click += delegate
            {
                state.ResetRemaining();
                UpdateStats();
                resultLabel.Text = "已重置";
            };

            funTitleLabel = MakeLabel("趣味玩法", 13F, FontStyle.Bold);

            multiLabel = MakeLabel("连抽人数");
            multiCount = new NumericUpDown();
            multiCount.Minimum = 1;
            multiCount.Maximum = 20;
            multiCount.Value = 3;

            multiButton = new Button();
            multiButton.Text = "连抽";
            multiButton.Click += delegate { DrawMany(); };

            groupLabel = MakeLabel("每组人数");
            groupSize = new NumericUpDown();
            groupSize.Minimum = 2;
            groupSize.Maximum = 12;
            groupSize.Value = 4;

            groupButton = new Button();
            groupButton.Text = "随机分组";
            groupButton.Click += delegate { ShowGroups(); };

            luckyButton = new Button();
            luckyButton.Text = "幸运星";
            luckyButton.Click += delegate { DrawLuckyStar(); };

            countdownButton = new Button();
            countdownButton.Text = "3 秒倒计时";
            countdownButton.Click += delegate { StartCountdown(); };

            historyLabel = MakeLabel("点名记录");
            historyList = new ListBox();
            historyList.BorderStyle = BorderStyle.FixedSingle;

            clearHistoryButton = new Button();
            clearHistoryButton.Text = "清空记录";
            clearHistoryButton.Click += delegate
            {
                state.ClearHistory();
                UpdateHistoryList();
            };

            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(themeCombo);
            Controls.Add(miniButton);
            Controls.Add(exitButton);
            Controls.Add(settingsPanel);
            Controls.Add(drawPanel);
            Controls.Add(funPanel);

            AddSettingsControls();
            AddDrawControls();
            AddFunControls();
        }

        private void AddSettingsControls()
        {
            settingsPanel.Controls.Add(listLabel);
            settingsPanel.Controls.Add(listCombo);
            settingsPanel.Controls.Add(refreshButton);
            settingsPanel.Controls.Add(importButton);
            settingsPanel.Controls.Add(saveButton);
            settingsPanel.Controls.Add(defaultButton);
            settingsPanel.Controls.Add(applyButton);
            settingsPanel.Controls.Add(namesLabel);
            settingsPanel.Controls.Add(namesBox);
            settingsPanel.Controls.Add(countLabel);
        }

        private void AddDrawControls()
        {
            drawPanel.Controls.Add(drawTitleLabel);
            drawPanel.Controls.Add(withReplacementRadio);
            drawPanel.Controls.Add(withoutReplacementRadio);
            drawPanel.Controls.Add(resultLabel);
            drawPanel.Controls.Add(poolLabel);
            drawPanel.Controls.Add(startButton);
            drawPanel.Controls.Add(resetButton);
        }

        private void AddFunControls()
        {
            funPanel.Controls.Add(funTitleLabel);
            funPanel.Controls.Add(multiLabel);
            funPanel.Controls.Add(multiCount);
            funPanel.Controls.Add(multiButton);
            funPanel.Controls.Add(groupLabel);
            funPanel.Controls.Add(groupSize);
            funPanel.Controls.Add(groupButton);
            funPanel.Controls.Add(luckyButton);
            funPanel.Controls.Add(countdownButton);
            funPanel.Controls.Add(historyLabel);
            funPanel.Controls.Add(historyList);
            funPanel.Controls.Add(clearHistoryButton);
        }

        private Label MakeLabel(string text)
        {
            return MakeLabel(text, 9F, FontStyle.Regular);
        }

        private Label MakeLabel(string text, float size, FontStyle style)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.BackColor = Color.Transparent;
            label.Font = new Font(Font.FontFamily, size, style);
            return label;
        }

        private void LayoutControls()
        {
            int margin = 18;
            int gap = 14;
            int headerHeight = 78;
            int width = ClientSize.Width;
            int height = ClientSize.Height;
            if (width < 1 || height < 1)
            {
                return;
            }

            titleLabel.Location = new Point(margin + 6, 17);
            subtitleLabel.Location = new Point(margin + 9, 57);
            exitButton.Bounds = new Rectangle(width - margin - 88, 24, 88, 34);
            miniButton.Bounds = new Rectangle(exitButton.Left - 118, 24, 110, 34);
            themeCombo.Bounds = new Rectangle(miniButton.Left - 150, 25, 136, 32);

            int top = margin + headerHeight;
            int contentHeight = height - top - margin;
            int leftWidth = Math.Max(275, width * 30 / 100);
            int rightWidth = Math.Max(245, width * 25 / 100);
            int centerWidth = width - margin * 2 - gap * 2 - leftWidth - rightWidth;
            if (centerWidth < 260)
            {
                centerWidth = 260;
                int overflow = margin * 2 + gap * 2 + leftWidth + rightWidth + centerWidth - width;
                leftWidth = Math.Max(250, leftWidth - overflow / 2);
                rightWidth = Math.Max(225, rightWidth - overflow / 2);
            }

            settingsPanel.Bounds = new Rectangle(margin, top, leftWidth, contentHeight);
            drawPanel.Bounds = new Rectangle(settingsPanel.Right + gap, top, centerWidth, contentHeight);
            funPanel.Bounds = new Rectangle(drawPanel.Right + gap, top, width - drawPanel.Right - gap - margin, contentHeight);

            LayoutSettingsPanel();
            LayoutDrawPanel();
            LayoutFunPanel();
        }

        private void LayoutSettingsPanel()
        {
            int pad = 16;
            int w = settingsPanel.ClientSize.Width;
            int h = settingsPanel.ClientSize.Height;
            int y = 18;

            listLabel.Location = new Point(pad, y);
            y += 24;
            listCombo.Bounds = new Rectangle(pad, y, w - pad * 2 - 70, 30);
            refreshButton.Bounds = new Rectangle(w - pad - 62, y, 62, 30);
            y += 42;

            int buttonW = (w - pad * 2 - 8) / 2;
            importButton.Bounds = new Rectangle(pad, y, buttonW, 32);
            saveButton.Bounds = new Rectangle(pad + buttonW + 8, y, buttonW, 32);
            y += 40;
            defaultButton.Bounds = new Rectangle(pad, y, buttonW, 32);
            applyButton.Bounds = new Rectangle(pad + buttonW + 8, y, buttonW, 32);
            y += 48;

            namesLabel.Location = new Point(pad, y);
            y += 26;
            int countHeight = 28;
            namesBox.Bounds = new Rectangle(pad, y, w - pad * 2, Math.Max(120, h - y - countHeight - pad));
            countLabel.Location = new Point(pad, h - countHeight - 2);
        }

        private void LayoutDrawPanel()
        {
            int pad = 18;
            int w = drawPanel.ClientSize.Width;
            int h = drawPanel.ClientSize.Height;

            drawTitleLabel.Location = new Point(pad, 17);
            withReplacementRadio.Bounds = new Rectangle(pad, 52, 108, 28);
            withoutReplacementRadio.Bounds = new Rectangle(pad + 118, 52, 130, 28);

            resultLabel.Bounds = new Rectangle(pad, 93, w - pad * 2, Math.Max(160, h - 245));
            poolLabel.Bounds = new Rectangle(pad, resultLabel.Bottom + 2, w - pad * 2, 30);
            startButton.Bounds = new Rectangle(pad, h - 96, w - pad * 2, 46);
            resetButton.Bounds = new Rectangle(pad, h - 42, w - pad * 2, 30);
            FitResultFont(resultLabel.Text);
        }

        private void LayoutFunPanel()
        {
            int pad = 16;
            int w = funPanel.ClientSize.Width;
            int h = funPanel.ClientSize.Height;
            int y = 18;

            funTitleLabel.Location = new Point(pad, y);
            y += 42;

            multiLabel.Location = new Point(pad, y + 5);
            multiCount.Bounds = new Rectangle(w - pad - 62, y, 62, 28);
            y += 34;
            multiButton.Bounds = new Rectangle(pad, y, w - pad * 2, 32);
            y += 43;

            groupLabel.Location = new Point(pad, y + 5);
            groupSize.Bounds = new Rectangle(w - pad - 62, y, 62, 28);
            y += 34;
            groupButton.Bounds = new Rectangle(pad, y, w - pad * 2, 32);
            y += 43;

            luckyButton.Bounds = new Rectangle(pad, y, w - pad * 2, 34);
            y += 42;
            countdownButton.Bounds = new Rectangle(pad, y, w - pad * 2, 34);
            y += 50;

            historyLabel.Location = new Point(pad, y);
            y += 26;
            clearHistoryButton.Bounds = new Rectangle(pad, h - 42, w - pad * 2, 30);
            historyList.Bounds = new Rectangle(pad, y, w - pad * 2, Math.Max(95, h - y - 52));
        }

        private void ApplyTheme(ThemeInfo nextTheme)
        {
            theme = nextTheme;
            BackColor = theme.Bottom;
            ForeColor = theme.Text;

            titleLabel.ForeColor = theme.Text;
            subtitleLabel.ForeColor = theme.Muted;
            StyleCombo(themeCombo);
            StyleButton(miniButton, true);
            StyleButton(exitButton, false);

            ApplyCardTheme(settingsPanel);
            ApplyCardTheme(drawPanel);
            ApplyCardTheme(funPanel);

            Label[] labels =
            {
                listLabel, namesLabel, countLabel, drawTitleLabel, poolLabel, funTitleLabel,
                multiLabel, groupLabel, historyLabel
            };
            foreach (Label label in labels)
            {
                label.ForeColor = label == countLabel || label == poolLabel ? theme.Muted : theme.Text;
            }

            withReplacementRadio.ForeColor = theme.Text;
            withoutReplacementRadio.ForeColor = theme.Text;
            withReplacementRadio.BackColor = Color.Transparent;
            withoutReplacementRadio.BackColor = Color.Transparent;

            resultLabel.ForeColor = theme.Accent;
            namesBox.BackColor = theme.CardAlt;
            namesBox.ForeColor = theme.Text;
            historyList.BackColor = theme.CardAlt;
            historyList.ForeColor = theme.Text;

            StyleCombo(listCombo);
            StyleButton(refreshButton, false);
            StyleButton(importButton, false);
            StyleButton(saveButton, true);
            StyleButton(defaultButton, false);
            StyleButton(applyButton, true);
            StyleButton(startButton, true);
            StyleButton(resetButton, false);
            StyleButton(multiButton, true);
            StyleButton(groupButton, true);
            StyleButton(luckyButton, false);
            StyleButton(countdownButton, false);
            StyleButton(clearHistoryButton, false);

            multiCount.BackColor = theme.CardAlt;
            multiCount.ForeColor = theme.Text;
            groupSize.BackColor = theme.CardAlt;
            groupSize.ForeColor = theme.Text;

            Icon = SeasonIcons.Get(theme.Season);
            Invalidate(true);
        }

        private void ApplyCardTheme(CardPanel panel)
        {
            panel.Theme = theme;
            panel.BackColor = Color.Transparent;
            panel.Invalidate();
        }

        private void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = primary ? theme.Accent : theme.SoftAccent;
            button.ForeColor = primary ? Color.White : theme.Text;
            button.Cursor = Cursors.Hand;
            button.Font = button == startButton
                ? new Font(Font.FontFamily, 14F, FontStyle.Bold)
                : new Font(Font.FontFamily, 9F, FontStyle.Bold);
        }

        private void StyleCombo(ComboBox combo)
        {
            combo.BackColor = theme.CardAlt;
            combo.ForeColor = theme.Text;
            combo.Font = new Font(Font.FontFamily, 9F);
        }

        private void EnsureDataFiles()
        {
            string dataDir = GetDataDirectory();
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            string defaultFile = Path.Combine(dataDir, "默认名单_1-50.txt");
            if (!File.Exists(defaultFile))
            {
                File.WriteAllLines(defaultFile, RollCallState.DefaultNames().ToArray(), Encoding.UTF8);
            }
        }

        private string GetDataDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "name");
        }

        private void RefreshListCombo()
        {
            EnsureDataFiles();
            loadingList = true;
            listCombo.Items.Clear();

            string[] files = Directory.GetFiles(GetDataDirectory(), "*.txt");
            Array.Sort(files, StringComparer.CurrentCultureIgnoreCase);
            foreach (string file in files)
            {
                listCombo.Items.Add(new ListFileItem(file));
            }

            if (listCombo.Items.Count > 0)
            {
                int defaultIndex = 0;
                for (int i = 0; i < listCombo.Items.Count; i++)
                {
                    ListFileItem item = listCombo.Items[i] as ListFileItem;
                    if (item != null && item.DisplayName.StartsWith("默认名单"))
                    {
                        defaultIndex = i;
                        break;
                    }
                }
                listCombo.SelectedIndex = defaultIndex;
            }
            loadingList = false;
            LoadSelectedList();
        }

        private void LoadSelectedList()
        {
            ListFileItem selected = listCombo.SelectedItem as ListFileItem;
            if (selected == null || !File.Exists(selected.FilePath))
            {
                return;
            }

            try
            {
                string text = File.ReadAllText(selected.FilePath, Encoding.UTF8);
                List<string> parsed = RollCallState.ParseNames(text);
                if (parsed.Count == 0)
                {
                    MessageBox.Show("这个名单文件是空的，已保留当前名单。", "名单为空",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                SetNamesBox(parsed);
                ApplyNamesFromBox(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取名单失败：\n" + ex.Message, "读取失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ImportNames()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "选择名单文件";
                dialog.Filter = "文本名单 (*.txt;*.csv)|*.txt;*.csv|所有文件 (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    string text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                    List<string> parsed = RollCallState.ParseNames(text);
                    if (parsed.Count == 0)
                    {
                        MessageBox.Show("没有读到有效名字。", "导入失败",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    SetNamesBox(parsed);
                    ApplyNamesFromBox(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败：\n" + ex.Message, "导入失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SaveNames()
        {
            List<string> parsed = RollCallState.ParseNames(namesBox.Text);
            if (parsed.Count == 0)
            {
                MessageBox.Show("请先输入至少一个名字。", "无法保存",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "保存名单";
                dialog.InitialDirectory = GetDataDirectory();
                dialog.FileName = "我的班级名单.txt";
                dialog.Filter = "文本名单 (*.txt)|*.txt";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    File.WriteAllLines(dialog.FileName, parsed.ToArray(), Encoding.UTF8);
                    state.SetNames(parsed);
                    RefreshListCombo();
                    UpdateStats();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("保存失败：\n" + ex.Message, "保存失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void SetNamesBox(IEnumerable<string> names)
        {
            loadingList = true;
            namesBox.Text = string.Join(Environment.NewLine, names.ToArray());
            loadingList = false;
            UpdateNamePreview();
        }

        private void UpdateNamePreview()
        {
            int count = RollCallState.ParseNames(namesBox.Text).Count;
            countLabel.Text = "当前编辑区：" + count + " 人";
        }

        private void ApplyNamesFromBox(bool showMessage)
        {
            List<string> parsed = RollCallState.ParseNames(namesBox.Text);
            if (parsed.Count == 0)
            {
                parsed = RollCallState.DefaultNames();
                SetNamesBox(parsed);
            }

            state.SetNames(parsed);
            UpdateStats();
            if (showMessage)
            {
                resultLabel.Text = "名单已应用";
                FitResultFont(resultLabel.Text);
            }
        }

        private void UpdateStats()
        {
            poolLabel.Text = state.WithReplacement
                ? "当前名单 " + state.Names.Count + " 人；放回模式每次都从全名单抽取"
                : "当前名单 " + state.Names.Count + " 人；未点 " + state.RemainingCount + " 人";
            UpdateNamePreview();
        }

        private void UpdateHistoryList()
        {
            historyList.BeginUpdate();
            historyList.Items.Clear();
            foreach (string item in state.History)
            {
                historyList.Items.Add(item);
            }
            historyList.EndUpdate();
        }

        private void StartRoll()
        {
            if (rolling)
            {
                StopRoll();
                return;
            }

            ApplyNamesFromBox(false);
            rolling = true;
            rollFrame = 0;
            startButton.Text = "停止点名";
            resultLabel.ForeColor = theme.Accent2;
            rollTimer.Start();
        }

        private void RollTimerTick(object sender, EventArgs e)
        {
            rollFrame++;
            ShowResultText(state.PeekRandom());
        }

        private void StopRoll()
        {
            rollTimer.Stop();
            rolling = false;
            string finalName = state.DrawOne();
            resultLabel.ForeColor = theme.Accent;
            ShowResultText(finalName);
            startButton.Text = "开始点名";
            UpdateStats();
            UpdateHistoryList();
        }

        private void ShowResultText(string text)
        {
            resultLabel.Text = text;
            FitResultFont(text);
        }

        private void FitResultFont(string text)
        {
            if (resultLabel.Width <= 20 || resultLabel.Height <= 20)
            {
                return;
            }

            Font target = resultFontLarge;
            if ((text ?? string.Empty).Length > 8)
            {
                target = resultFontMedium;
            }
            if ((text ?? string.Empty).Length > 16)
            {
                target = resultFontSmall;
            }
            resultLabel.Font = target;
        }

        private void DrawMany()
        {
            ApplyNamesFromBox(false);
            List<string> results = state.DrawMany((int)multiCount.Value);
            if (results.Count == 0)
            {
                ShowResultText("请重置名单");
            }
            else
            {
                ShowResultText(string.Join("、", results.ToArray()));
            }
            UpdateStats();
            UpdateHistoryList();
        }

        private void ShowGroups()
        {
            ApplyNamesFromBox(false);
            List<List<string>> groups = state.BuildGroups((int)groupSize.Value);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < groups.Count; i++)
            {
                builder.Append("第 ").Append(i + 1).Append(" 组：");
                builder.AppendLine(string.Join("、", groups[i].ToArray()));
            }

            MessageBox.Show(builder.ToString(), "随机分组", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DrawLuckyStar()
        {
            ApplyNamesFromBox(false);
            string selected = state.PickLuckyStar();
            ShowResultText("幸运星：" + selected);
            UpdateHistoryList();
        }

        private void StartCountdown()
        {
            if (rolling || countdownTimer.Enabled)
            {
                return;
            }

            countdownValue = 3;
            ShowResultText(countdownValue.ToString());
            countdownTimer.Start();
        }

        private void CountdownTimerTick(object sender, EventArgs e)
        {
            countdownValue--;
            if (countdownValue > 0)
            {
                ShowResultText(countdownValue.ToString());
                return;
            }

            countdownTimer.Stop();
            StartRoll();
        }

        private void OpenMiniMode()
        {
            if (rolling)
            {
                StopRoll();
            }
            ApplyNamesFromBox(false);
            MiniForm mini = new MiniForm(state, theme);
            mini.FormClosed += delegate
            {
                Show();
                WindowState = FormWindowState.Normal;
                UpdateStats();
                UpdateHistoryList();
            };
            Hide();
            mini.Show(this);
        }
    }

    internal sealed class MiniForm : Form
    {
        private readonly RollCallState state;
        private readonly Timer timer = new Timer();
        private readonly Label faceLabel = new Label();
        private readonly Label statusLabel = new Label();
        private readonly Button expandButton = new Button();
        private Font faceFontLarge;
        private Font faceFontMedium;
        private Font faceFontSmall;
        private Font miniSmallFont;
        private ThemeInfo theme;
        private bool rolling;
        private int frame;
        private Point dragStart;
        private bool dragging;
        private Point dragPressScreen;

        public MiniForm(RollCallState rollCallState, ThemeInfo currentTheme)
        {
            state = rollCallState;
            theme = currentTheme;
            Text = "班级点名器 - 缩小模式";
            Size = new Size(158, 158);
            MinimumSize = Size;
            MaximumSize = Size;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = true;
            DoubleBuffered = true;
            Font = new Font("Microsoft YaHei UI", 10F);
            faceFontLarge = new Font(Font.FontFamily, 20F, FontStyle.Bold);
            faceFontMedium = new Font(Font.FontFamily, 15F, FontStyle.Bold);
            faceFontSmall = new Font(Font.FontFamily, 11F, FontStyle.Bold);
            miniSmallFont = new Font(Font.FontFamily, 8F, FontStyle.Bold);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            faceLabel.Bounds = ClientRectangle;
            faceLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            faceLabel.Text = "开始";
            faceLabel.TextAlign = ContentAlignment.MiddleCenter;
            faceLabel.BackColor = Color.Transparent;
            faceLabel.Padding = new Padding(0, 24, 0, 0);
            faceLabel.Cursor = Cursors.Hand;
            faceLabel.MouseDown += StartDrag;
            faceLabel.MouseMove += DragWindow;
            faceLabel.MouseUp += OnMouseUp;
            Controls.Add(faceLabel);

            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Bounds = new Rectangle(0, ClientSize.Height - 26, ClientSize.Width, 26);
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            statusLabel.MouseDown += StartDrag;
            statusLabel.MouseMove += DragWindow;
            statusLabel.MouseUp += OnMouseUp;
            Controls.Add(statusLabel);
            statusLabel.BringToFront();

            expandButton.Text = "放大";
            expandButton.Size = new Size(46, 24);
            expandButton.Location = new Point(Width - expandButton.Width - 7, 7);
            expandButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            expandButton.FlatStyle = FlatStyle.Flat;
            expandButton.FlatAppearance.BorderSize = 0;
            expandButton.Cursor = Cursors.Hand;
            expandButton.Click += delegate { Close(); };
            Controls.Add(expandButton);
            expandButton.BringToFront();

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("放大", null, delegate { Close(); });
            menu.Items.Add("重置未点名单", null, delegate
            {
                state.ResetRemaining();
                SetFaceText("已重置");
                UpdateMiniStats();
            });
            menu.Items.Add("退出程序", null, delegate { Application.Exit(); });
            ContextMenuStrip = menu;
            faceLabel.ContextMenuStrip = menu;
            statusLabel.ContextMenuStrip = menu;

            timer.Interval = 40;
            timer.Tick += TimerTick;
            ApplyTheme(theme);
            Icon = SeasonIcons.Get(theme.Season);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            SeasonPainter.Paint(e.Graphics, ClientRectangle, theme, true);
            using (Pen pen = new Pen(theme.Border, 2))
            {
                Rectangle rect = new Rectangle(1, 1, Width - 3, Height - 3);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void ApplyTheme(ThemeInfo nextTheme)
        {
            theme = nextTheme;
            faceLabel.ForeColor = theme.Text;
            faceLabel.BackColor = Color.Transparent;
            statusLabel.ForeColor = theme.Muted;
            statusLabel.BackColor = theme.SoftAccent;
            statusLabel.Font = miniSmallFont;
            expandButton.BackColor = theme.SoftAccent;
            expandButton.ForeColor = theme.Text;
            expandButton.Font = miniSmallFont;
            SetFaceText("开始");
            UpdateMiniStats();
            Invalidate();
        }

        private void StartRoll()
        {
            if (rolling)
            {
                StopRoll();
                return;
            }
            rolling = true;
            frame = 0;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            frame++;
            faceLabel.ForeColor = theme.Accent2;
            SetFaceText(state.PeekRandom());
        }

        private void StopRoll()
        {
            timer.Stop();
            rolling = false;
            faceLabel.ForeColor = theme.Accent;
            SetFaceText(state.DrawOne());
            UpdateMiniStats();
        }

        private void UpdateMiniStats()
        {
            int statusHeight = state.WithReplacement ? 0 : 26;
            faceLabel.Bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height - statusHeight);
            statusLabel.Visible = !state.WithReplacement;
            statusLabel.Bounds = new Rectangle(0, ClientSize.Height - statusHeight, ClientSize.Width, statusHeight);
            statusLabel.Text = state.WithReplacement ? string.Empty : "未点 " + state.RemainingCount + "/" + state.Names.Count;
            expandButton.BringToFront();
            statusLabel.BringToFront();
            expandButton.BringToFront();
        }

        private void SetFaceText(string text)
        {
            faceLabel.Text = text;
            Font target = faceFontLarge;
            if ((text ?? string.Empty).Length > 4)
            {
                target = faceFontMedium;
            }
            if ((text ?? string.Empty).Length > 8)
            {
                target = faceFontSmall;
            }
            faceLabel.Font = target;
        }

        private void StartDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStart = e.Location;
                dragPressScreen = Cursor.Position;
                dragging = false;
            }
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point screen = Cursor.Position;
                if (Math.Abs(screen.X - dragPressScreen.X) > 3 || Math.Abs(screen.Y - dragPressScreen.Y) > 3)
                {
                    dragging = true;
                }
                Left += e.X - dragStart.X;
                Top += e.Y - dragStart.Y;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !dragging)
            {
                StartRoll();
            }
            dragging = false;
        }
    }

    internal static class SeasonIcons
    {
        private static readonly Icon[] Cache = new Icon[4];

        public static Icon Get(Season season)
        {
            int index = (int)season;
            if (Cache[index] != null)
            {
                return Cache[index];
            }
            Cache[index] = Create(season);
            return Cache[index];
        }

        private static Icon Create(Season season)
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                switch (season)
                {
                    case Season.Spring:
                        using (SolidBrush petal = new SolidBrush(Color.FromArgb(255, 176, 200)))
                        using (SolidBrush center = new SolidBrush(Color.FromArgb(226, 92, 142)))
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                double a = i * 2 * Math.PI / 5 - Math.PI / 2;
                                int px = 16 + (int)(7 * Math.Cos(a));
                                int py = 16 + (int)(7 * Math.Sin(a));
                                g.FillEllipse(petal, px - 5, py - 5, 10, 10);
                            }
                            g.FillEllipse(center, 11, 11, 10, 10);
                        }
                        break;
                    case Season.Summer:
                        using (SolidBrush sun = new SolidBrush(Color.FromArgb(255, 208, 88)))
                        using (Pen ray = new Pen(Color.FromArgb(255, 165, 0), 2))
                        {
                            g.FillEllipse(sun, 8, 8, 16, 16);
                            for (int i = 0; i < 8; i++)
                            {
                                double a = i * Math.PI / 4;
                                int x1 = 16 + (int)(10 * Math.Cos(a));
                                int y1 = 16 + (int)(10 * Math.Sin(a));
                                int x2 = 16 + (int)(14 * Math.Cos(a));
                                int y2 = 16 + (int)(14 * Math.Sin(a));
                                g.DrawLine(ray, x1, y1, x2, y2);
                            }
                        }
                        break;
                    case Season.Autumn:
                        using (SolidBrush leaf = new SolidBrush(Color.FromArgb(224, 106, 44)))
                        using (SolidBrush stem = new SolidBrush(Color.FromArgb(139, 69, 19)))
                        {
                            g.FillPolygon(leaf, new Point[] { new Point(16, 3), new Point(28, 16), new Point(16, 29), new Point(4, 16) });
                            g.FillRectangle(stem, 15, 20, 2, 8);
                        }
                        break;
                    case Season.Winter:
                        using (Pen snow = new Pen(Color.FromArgb(73, 132, 205), 2))
                        using (SolidBrush ice = new SolidBrush(Color.White))
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                double a = i * Math.PI / 3;
                                int x1 = 16 + (int)(3 * Math.Cos(a));
                                int y1 = 16 + (int)(3 * Math.Sin(a));
                                int x2 = 16 + (int)(13 * Math.Cos(a));
                                int y2 = 16 + (int)(13 * Math.Sin(a));
                                g.DrawLine(snow, x1, y1, x2, y2);
                            }
                            g.FillEllipse(ice, 13, 13, 6, 6);
                        }
                        break;
                }
            }
            IntPtr hIcon = bmp.GetHicon();
            return Icon.FromHandle(hIcon);
        }
    }

    internal sealed class CardPanel : Panel
    {
        public ThemeInfo Theme { get; set; }

        public CardPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Theme == null)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = SeasonPainter.RoundRect(rect, 8))
            using (SolidBrush fill = new SolidBrush(Theme.Card))
            using (Pen border = new Pen(Theme.Border))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }
    }

    internal sealed class ListFileItem
    {
        public readonly string FilePath;
        public readonly string DisplayName;

        public ListFileItem(string filePath)
        {
            FilePath = filePath;
            DisplayName = Path.GetFileNameWithoutExtension(filePath);
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal static class SeasonPainter
    {
        public static void Paint(Graphics graphics, Rectangle bounds, ThemeInfo theme, bool mini)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (LinearGradientBrush brush = new LinearGradientBrush(bounds, theme.Top, theme.Bottom, 90F))
            {
                graphics.FillRectangle(brush, bounds);
            }

            switch (theme.Season)
            {
                case Season.Spring:
                    PaintSpring(graphics, bounds, theme, mini);
                    break;
                case Season.Summer:
                    PaintSummer(graphics, bounds, theme, mini);
                    break;
                case Season.Autumn:
                    PaintAutumn(graphics, bounds, theme, mini);
                    break;
                case Season.Winter:
                    PaintWinter(graphics, bounds, theme, mini);
                    break;
            }
        }

        public static GraphicsPath RoundRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void PaintSpring(Graphics g, Rectangle b, ThemeInfo t, bool mini)
        {
            int count = mini ? 18 : 58;
            using (SolidBrush petal = new SolidBrush(Color.FromArgb(170, 255, 176, 205)))
            using (SolidBrush petal2 = new SolidBrush(Color.FromArgb(150, 255, 220, 235)))
            using (Pen branch = new Pen(Color.FromArgb(95, 119, 77, 88), 3))
            {
                g.DrawBezier(branch, -40, 70, b.Width / 4, 5, b.Width / 3, 80, b.Width + 45, 24);
                for (int i = 0; i < count; i++)
                {
                    int x = (i * 67 + 23) % Math.Max(1, b.Width);
                    int y = (i * 43 + 19) % Math.Max(1, b.Height);
                    Rectangle r = new Rectangle(x, y, 11 + (i % 3) * 3, 7 + (i % 2) * 2);
                    g.FillEllipse(i % 2 == 0 ? petal : petal2, r);
                }
            }
        }

        private static void PaintSummer(Graphics g, Rectangle b, ThemeInfo t, bool mini)
        {
            using (SolidBrush sun = new SolidBrush(Color.FromArgb(170, 255, 208, 88)))
            using (Pen wave = new Pen(Color.FromArgb(110, 39, 174, 201), 3))
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(125, 252, 244, 154)))
            {
                g.FillEllipse(sun, b.Width - (mini ? 46 : 120), mini ? 10 : 22, mini ? 34 : 78, mini ? 34 : 78);
                int baseY = b.Height - (mini ? 24 : 62);
                for (int row = 0; row < (mini ? 2 : 4); row++)
                {
                    GraphicsPath path = new GraphicsPath();
                    int y = baseY + row * 14;
                    path.StartFigure();
                    path.AddBezier(-20, y, b.Width / 4, y - 24, b.Width / 2, y + 24, b.Width + 20, y);
                    g.DrawPath(wave, path);
                    path.Dispose();
                }
                int count = mini ? 8 : 26;
                for (int i = 0; i < count; i++)
                {
                    int x = (i * 83 + 37) % Math.Max(1, b.Width);
                    int y = (i * 53 + 31) % Math.Max(1, Math.Max(1, b.Height - 70));
                    g.FillEllipse(glow, x, y, 5, 5);
                }
            }
        }

        private static void PaintAutumn(Graphics g, Rectangle b, ThemeInfo t, bool mini)
        {
            int count = mini ? 16 : 52;
            using (SolidBrush leaf = new SolidBrush(Color.FromArgb(160, 224, 106, 43)))
            using (SolidBrush leaf2 = new SolidBrush(Color.FromArgb(145, 143, 105, 190)))
            using (Pen wind = new Pen(Color.FromArgb(80, 143, 105, 190), 2))
            {
                for (int row = 0; row < (mini ? 2 : 4); row++)
                {
                    int y = 55 + row * 42;
                    g.DrawBezier(wind, -10, y, b.Width / 3, y - 26, b.Width * 2 / 3, y + 24, b.Width + 10, y - 12);
                }
                for (int i = 0; i < count; i++)
                {
                    int x = (i * 59 + 41) % Math.Max(1, b.Width);
                    int y = (i * 71 + 15) % Math.Max(1, b.Height);
                    Point[] points =
                    {
                        new Point(x, y - 7),
                        new Point(x + 7, y),
                        new Point(x + 2, y + 8),
                        new Point(x - 6, y + 4)
                    };
                    g.FillPolygon(i % 2 == 0 ? leaf : leaf2, points);
                }
            }
        }

        private static void PaintWinter(Graphics g, Rectangle b, ThemeInfo t, bool mini)
        {
            int count = mini ? 20 : 70;
            using (SolidBrush snow = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
            using (Pen hill = new Pen(Color.FromArgb(100, 111, 157, 208), 2))
            using (SolidBrush hillFill = new SolidBrush(Color.FromArgb(70, 218, 235, 255)))
            {
                GraphicsPath hills = new GraphicsPath();
                int y = b.Height - (mini ? 32 : 95);
                hills.StartFigure();
                hills.AddBezier(-20, y + 35, b.Width / 5, y - 20, b.Width / 2, y + 44, b.Width + 30, y);
                hills.AddLine(b.Width + 30, b.Height + 20, -20, b.Height + 20);
                hills.CloseFigure();
                g.FillPath(hillFill, hills);
                g.DrawPath(hill, hills);
                hills.Dispose();

                for (int i = 0; i < count; i++)
                {
                    int x = (i * 47 + 11) % Math.Max(1, b.Width);
                    int yy = (i * 61 + 17) % Math.Max(1, b.Height);
                    int size = 3 + (i % 3);
                    g.FillEllipse(snow, x, yy, size, size);
                }
            }
        }
    }
}
