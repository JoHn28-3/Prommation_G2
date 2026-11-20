#nullable enable

using System;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Pages
{
    public sealed class WelcomePage : AppPage
    {
        private readonly TextBox _name;
        private readonly TextBox _username;
        private readonly TextBox _ip;
        private readonly ComboBox _theme;
        private readonly Button _start;
        private readonly PictureBox _photo;
        private string? _photoPath;

        public event Action<UserProfile, string?>? RegistrationCompleted;

        public WelcomePage(UserProfile profile)
            : base("Bienvenue")
        {
            _name = new TextBox { Text = profile.DisplayName, Dock = DockStyle.Fill };
            _username = new TextBox { Text = profile.Username, Dock = DockStyle.Fill };
            _ip = new TextBox { Text = profile.IpAddress, Dock = DockStyle.Fill };
            _theme = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _theme.Items.AddRange(new object[] { "Clair", "Sombre", "Système" });
            _theme.SelectedIndex = profile.ThemeMode switch
            {
                "light" => 0,
                "system" => 2,
                _ => 1
            };
            _theme.SelectedIndexChanged += (s, e) =>
            {
                Theme.SetThemeMode(_theme.SelectedIndex switch
                {
                    0 => ThemeMode.Light,
                    2 => ThemeMode.System,
                    _ => ThemeMode.Dark
                });
            };

            _photo = new PictureBox
            {
                Width = LogicalToDeviceUnits(160),
                Height = LogicalToDeviceUnits(160),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.Panel,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _photo.Click += ChoosePhoto;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(LogicalToDeviceUnits(40)),
                BackColor = Theme.AppBackground
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(220)));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(_photo, 0, 0);
            layout.Controls.Add(CreateField("Nom affiché", _name), 1, 0);
            layout.Controls.Add(CreateField("Pseudo (@pseudo)", _username), 1, 1);
            layout.Controls.Add(CreateField("Adresse détectée / saisie", _ip), 1, 2);
            layout.Controls.Add(CreateField("Thème", _theme), 1, 3);

            _start = new Button
            {
                Text = "Commencer",
                Dock = DockStyle.Bottom,
                Height = LogicalToDeviceUnits(48),
                Enabled = false,
                BackColor = Theme.Accent,
                ForeColor = Theme.AccentText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _start.FlatAppearance.BorderSize = 0;
            _start.Click += CompleteRegistration;

            _name.TextChanged += Validate;
            _username.TextChanged += Validate;
            _ip.TextChanged += Validate;
            Controls.Add(layout);
            Controls.Add(_start);
            Validate(this, EventArgs.Empty);
        }

        private static Panel CreateField(string labelText, Control field)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 80,
                BackColor = Theme.AppBackground,
                Padding = new Padding(8)
            };
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Theme.TextSecondary
            };
            field.BackColor = Theme.SearchBackground;
            field.ForeColor = Theme.TextPrimary;
            field.Height = 32;
            panel.Controls.Add(field);
            panel.Controls.Add(label);
            return panel;
        }

        private void ChoosePhoto(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choisir une photo de profil",
                Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            try
            {
                _photo.Image?.Dispose();
                _photo.Image = Image.FromFile(dialog.FileName);
                _photoPath = dialog.FileName;
            }
            catch
            {
                _photo.Image = null;
            }
        }

        private void Validate(object? sender, EventArgs e)
        {
            bool nameValid = _name.Text.Trim().Length >= 1;
            bool usernameValid = Regex.IsMatch(_username.Text.Trim(),
                @"^[a-z0-9_.]{3,24}$", RegexOptions.IgnoreCase);
            bool ipValid = IPAddress.TryParse(_ip.Text.Trim(), out IPAddress? ip)
                && ip != null
                && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                && !IPAddress.IsLoopback(ip);
            _start.Enabled = nameValid && usernameValid && ipValid;
        }

        private void CompleteRegistration(object? sender, EventArgs e)
        {
            var profile = new UserProfile
            {
                DisplayName = _name.Text.Trim(),
                Username = _username.Text.Trim(),
                IpAddress = _ip.Text.Trim(),
                ThemeMode = _theme.SelectedIndex switch
                {
                    0 => "light",
                    2 => "system",
                    _ => "dark"
                }
            };
            RegistrationCompleted?.Invoke(profile, _photoPath);
        }
    }
}
