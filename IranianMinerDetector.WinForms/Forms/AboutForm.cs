using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace IranianMinerDetector.WinForms.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            LoadAssemblyInfo();
        }

        private void InitializeComponent()
        {
            this.Text = "درباره (About)";
            this.Size = new Size(550, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            // Main Panel
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            this.Controls.Add(mainPanel);

            // Title Label
            var titleLabel = new Label
            {
                Text = "Iranian Miner Detector",
                Location = new Point(20, 20),
                Font = new Font("Tahoma", 16, FontStyle.Bold),
                AutoSize = true
            };
            mainPanel.Controls.Add(titleLabel);

            // Version Label
            var versionLabel = new Label
            {
                Name = "versionLabel",
                Location = new Point(20, 60),
                Font = new Font("Tahoma", 9),
                AutoSize = true
            };
            mainPanel.Controls.Add(versionLabel);

            // Description Label
            var descLabel = new Label
            {
                Text = "سیستم تشخیص ماینر شبکه ایرانی\n" +
                       "Iranian Network Miner Detection System\n" +
                       "نسخه فرم‌های ویندوز (Windows Forms Edition)",
                Location = new Point(20, 100),
                Font = new Font("Tahoma", 9),
                AutoSize = true
            };
            mainPanel.Controls.Add(descLabel);

            // Features GroupBox
            var featuresGroupBox = new GroupBox
            {
                Text = "ویژگی‌ها (Features)",
                Location = new Point(20, 160),
                Width = 490,
                Height = 140
            };
            mainPanel.Controls.Add(featuresGroupBox);

            var featuresLabel = new Label
            {
                Text = "• اسکن شبکه TCP با پورت های پیکربندی شده\n" +
                       "• تشخیص عملیات استخراج (Mining Detection)\n" +
                       "• پشتیبانی از موقعیت یابی جغرافیایی (Geolocation)\n" +
                       "• نقشه های تعاملی با Leaflet.js\n" +
                       "• گزارشات PDF/Excel/CSV/HTML\n" +
                       "• ذخیره سازی در پایگاه داده SQLite\n" +
                       "• پشتیبانی از ۳۱ استان ایران\n" +
                       "• انتخاب ISP ایرانی (TCI, Irancell, RighTel, ...)",
                Location = new Point(10, 20),
                Width = 470,
                Height = 110,
                Font = new Font("Tahoma", 9)
            };
            featuresGroupBox.Controls.Add(featuresLabel);

            // Copyright Label
            var copyrightLabel = new Label
            {
                Name = "copyrightLabel",
                Location = new Point(20, 310),
                Font = new Font("Tahoma", 8, FontStyle.Italic),
                AutoSize = true
            };
            mainPanel.Controls.Add(copyrightLabel);

            // OK Button
            var okButton = new Button
            {
                Text = "تایید (OK)",
                Location = new Point(430, 320),
                Width = 80,
                Height = 30,
                DialogResult = DialogResult.OK,
                BackColor = Color.Blue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            mainPanel.Controls.Add(okButton);
        }

        private void LoadAssemblyInfo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;

                var versionLabel = this.Controls.Find("versionLabel", true)[0] as Label;
                if (versionLabel != null && version != null)
                {
                    versionLabel.Text = $"نسخه (Version): {version.Major}.{version.Minor}.{version.Build} (.NET 8)";
                }

                var copyrightLabel = this.Controls.Find("copyrightLabel", true)[0] as Label;
                if (copyrightLabel != null)
                {
                    copyrightLabel.Text = copyright ?? "© 2024 Iranian Network Security";
                }
            }
            catch
            {
                // Use default values if assembly info fails to load
                var versionLabel = this.Controls.Find("versionLabel", true)[0] as Label;
                if (versionLabel != null)
                {
                    versionLabel.Text = "نسخه (Version): 1.0.0 (.NET 8)";
                }

                var copyrightLabel = this.Controls.Find("copyrightLabel", true)[0] as Label;
                if (copyrightLabel != null)
                {
                    copyrightLabel.Text = "© 2024 Iranian Network Security";
                }
            }
        }
    }
}
