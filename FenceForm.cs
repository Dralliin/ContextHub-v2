#pragma warning disable CS0101
using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ContextHubDev
{
    public class FenceForm : Form
    {
        public FenceData Data { get; private set; }
        private ListView iconsListView = new ListView();
        private ImageList largeImageList = new ImageList();
        private Dictionary<string, List<string>> tabFilePaths;
        private Action onMoveOrResizeCallback;
        private Form mainControlPanelReference;

        private Panel headerPanel = new Panel();
        private FlowLayoutPanel tabsPanel = new FlowLayoutPanel();
        
        private bool isDragging = false;
        private Point dragCursorPosition;
        private Point dragFormPosition;

        public bool IsCollapsed { get; private set; } = false;
        public int OriginalHeight { get; private set; }

        protected override bool ShowWithoutActivation => true;

        public FenceForm(FenceData data, Action onMoveOrResize, Form mainPanel)
        {
            this.Data = data;
            this.onMoveOrResizeCallback = onMoveOrResize;
            this.mainControlPanelReference = mainPanel;
            this.tabFilePaths = new Dictionary<string, List<string>>();
            this.OriginalHeight = data.H;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(data.X, data.Y);
            this.Size = new Size(data.W, data.H);
            this.BackColor = Color.FromArgb(20, 20, 25); 
            this.Opacity = 0.85; 
            this.ShowInTaskbar = false;

            UpdateRounding();

            foreach (var tab in data.Tabs) tabFilePaths[tab] = new List<string>();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            headerPanel = new Panel() { Height = 35, Dock = DockStyle.Top, BackColor = Color.Transparent };

            Label titleLabel = new Label() 
            { 
                Text = Data.Name, 
                ForeColor = Color.White, 
                Font = new Font("Segoe UI", 11, FontStyle.Bold), 
                TextAlign = ContentAlignment.MiddleCenter, 
                Dock = DockStyle.Fill 
            };
            headerPanel.Controls.Add(titleLabel);

            titleLabel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isDragging = true; dragCursorPosition = Cursor.Position; dragFormPosition = this.Location; } };
            titleLabel.MouseMove += (s, e) => { if (isDragging) { Point cur = Cursor.Position; this.Location = new Point(dragFormPosition.X + (cur.X - dragCursorPosition.X), dragFormPosition.Y + (cur.Y - dragCursorPosition.Y)); Data.X = this.Location.X; Data.Y = this.Location.Y; } };
            titleLabel.MouseUp += (s, e) => { if (isDragging) { isDragging = false; if (this.Top <= 40 && this.Top >= -40) { this.Top = 0; Data.Y = 0; } onMoveOrResizeCallback?.Invoke(); } };

            EventHandler toggleCollapseHandler = (s, e) => {
                if (!IsCollapsed) { OriginalHeight = this.Height; tabsPanel.Visible = false; iconsListView.Visible = false; this.Height = headerPanel.Height; IsCollapsed = true; }
                else { this.Height = OriginalHeight; tabsPanel.Visible = true; iconsListView.Visible = true; IsCollapsed = false; }
                UpdateRounding();
            };
            titleLabel.DoubleClick += toggleCollapseHandler;

            this.Controls.Add(headerPanel);

            tabsPanel = new FlowLayoutPanel() { Height = 25, Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
            for (int i = 0; i < Data.Tabs.Count; i++)
            {
                int idx = i;
                Button btn = new Button() { Text = Data.Tabs[i].ToUpper(), Height = 20, AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = (idx == Data.SelectedTabIndex) ? Color.FromArgb(0, 255, 204) : Color.DarkGray, Font = new Font("Segoe UI Semibold", 7.5f) };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => { Data.SelectedTabIndex = idx; UpdateTabsHighlight(); UpdateItemsDisplay(); };
                tabsPanel.Controls.Add(btn);
            }
            if (Data.Tabs.Count <= 1) tabsPanel.Height = 0;
            this.Controls.Add(tabsPanel);

            largeImageList = new ImageList() { ImageSize = new Size(32, 32), ColorDepth = ColorDepth.Depth32Bit };
            iconsListView = new ListView() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 25), BorderStyle = BorderStyle.None, View = View.LargeIcon, LargeImageList = largeImageList, MultiSelect = false };
            iconsListView.DoubleClick += (s, e) => {
                if (iconsListView.SelectedIndices.Count > 0) {
                    string path = tabFilePaths[Data.Tabs[Data.SelectedTabIndex]][iconsListView.SelectedIndices[0]];
                    try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
                }
            };
            this.Controls.Add(iconsListView);

            Button settingsBtn = new Button() 
            { 
                Text = "⚙️", 
                Size = new Size(25, 25), 
                Location = new Point(Data.W - 35, 5),
                FlatStyle = FlatStyle.Flat, 
                ForeColor = Color.White, 
                BackColor = Color.Transparent, 
                Font = new Font("Segoe UI", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            settingsBtn.FlatAppearance.BorderSize = 0;
            settingsBtn.Click += (s, e) => { 
                if (mainControlPanelReference != null) { 
                    mainControlPanelReference.Show(); 
                    mainControlPanelReference.BringToFront(); 
                } 
            };
            
            this.Controls.Add(settingsBtn);

            iconsListView.SendToBack();
            tabsPanel.BringToFront();
            headerPanel.BringToFront();
            settingsBtn.BringToFront();
        }

        private void UpdateTabsHighlight()
        {
            for (int i = 0; i < tabsPanel.Controls.Count; i++)
                if (tabsPanel.Controls[i] is Button b) b.ForeColor = (i == Data.SelectedTabIndex) ? Color.FromArgb(0, 255, 204) : Color.DarkGray;
        }

        public void AddFileToTab(string tab, string fullPath) { if (tabFilePaths.ContainsKey(tab) && !tabFilePaths[tab].Contains(fullPath)) tabFilePaths[tab].Add(fullPath); }
        public void ClearAllTabsData() { foreach (var k in tabFilePaths.Keys) tabFilePaths[k].Clear(); }

        public void UpdateItemsDisplay()
        {
            iconsListView.Items.Clear(); largeImageList.Images.Clear();
            string currentTab = Data.Tabs[Data.SelectedTabIndex];
            if (!tabFilePaths.ContainsKey(currentTab)) return;

            int idx = 0;
            foreach (string path in tabFilePaths[currentTab])
            {
                string name = Path.GetFileNameWithoutExtension(path);
                Icon icon = SystemStuff.GetSystemIcon(path);
                if (icon != null) largeImageList.Images.Add(icon);
                else largeImageList.Images.Add(SystemIcons.Application);

                iconsListView.Items.Add(new ListViewItem(name, idx) { ForeColor = Color.White });
                idx++;
            }
        }

        private void UpdateRounding()
        {
            IntPtr pRegion = SystemStuff.CreateRoundRectRgn(0, 0, Width, Height, 12, 12);
            this.Region = Region.FromHrgn(pRegion);
        }

        public void UpdateSize(int w, int h)
        {
            this.Size = new Size(w, h);
            if (!IsCollapsed) OriginalHeight = h;
            UpdateRounding();
            Data.W = w; 
            Data.H = h;
        }
    }
}