#nullable enable

using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Forms
{
    public sealed class RegistrationForm : Form
    {
        private readonly TextBox _name;
        private readonly TextBox _ip;
        private readonly ComboBox _theme;
        private readonly PictureBox _photo;
        private string? _photoPath;

        public UserProfile Profile { get; private set; }

        public RegistrationForm(UserProfile profile)
        {
            Profile = profile;
            Text = "Créer mon profil";
            Size = new Size(460, 430);
            MinimumSize = Size;
            MaximumSize = Size;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.AppBackground;
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var title = new Label
            {
                Text = "Bienvenue dans WHATSAPP",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 55
            };

            var center = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(24),
                RowCount = 6
            };
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _photo = new PictureBox
            {
                Width = 140,
                Height = 140,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Theme.Panel,
                Cursor = Cursors.Hand
            };
            _photo.Click += ChoosePhoto;

            var photoHint = new Label
            {
                Text = "Cliquer pour choisir\nune photo",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill
            };

            _name = CreateText(profile.DisplayName);
            _ip = CreateText(profile.IpAddress);
            _theme = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary
            };
            _theme.Items.AddRange(new object[] { "Sombre", "Clair" });
            _theme.SelectedIndex = profile.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            center.Controls.Add(_photo, 0, 0);
            center.Controls.Add(photoHint, 1, 0);
            center.Controls.Add(new Label { Text = "Nom d'utilisateur", ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill }, 0, 1);
            center.Controls.Add(_name, 1, 1);
            center.Controls.Add(new Label { Text = "Adresse IP locale", ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill }, 0, 2);
            center.Controls.Add(_ip, 1, 2);
            center.Controls.Add(new Label { Text = "Interface", ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill }, 0, 3);
            center.Controls.Add(_theme, 1, 3);

            var create = new Button
            {
                Text = "Créer mon profil",
                Dock = DockStyle.Bottom,
                Height = 42,
                BackColor = Theme.Accent,
                ForeColor = Theme.AccentText,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            create.FlatAppearance.BorderSize = 0;
            create.Click += CreateProfile;

            Controls.Add(center);
            Controls.Add(create);
            Controls.Add(title);
            AcceptButton = create;
        }

        private TextBox CreateText(string text)
        {
            return new TextBox
            {
                Text = text,
                Dock = DockStyle.Fill,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void ChoosePhoto(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choisir ma photo de profil",
                Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _photo.Image?.Dispose();
                _photo.Image = Image.FromFile(dialog.FileName);
                _photoPath = dialog.FileName;
            }
            catch
            {
                MessageBox.Show(this, "Cette image ne peut pas être utilisée.", "Photo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CreateProfile(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show(this, "Entrez votre nom d'utilisateur.", "Inscription", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _name.Focus();
                return;
            }

            if (!IPAddress.TryParse(_ip.Text.Trim(), out _))
            {
                MessageBox.Show(this, "Entrez une adresse IPv4 valide.", "Inscription", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _ip.Focus();
                return;
            }

            Profile = new UserProfile
            {
                DisplayName = _name.Text.Trim(),
                IpAddress = _ip.Text.Trim(),
                ThemeMode = _theme.SelectedIndex == 1 ? "light" : "dark"
            };
            Tag = _photoPath;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
