#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

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
        private Color _selectedColor;

        public string DisplayName => _txtName.Text.Trim();
        public string LocalIp => _txtIp.Text.Trim();
        public string Identifier => _txtGuid.Text.Trim();
        public Color ThemeColor => _selectedColor;

        public SettingsForm(string currentName, string currentIp, Guid currentId, Color currentColor)
        {
            _selectedColor = currentColor;

            Text = "Paramètres";
            Size = new Size(420, 360);
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

            // Couleur
            var lblColor = new Label
            {
                Text = "Couleur d'accentuation :",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(left, 195),
                AutoSize = true
            };
            _btnColor = new Button
            {
                Location = new Point(left, 218),
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
                Location = new Point(190, 270),
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
                Location = new Point(290, 270),
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
            Controls.Add(_txtGuid);
            Controls.Add(lblColor);
            Controls.Add(_btnColor);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }
    }
}
