#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Services;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Panneau de conversation Meta AI intégrant GroqAiService.
    /// - Affiche l'historique local de la conversation (en mémoire uniquement).
    /// - Zone de saisie + bouton Envoyer + indicateur de traitement.
    /// - Ne bloque jamais le thread UI.
    /// </summary>
    public sealed class MetaAiForm : Form
    {
        private readonly GroqAiService _groq = new GroqAiService();
        private readonly RichTextBox _historyBox;
        private readonly TextBox _inputBox;
        private readonly Button _btnSend;
        private readonly Label _lblStatus;
        private CancellationTokenSource? _cts;

        private readonly List<(string Role, string Text)> _history = new();

        public MetaAiForm()
        {
            Text = "Meta AI";
            Size = new Size(600, 500);
            MinimumSize = new Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(18, 18, 18);
            ForeColor = Color.FromArgb(220, 220, 220);
            FormBorderStyle = FormBorderStyle.Sizable;
            Font = new Font("Segoe UI", 9.5f);

            // --- Titre ---
            var lblTitle = new Label
            {
                Text = "Meta AI",
                Dock = DockStyle.Top,
                Height = 44,
                ForeColor = Color.FromArgb(100, 160, 255),
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(25, 25, 25),
                Image = LoadLogo(),
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 12, 0)
            };

            // --- Historique ---
            _historyBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.FromArgb(210, 210, 210),
                Font = new Font("Segoe UI", 9.5f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true
            };

            // --- Statut ---
            _lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(100, 200, 100),
                Font = new Font("Segoe UI", 8f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(18, 18, 18),
                Text = _groq.HasKey ? "Prêt · Groq connecté" : "Clé Groq non configurée"
            };

            if (!_groq.HasKey) _lblStatus.ForeColor = Color.FromArgb(220, 150, 50);

            // --- Zone de saisie ---
            var inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(25, 25, 25),
                Padding = new Padding(8)
            };

            _inputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(38, 38, 38),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "Posez votre question à Meta AI…"
            };
            _inputBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    SendMessageAsync();
                }
            };

            _btnSend = new Button
            {
                Dock = DockStyle.Right,
                Width = 80,
                Text = "Envoyer",
                BackColor = Color.FromArgb(0, 168, 132),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += (s, e) => SendMessageAsync();

            inputPanel.Controls.Add(_inputBox);
            inputPanel.Controls.Add(_btnSend);

            // --- Assemblage ---
            Controls.Add(_historyBox);
            Controls.Add(_lblStatus);
            Controls.Add(inputPanel);
            Controls.Add(lblTitle);

            AppendHistory("Meta AI", _groq.HasKey
                ? "Bonjour ! Je suis Meta AI, propulsé par Groq. Comment puis-je vous aider ?"
                : "Bonjour ! Ajoutez votre clé Groq dans les paramètres pour activer les réponses.");
        }

        private async void SendMessageAsync()
        {
            var text = _inputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            _inputBox.Clear();
            _btnSend.Enabled = false;
            _lblStatus.Text = "Traitement…";
            _lblStatus.ForeColor = Color.FromArgb(100, 160, 255);

            AppendHistory("Vous", text);
            _history.Add(("user", text));

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            var (response, error) = await _groq.AskAsync(text, _cts.Token);

            if (!IsDisposed)
            {
                if (error != null)
                {
                    AppendHistory("⚠ Erreur", error);
                    _lblStatus.Text = "Erreur de connexion";
                    _lblStatus.ForeColor = Color.FromArgb(220, 80, 80);
                }
                else
                {
                    var answer = response ?? "[Réponse vide]";
                    _history.Add(("assistant", answer));
                    AppendHistory("Meta AI", answer);
                    _lblStatus.Text = _groq.HasKey ? "Prêt · Groq connecté" : "Clé Groq non configurée";
                    _lblStatus.ForeColor = _groq.HasKey ? Color.FromArgb(100, 200, 100) : Color.FromArgb(220, 150, 50);
                }

                _btnSend.Enabled = true;
            }
        }

        private static Image? LoadLogo()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "LOGO", "asset.png");
            return System.IO.File.Exists(path) ? new Bitmap(path) : null;
        }

        private void AppendHistory(string author, string text)
        {
            _historyBox.AppendText($"\n── {author} ──\n{text}\n");
            _historyBox.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _groq.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
