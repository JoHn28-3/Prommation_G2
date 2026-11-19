#nullable enable

using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Boîte de dialogue pour ajouter un nouveau contact P2P via son adresse IP.
    /// </summary>
    public sealed class NewContactForm : Form
    {
        private readonly TextBox _txtName;
        private readonly TextBox _txtIp;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public string ContactName => _txtName.Text.Trim();
        public string IpAddress => _txtIp.Text.Trim();

        public NewContactForm()
        {
            Text = "Nouveau contact (P2P)";
            Size = new Size(380, 240);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.AppBackground;
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblName = new Label
            {
                Text = "Nom du contact :",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextPrimary,
                Location = new Point(20, 20),
                AutoSize = true
            };

            _txtName = new TextBox
            {
                Location = new Point(20, 45),
                Width = 320,
                Font = new Font("Segoe UI", 10f),
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Ex: Alice"
            };

            var lblIp = new Label
            {
                Text = "Adresse IP du pair :",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextPrimary,
                Location = new Point(20, 85),
                AutoSize = true
            };

            _txtIp = new TextBox
            {
                Location = new Point(20, 110),
                Width = 320,
                Font = new Font("Segoe UI", 10f),
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Ex: 192.168.1.42 ou 127.0.0.1"
            };

            _btnOk = new Button
            {
                Text = "Ajouter",
                DialogResult = DialogResult.OK,
                Location = new Point(160, 155),
                Width = 85,
                Height = 32,
                BackColor = Theme.Accent,
                ForeColor = Theme.AccentText,
                FlatStyle = FlatStyle.Flat
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += OnOkClick;

            _btnCancel = new Button
            {
                Text = "Annuler",
                DialogResult = DialogResult.Cancel,
                Location = new Point(255, 155),
                Width = 85,
                Height = 32,
                BackColor = Theme.Panel,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblIp);
            Controls.Add(_txtIp);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show(this, "Le nom du contact ne peut pas être vide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtIp.Text) || !IPAddress.TryParse(_txtIp.Text.Trim(), out _))
            {
                MessageBox.Show(this, "Veuillez entrer une adresse IP valide (ex: 127.0.0.1).", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
        }
    }
}
