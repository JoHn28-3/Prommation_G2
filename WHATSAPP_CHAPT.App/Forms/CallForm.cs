#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Type d'appel P2P.
    /// </summary>
    public enum CallType
    {
        Audio,
        Video
    }

    /// <summary>
    /// Fenêtre d'appel peer-to-peer audio et vidéo (Phase H).
    /// </summary>
    public sealed class CallForm : Form
    {
        private readonly string _peerName;
        private readonly CallType _callType;
        private readonly Label _lblPeer;
        private readonly Label _lblStatus;
        private readonly Panel _videoPanel;
        private readonly Button _btnMute;
        private readonly Button _btnVideo;
        private readonly Button _btnEnd;
        private readonly System.Windows.Forms.Timer _callTimer = new() { Interval = 1000 };

        private bool _isMuted = false;
        private bool _isVideoOn = false;
        private int _secondsElapsed = 0;

        public CallForm(string peerName, CallType callType)
        {
            _peerName = peerName;
            _callType = callType;
            _isVideoOn = callType == CallType.Video;

            Text = _callType == CallType.Video ? $"Appel vidéo - {_peerName}" : $"Appel audio - {_peerName}";
            Size = new Size(540, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(20, 26, 31);
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Panel d'en-tête (Nom + Statut)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(16, 22, 26)
            };

            _lblPeer = new Label
            {
                Text = _peerName,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblStatus = new Label
            {
                Text = "Connexion P2P en cours...",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter
            };

            topPanel.Controls.Add(_lblStatus);
            topPanel.Controls.Add(_lblPeer);

            // Zone vidéo
            _videoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 16, 20)
            };
            _videoPanel.Paint += OnVideoPanelPaint;

            // Barre de contrôle en bas
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 110,
                BackColor = Color.FromArgb(16, 22, 26),
                Padding = new Padding(20)
            };

            var controlsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            _btnMute = new Button
            {
                Text = "🎙 Micro",
                Size = new Size(110, 48),
                BackColor = Color.FromArgb(40, 50, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _btnMute.FlatAppearance.BorderSize = 0;
            _btnMute.Click += (s, e) =>
            {
                _isMuted = !_isMuted;
                _btnMute.Text = _isMuted ? "🔇 Muet" : "🎙 Micro";
                _btnMute.BackColor = _isMuted ? Color.FromArgb(180, 50, 50) : Color.FromArgb(40, 50, 60);
            };

            _btnVideo = new Button
            {
                Text = _isVideoOn ? "📷 Vidéo ON" : "📷 Vidéo OFF",
                Size = new Size(120, 48),
                BackColor = _isVideoOn ? Theme.Accent : Color.FromArgb(40, 50, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _btnVideo.FlatAppearance.BorderSize = 0;
            _btnVideo.Click += (s, e) =>
            {
                _isVideoOn = !_isVideoOn;
                _btnVideo.Text = _isVideoOn ? "📷 Vidéo ON" : "📷 Vidéo OFF";
                _btnVideo.BackColor = _isVideoOn ? Theme.Accent : Color.FromArgb(40, 50, 60);
                _videoPanel.Invalidate();
            };

            _btnEnd = new Button
            {
                Text = "🔴 Raccrocher",
                Size = new Size(130, 48),
                BackColor = Color.FromArgb(220, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            _btnEnd.FlatAppearance.BorderSize = 0;
            _btnEnd.Click += (s, e) =>
            {
                _callTimer.Stop();
                _lblStatus.Text = "Appel terminé";
                Close();
            };

            controlsFlow.Controls.Add(_btnMute);
            controlsFlow.Controls.Add(_btnVideo);
            controlsFlow.Controls.Add(_btnEnd);
            bottomPanel.Controls.Add(controlsFlow);

            Controls.Add(_videoPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            _callTimer.Tick += (s, e) =>
            {
                _secondsElapsed++;
                int mins = _secondsElapsed / 60;
                int secs = _secondsElapsed % 60;
                _lblStatus.Text = $"En communication ({mins:D2}:{secs:D2})";
                _lblStatus.ForeColor = Color.FromArgb(40, 190, 80);
            };

            // Démarrer après 1s
            var startTimer = new System.Windows.Forms.Timer { Interval = 1200 };
            startTimer.Tick += (s, e) =>
            {
                startTimer.Stop();
                startTimer.Dispose();
                _callTimer.Start();
                _lblStatus.Text = "En communication (00:00)";
                _lblStatus.ForeColor = Color.FromArgb(40, 190, 80);
            };
            startTimer.Start();
        }

        private void OnVideoPanelPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(12, 16, 20));

            if (_isVideoOn)
            {
                // Rendu visuel d'un flux vidéo P2P
                using var brush = new SolidBrush(Color.FromArgb(30, 40, 50));
                g.FillRectangle(brush, 40, 40, _videoPanel.Width - 80, _videoPanel.Height - 80);

                using var borderPen = new Pen(Theme.Accent, 2f);
                g.DrawRectangle(borderPen, 40, 40, _videoPanel.Width - 80, _videoPanel.Height - 80);

                using var font = new Font("Segoe UI", 14f, FontStyle.Bold);
                TextRenderer.DrawText(g, $"Flux vidéo direct : {_peerName}", font,
                    new Rectangle(40, 40, _videoPanel.Width - 80, _videoPanel.Height - 80),
                    Theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            else
            {
                // Rendu audio seul (avatar centré)
                int size = 120;
                int x = (_videoPanel.Width - size) / 2;
                int y = (_videoPanel.Height - size) / 2;

                using var avatarBrush = new SolidBrush(Theme.Accent);
                g.FillEllipse(avatarBrush, x, y, size, size);

                using var font = new Font("Segoe UI", 32f, FontStyle.Bold);
                string initial = string.IsNullOrEmpty(_peerName) ? "?" : _peerName[..1].ToUpperInvariant();
                TextRenderer.DrawText(g, initial, font, new Rectangle(x, y, size, size),
                    Theme.AccentText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _callTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
