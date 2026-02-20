using System;
using System.Windows.Forms;
using System.IO;
using IranianMinerDetector.WinForms.Data;

namespace IranianMinerDetector.WinForms.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;
        private string? _apiKey;
        private string? _apiProvider;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "تنظیمات (Settings)";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.MinimumSize = new Size(600, 500);

            // Main Layout
            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            this.Controls.Add(mainPanel);

            // API Settings GroupBox
            var apiGroupBox = new GroupBox
            {
                Text = "تنظیمات API جغرافیایی (Geolocation API)",
                Location = new Point(10, 10),
                Width = 560,
                Height = 150
            };
            mainPanel.Controls.Add(apiGroupBox);

            // API Provider Label
            var providerLabel = new Label
            {
                Text = "سرویس دهنده (Provider):",
                Location = new Point(10, 30),
                AutoSize = true
            };
            apiGroupBox.Controls.Add(providerLabel);

            // API Provider ComboBox
            var providerComboBox = new ComboBox
            {
                Location = new Point(10, 55),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            providerComboBox.Items.AddRange(new[] { "ip-api.com", "ipinfo.io", "ipgeolocation.io" });
            providerComboBox.SelectedIndex = 0;
            providerComboBox.Name = "providerComboBox";
            apiGroupBox.Controls.Add(providerComboBox);

            // API Key Label
            var keyLabel = new Label
            {
                Text = "کلید API (API Key) - اختیاری:",
                Location = new Point(10, 85),
                AutoSize = true
            };
            apiGroupBox.Controls.Add(keyLabel);

            // API Key TextBox
            var keyTextBox = new TextBox
            {
                Location = new Point(10, 110),
                Width = 540,
                PlaceholderText = "Enter API key if required by the service"
            };
            keyTextBox.Name = "keyTextBox";
            apiGroupBox.Controls.Add(keyTextBox);

            // Scan Settings GroupBox
            var scanGroupBox = new GroupBox
            {
                Text = "تنظیمات پیش فرض اسکن (Default Scan Settings)",
                Location = new Point(10, 170),
                Width = 560,
                Height = 200
            };
            mainPanel.Controls.Add(scanGroupBox);

            // Default Timeout Label
            var timeoutLabel = new Label
            {
                Text = "زمان انتظار پیش فرض (ms):",
                Location = new Point(10, 30),
                AutoSize = true
            };
            scanGroupBox.Controls.Add(timeoutLabel);

            // Default Timeout NumericUpDown
            var timeoutNumeric = new NumericUpDown
            {
                Location = new Point(10, 55),
                Width = 100,
                Minimum = 500,
                Maximum = 10000,
                Value = 3000
            };
            timeoutNumeric.Name = "timeoutNumeric";
            scanGroupBox.Controls.Add(timeoutNumeric);

            // Default Concurrency Label
            var concurrencyLabel = new Label
            {
                Text = "تعداد همزمان پیش فرض:",
                Location = new Point(120, 30),
                AutoSize = true
            };
            scanGroupBox.Controls.Add(concurrencyLabel);

            // Default Concurrency NumericUpDown
            var concurrencyNumeric = new NumericUpDown
            {
                Location = new Point(120, 55),
                Width = 100,
                Minimum = 10,
                Maximum = 500,
                Value = 100
            };
            concurrencyNumeric.Name = "concurrencyNumeric";
            scanGroupBox.Controls.Add(concurrencyNumeric);

            // Default Ports Label
            var portsLabel = new Label
            {
                Text = "پورت های پیش فرض:",
                Location = new Point(10, 90),
                AutoSize = true
            };
            scanGroupBox.Controls.Add(portsLabel);

            // Default Ports TextBox
            var portsTextBox = new TextBox
            {
                Location = new Point(10, 115),
                Width = 540,
                Text = "8332,8333,30303,3333,4028,4444,18081"
            };
            portsTextBox.Name = "portsTextBox";
            scanGroupBox.Controls.Add(portsTextBox);

            // Perform Banner Grab CheckBox
            var bannerCheckBox = new CheckBox
            {
                Text = "دریافت اطلاعات بنر (Banner Grab)",
                Location = new Point(10, 150),
                Width = 250,
                Checked = true
            };
            bannerCheckBox.Name = "bannerCheckBox";
            scanGroupBox.Controls.Add(bannerCheckBox);

            // Use Geolocation CheckBox
            var geoCheckBox = new CheckBox
            {
                Text = "استفاده از موقعیت یابی جغرافیایی",
                Location = new Point(270, 150),
                Width = 250,
                Checked = true
            };
            geoCheckBox.Name = "geoCheckBox";
            scanGroupBox.Controls.Add(geoCheckBox);

            // Database Info GroupBox
            var dbGroupBox = new GroupBox
            {
                Text = "اطلاعات پایگاه داده (Database Information)",
                Location = new Point(10, 380),
                Width = 560,
                Height = 60
            };
            mainPanel.Controls.Add(dbGroupBox);

            var dbPathLabel = new Label
            {
                Location = new Point(10, 25),
                Width = 540,
                AutoSize = true
            };
            dbPathLabel.Name = "dbPathLabel";
            dbGroupBox.Controls.Add(dbPathLabel);

            // Buttons Panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60
            };
            mainPanel.Controls.Add(buttonPanel);

            var saveButton = new Button
            {
                Text = "ذخیره (Save)",
                Location = new Point(380, 15),
                Width = 80,
                Height = 30,
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += (s, e) => SaveSettings();
            buttonPanel.Controls.Add(saveButton);

            var cancelButton = new Button
            {
                Text = "لغو (Cancel)",
                Location = new Point(470, 15),
                Width = 80,
                Height = 30,
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(cancelButton);

            var clearCacheButton = new Button
            {
                Text = "پاک کردن کش (Clear Cache)",
                Location = new Point(10, 15),
                Width = 120,
                Height = 30
            };
            clearCacheButton.Click += (s, e) => ClearCache();
            buttonPanel.Controls.Add(clearCacheButton);

            // Initialize values
            Load += (s, e) =>
            {
                var providerCb = this.Controls.Find("providerComboBox", true)[0] as ComboBox;
                var keyTb = this.Controls.Find("keyTextBox", true)[0] as TextBox;
                var timeoutNum = this.Controls.Find("timeoutNumeric", true)[0] as NumericUpDown;
                var concurrencyNum = this.Controls.Find("concurrencyNumeric", true)[0] as NumericUpDown;
                var portsTb = this.Controls.Find("portsTextBox", true)[0] as TextBox;
                var bannerCb = this.Controls.Find("bannerCheckBox", true)[0] as CheckBox;
                var geoCb = this.Controls.Find("geoCheckBox", true)[0] as CheckBox;
                var dbPathLb = this.Controls.Find("dbPathLabel", true)[0] as Label;

                if (providerCb != null && _apiProvider != null)
                    providerCb.SelectedItem = _apiProvider;

                if (keyTb != null && _apiKey != null)
                    keyTb.Text = _apiKey;

                if (dbPathLb != null)
                    dbPathLb.Text = $"مسیر دیتابیس: {_db.GetDatabasePath()}";
            };
        }

        private void LoadSettings()
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IranianMinerDetector");
            var settingsFile = Path.Combine(appData, "settings.json");

            if (File.Exists(settingsFile))
            {
                try
                {
                    var json = File.ReadAllText(settingsFile);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);
                    if (settings != null)
                    {
                        _apiKey = settings.ApiKey;
                        _apiProvider = settings.ApiProvider;
                    }
                }
                catch
                {
                    // Use defaults
                }
            }
        }

        private void SaveSettings()
        {
            var providerComboBox = this.Controls.Find("providerComboBox", true)[0] as ComboBox;
            var keyTextBox = this.Controls.Find("keyTextBox", true)[0] as TextBox;
            var timeoutNumeric = this.Controls.Find("timeoutNumeric", true)[0] as NumericUpDown;
            var concurrencyNumeric = this.Controls.Find("concurrencyNumeric", true)[0] as NumericUpDown;
            var portsTextBox = this.Controls.Find("portsTextBox", true)[0] as TextBox;
            var bannerCheckBox = this.Controls.Find("bannerCheckBox", true)[0] as CheckBox;
            var geoCheckBox = this.Controls.Find("geoCheckBox", true)[0] as CheckBox;

            var settings = new SettingsData
            {
                ApiKey = keyTextBox?.Text,
                ApiProvider = providerComboBox?.SelectedItem?.ToString(),
                DefaultTimeout = (int)(timeoutNumeric?.Value ?? 3000),
                DefaultConcurrency = (int)(concurrencyNumeric?.Value ?? 100),
                DefaultPorts = portsTextBox?.Text ?? "8332,8333,30303,3333,4028,4444,18081",
                PerformBannerGrab = bannerCheckBox?.Checked ?? true,
                UseGeolocation = geoCheckBox?.Checked ?? true
            };

            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IranianMinerDetector");

            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);

            var settingsFile = Path.Combine(appData, "settings.json");
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(settingsFile, json);

            MessageBox.Show(
                "تنظیمات ذخیره شد (Settings saved)",
                "موفقیت (Success)",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
        }

        private void ClearCache()
        {
            var result = MessageBox.Show(
                "آیا مطمئن هستید که می خواهید کش را پاک کنید؟\n" +
                "Are you sure you want to clear the cache?",
                "تایید پاک کردن (Confirm Clear)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _db.ClearGeolocationCache();
                    MessageBox.Show(
                        "کش با موفقیت پاک شد (Cache cleared successfully)",
                        "موفقیت (Success)",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"خطا در پاک کردن کش: {ex.Message}\nError clearing cache: {ex.Message}",
                        "خطا (Error)",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private class SettingsData
        {
            public string? ApiKey { get; set; }
            public string? ApiProvider { get; set; }
            public int DefaultTimeout { get; set; } = 3000;
            public int DefaultConcurrency { get; set; } = 100;
            public string? DefaultPorts { get; set; } = "8332,8333,30303,3333,4028,4444,18081";
            public bool PerformBannerGrab { get; set; } = true;
            public bool UseGeolocation { get; set; } = true;
        }
    }
}
