#pragma warning disable CS0101
using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ContextHubDev
{
    public class FenceData
    {
        public string Name { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public List<string> Tabs { get; set; } = new List<string>();
        public int SelectedTabIndex { get; set; }
    }

    public class MainControlPanel : Form
    {
        private List<FenceForm> activeFences = new List<FenceForm>();
        private ListBox fencesListBox = new ListBox();
        private TextBox newFenceTextBox = new TextBox();
        private CheckBox startupCheckBox = new CheckBox();
        private TrackBar widthTrackBar = new TrackBar();
        private TrackBar heightTrackBar = new TrackBar();
        private Label sizeInfoLabel = new Label();
        
        private readonly string configFile = "fences.txt";
        private readonly string registryKeyName = "ContextHubDev";

        public MainControlPanel()
        {
            this.Text = "ContextHub - Панель Керування";
            this.Size = new Size(480, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            InitializeControls();
            CheckRegistryStartupStatus();

            this.Load += (s, e) => { LoadConfiguration(); };
        }

        private void InitializeControls()
        {
            Label titleLabel = new Label() { Text = "Створення нової плитки:", Location = new Point(20, 15), Size = new Size(200, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            newFenceTextBox = new TextBox() { Location = new Point(20, 40), Size = new Size(240, 23) };
            
            Button addFenceBtn = new Button() { Text = "Додати", Location = new Point(270, 38), Size = new Size(170, 26) };
            addFenceBtn.Click += AddNewFenceClick;

            Label listLabel = new Label() { Text = "Список активних контейнерів:", Location = new Point(20, 80), Size = new Size(200, 20) };
            fencesListBox = new ListBox() { Location = new Point(20, 105), Size = new Size(420, 95), Font = new Font("Segoe UI", 9) };
            fencesListBox.SelectedIndexChanged += SelectedFenceChanged;

            Label widthLabel = new Label() { Text = "Ширина:", Location = new Point(20, 215), Size = new Size(60, 20) };
            widthTrackBar = new TrackBar() { Location = new Point(80, 210), Size = new Size(360, 45), Minimum = 150, Maximum = 800, Value = 310 };
            
            Label heightLabel = new Label() { Text = "Висота:", Location = new Point(20, 260), Size = new Size(60, 20) };
            heightTrackBar = new TrackBar() { Location = new Point(80, 255), Size = new Size(360, 45), Minimum = 100, Maximum = 800, Value = 420 };

            widthTrackBar.Scroll += SizeSlidersScroll;
            heightTrackBar.Scroll += SizeSlidersScroll;

            sizeInfoLabel = new Label() { Text = "Розмір плитки: 310 x 420 px", Location = new Point(20, 310), Size = new Size(300, 20) };

            startupCheckBox = new CheckBox() { Text = "Запускати автоматично при старті Windows", Location = new Point(20, 350), Size = new Size(420, 25) };
            startupCheckBox.CheckedChanged += StartupCheckBoxChanged;

            Button clearAllBtn = new Button() { Text = "❌ СХОВАТИ ТА ВИДАЛИТИ ВСІ ПЛИТКИ", Location = new Point(20, 400), Size = new Size(420, 38), BackColor = Color.Brown, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            clearAllBtn.FlatAppearance.BorderSize = 0;
            clearAllBtn.Click += ResetConfigurationClick;

            this.Controls.AddRange(new Control[] { titleLabel, newFenceTextBox, addFenceBtn, listLabel, fencesListBox, widthTrackBar, heightTrackBar, sizeInfoLabel, startupCheckBox, clearAllBtn });

            this.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
            };
        }

        private void LoadConfiguration()
        {
            fencesListBox.Items.Clear();
            activeFences.Clear();

            if (File.Exists(configFile) && new FileInfo(configFile).Length > 0)
            {
                try
                {
                    string[] lines = File.ReadAllLines(configFile);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string[] parts = line.Split('|');
                        if (parts.Length >= 6)
                        {
                            FenceData data = new FenceData()
                            {
                                Name = parts[0], X = int.Parse(parts[1]), Y = int.Parse(parts[2]),
                                W = int.Parse(parts[3]), H = int.Parse(parts[4]),
                                Tabs = new List<string>(parts[5].Split(',')), SelectedTabIndex = 0
                            };
                            BuildFenceWindow(data);
                        }
                    }
                }
                catch { InitDefaultLayout(); }
            }
            else { InitDefaultLayout(); }

            ScanAndDistributeDesktopFiles();
        }

        private void InitDefaultLayout()
        {
            BuildFenceWindow(new FenceData() { Name = "University Work", X = 50, Y = 120, W = 310, H = 420, Tabs = new List<string> { "General", "Labs", "Dev" } });
            BuildFenceWindow(new FenceData() { Name = "Entertainment", X = 390, Y = 120, W = 310, H = 420, Tabs = new List<string> { "Games", "Media" } });
            BuildFenceWindow(new FenceData() { Name = "General Desktop", X = 730, Y = 120, W = 310, H = 420, Tabs = new List<string> { "Files", "Shortcuts" } });
            SaveConfiguration();
        }

        private void BuildFenceWindow(FenceData data)
        {
            FenceForm form = new FenceForm(data, new Action(SaveConfiguration), this);
            form.Show();
            activeFences.Add(form);
            fencesListBox.Items.Add(data.Name);
        }

        public void ScanAndDistributeDesktopFiles()
        {
            foreach (var f in activeFences) f.ClearAllTabsData();

            List<string> desktopFolders = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")
            };

            HashSet<string> processedFiles = new HashSet<string>();

            foreach (string folder in desktopFolders)
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;
                try
                {
                    DirectoryInfo di = new DirectoryInfo(folder);
                    foreach (var item in di.GetFileSystemInfos())
                    {
                        string nameLower = item.Name.ToLower();
                        if (nameLower == "desktop.ini" || processedFiles.Contains(item.FullName.ToLower())) continue;
                        processedFiles.Add(item.FullName.ToLower());

                        FenceForm targetFence = activeFences.Find(f => f.Data.Name == "General Desktop");
                        string ext = item.Extension.ToLower();
                        string targetTab = (ext == ".lnk" || ext == ".url") ? "Shortcuts" : "Files";

                        if (nameLower.Contains("lab") || nameLower.Contains("uni") || nameLower.Contains("intern") || nameLower.Contains("code") || nameLower.Contains("курсов") || ext == ".cs")
                        {
                            FenceForm uniFence = activeFences.Find(f => f.Data.Name == "University Work");
                            if (uniFence != null)
                            {
                                targetFence = uniFence;
                                if (nameLower.Contains("lab")) targetTab = "Labs";
                                else if (nameLower.Contains("code") || ext == ".cs") targetTab = "Dev";
                                else targetTab = "General";
                            }
                        }
                        else if (nameLower.Contains("game") || nameLower.Contains("steam") || nameLower.Contains("play") || nameLower.Contains("brawl") || nameLower.Contains("cs") || nameLower.Contains("gta"))
                        {
                            FenceForm entFence = activeFences.Find(f => f.Data.Name == "Entertainment");
                            if (entFence != null)
                            {
                                targetFence = entFence;
                                targetTab = "Games";
                            }
                        }

                        if (targetFence != null)
                        {
                            if (!targetFence.Data.Tabs.Contains(targetTab)) targetTab = targetFence.Data.Tabs[0];
                            targetFence.AddFileToTab(targetTab, item.FullName);
                        }
                    }
                }
                catch { }
            }

            foreach (var f in activeFences) f.UpdateItemsDisplay();
        }

        private void SaveConfiguration()
        {
            try
            {
                List<string> rows = new List<string>();
                foreach (var f in activeFences)
                {
                    string tabsJoined = string.Join(",", f.Data.Tabs);
                    rows.Add($"{f.Data.Name}|{f.Data.X}|{f.Data.Y}|{f.Data.W}|{f.Data.H}|{tabsJoined}");
                }
                File.WriteAllLines(configFile, rows);
            }
            catch { }
        }

        private int ClampValue(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        private void SelectedFenceChanged(object sender, EventArgs e)
        {
            if (fencesListBox.SelectedItem == null) return;
            string selectedName = fencesListBox.SelectedItem.ToString();
            FenceForm targetForm = activeFences.Find(f => f.Data.Name == selectedName);

            if (targetForm != null)
            {
                widthTrackBar.Scroll -= SizeSlidersScroll;
                heightTrackBar.Scroll -= SizeSlidersScroll;

                int formWidth = targetForm.Width;
                int formHeight = targetForm.IsCollapsed ? targetForm.OriginalHeight : targetForm.Height;

                widthTrackBar.Value = ClampValue(formWidth, widthTrackBar.Minimum, widthTrackBar.Maximum);
                heightTrackBar.Value = ClampValue(formHeight, heightTrackBar.Minimum, heightTrackBar.Maximum);
                
                sizeInfoLabel.Text = $"Розмір плитки: {widthTrackBar.Value} x {heightTrackBar.Value} px";

                widthTrackBar.Scroll += SizeSlidersScroll;
                heightTrackBar.Scroll += SizeSlidersScroll;
            }
        }

        private void SizeSlidersScroll(object sender, EventArgs e)
        {
            if (fencesListBox.SelectedItem == null) return;
            string selectedName = fencesListBox.SelectedItem.ToString();
            FenceForm targetForm = activeFences.Find(f => f.Data.Name == selectedName);

            if (targetForm != null)
            {
                targetForm.UpdateSize(widthTrackBar.Value, heightTrackBar.Value);
                sizeInfoLabel.Text = $"Розмір плитки: {widthTrackBar.Value} x {heightTrackBar.Value} px";
                SaveConfiguration();
            }
        }

        private void AddNewFenceClick(object sender, EventArgs e)
        {
            string name = newFenceTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            if (activeFences.Exists(f => f.Data.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;

            FenceData data = new FenceData() { Name = name, X = 200, Y = 200, W = 310, H = 420, Tabs = new List<string> { "General" } };
            BuildFenceWindow(data);
            newFenceTextBox.Clear();
            SaveConfiguration();
            ScanAndDistributeDesktopFiles();
            fencesListBox.SelectedItem = name;
        }

        private void StartupCheckBoxChanged(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (startupCheckBox.Checked) rk.SetValue(registryKeyName, Application.ExecutablePath);
                    else rk.DeleteValue(registryKeyName, false);
                }
            }
            catch { }
        }

        private void CheckRegistryStartupStatus()
        {
            try
            {
                using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    startupCheckBox.Checked = (rk.GetValue(registryKeyName) != null);
                }
            }
            catch { startupCheckBox.Checked = false; }
        }

        private void ResetConfigurationClick(object sender, EventArgs e)
        {
            for (int i = activeFences.Count - 1; i >= 0; i--)
            {
                activeFences[i].Hide(); activeFences[i].Dispose();
            }
            activeFences.Clear(); fencesListBox.Items.Clear();
            if (File.Exists(configFile)) try { File.Delete(configFile); } catch {}
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainControlPanel());
        }
    }
}