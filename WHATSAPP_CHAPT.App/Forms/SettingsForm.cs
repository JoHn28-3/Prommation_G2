#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Interface de configuration (Phase G) permettant de paramétrer :
    /// Couleur, Nom, IP locale et Identifiant.
    /// </summary>
    public sealed class SettingsForm : Form
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtIp;
        private readonly TextBox _txtGuid;
        private readonly Button _btnColor;
        private readonly IAvatarProvider? _avatars;
        private readonly Guid _ownerId;
        private readonly ComboBox _themeMode;
        private Color _selectedColor;

        public string DisplayName => _txtName.Text.Trim();
        public string LocalIp => _txtIp.Text.Trim();
        public string Identifier => _txtGuid.Text.Trim();
        public Color ThemeColor => _selectedColor;
        public string ThemeMode => _themeMode.SelectedIndex == 1 ? "light" : "dark";

        public SettingsForm(string currentName, string currentIp, Guid currentId, Color currentColor, IAvatarProvider? avatars = null)
        {
            _selectedColor = currentColor;
            _avatars = avatars;
            _ownerId = currentId;

            Text = "Paramètres";
            Size = new Size(420, 440);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.AppBackground;
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int left = 20;
            int width = 360;

            // Nom
            var lblName = new Label
            {
                Text = "Nom d'affichage :",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(left, 15),
                AutoSize = true
            };
            _txtName = new TextBox
            {
                Location = new Point(left, 38),
                Width = width,
                Text = currentName,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            // IP
            var lblIp = new Label
            {
                Text = "Adresse IP locale :",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(left, 75),
                AutoSize = true
            };
            _txtIp = new TextBox
            {
                Location = new Point(left, 98),
                Width = width,
                Text = currentIp,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Identifiant GUID
            var lblGuid = new Label
            {
                Text = "Identifiant réseau unique (GUID) :",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(left, 135),
                AutoSize = true
            };
            _txtGuid = new TextBox
            {
                Location = new Point(left, 158),
                Width = width,
                Text = currentId.ToString("D"),
                ReadOnly = true,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextSecondary,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnPhoto = new Button
            {
                Text = "Modifier la photo de profil",
                Location = new Point(left, 195),
                Width = 360,
                Height = 32,
                BackColor = Theme.Panel,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPhoto.FlatAppearance.BorderSize = 0;
            btnPhoto.Click += (s, e) =>
            {
                if (_avatars == null) return;
                using var ofd = new OpenFileDialog
                {
                    Title = "Choisir une nouvelle photo de profil",
                    Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    if (!_avatars.SetPhoto(_ownerId, ofd.FileName))
                    {
                        MessageBox.Show(this, "Impossible de charger cette image.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var lblTheme = new Label
            {
                Text = "Interface :",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(left, 238),
                AutoSize = true
            };
            _themeMode = new ComboBox
            {
                Location = new Point(left, 261),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary
            };
            _themeMode.Items.AddRange(new object[] { "Sombre", "Clair" });
            _themeMode.SelectedIndex = Theme.AppBackground.R > 200 ? 1 : 0;

            // Couleur
            var lblColor = new Label
            {
                Text = "Couleur d'accentuation :",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(left, 290),
                AutoSize = true
            };
            _btnColor = new Button
            {
                Location = new Point(left, 313),
                Width = 120,
                Height = 30,
                BackColor = _selectedColor,
                Text = "Choisir couleur",
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnColor.FlatAppearance.BorderSize = 0;
            _btnColor.Click += (s, e) =>
            {
                using var cd = new ColorDialog { Color = _selectedColor };
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedColor = cd.Color;
                    _btnColor.BackColor = _selectedColor;
                }
            };

            // Boutons
            var btnOk = new Button
            {
                Text = "Enregistrer",
                DialogResult = DialogResult.OK,
                Location = new Point(190, 365),
                Width = 90,
                Height = 32,
                BackColor = Theme.Accent,
                ForeColor = Theme.AccentText,
                FlatStyle = FlatStyle.Flat
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Annuler",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 365),
                Width = 90,
                Height = 32,
                BackColor = Theme.Panel,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblIp);
            Controls.Add(_txtIp);
            Controls.Add(lblGuid);
            Controls.Add(btnPhoto);
            Controls.Add(_txtGuid);
            Controls.Add(lblTheme);
            Controls.Add(_themeMode);
            Controls.Add(lblColor);
            Controls.Add(_btnColor);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }
    }
}
