#nullable enable

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Models;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Contrôle de saisie et d'envoi de messages avec bandeau de réponse et dimensionnement dynamique.
    /// </summary>
    public sealed class MessageInputControl : UserControl
    {
        private readonly TextBox _textBox;
        private readonly Label _placeholderLabel;
        private readonly Button _emojiButton;
        private readonly Button _attachmentButton;
        private readonly Button _voiceButton;
        private MessageViewItem? _replyTarget;
        private MessageViewItem? _editTarget;

        private Rectangle _replyBannerRect;
        private Rectangle _replyCloseButtonRect;
        private Rectangle _inputContainerRect;
        private Rectangle _sendButtonRect;

        private Font _fontInput;
        private Font _fontReplyName;
        private Font _fontReplyText;
        private Font _fontClose;

        /// <summary>
        /// Événement déclenché lors d'une demande d'envoi de message.
        /// </summary>
        public event Action<string, Guid?>? SendRequested;

        /// <summary>
        /// Événement déclenché lors d'une demande de modification d'un message existant.
        /// </summary>
        public event Action<Guid, string>? EditRequested;

        /// <summary>
        /// Événement déclenché lors de l'annulation du mode modification.
        /// </summary>
        public event Action? EditCancelled;
        public event Action<string>? AttachmentRequested;
        public event Action? EmojiRequested;
        public event Action? VoiceRequested;

        /// <summary>
        /// Indique si le contrôle est actuellement en mode modification de message.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEditing => _editTarget != null;

        /// <summary>
        /// Obtient ou définit le texte saisi.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [AllowNull]
        public override string Text
        {
            get => _textBox.Text ?? string.Empty;
            set
            {
                _textBox.Text = value ?? string.Empty;
                UpdatePlaceholderVisibility();
                AdjustLayoutAndHeight();
            }
        }

        /// <inheritdoc />
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool Enabled
        {
            get => base.Enabled;
            set
            {
                base.Enabled = value;
                _textBox.Enabled = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Initialise une nouvelle instance de <see cref="MessageInputControl"/>.
        /// </summary>
        public MessageInputControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            BackColor = Theme.Header;

            _fontInput = new Font("Segoe UI", 9.75f, FontStyle.Regular);
            _fontReplyName = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            _fontReplyText = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            _fontClose = new Font("Segoe UI", 11f, FontStyle.Bold);

            _textBox = new TextBox
            {
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.SearchBackground,
                ForeColor = Theme.TextPrimary,
                Font = _fontInput,
                ScrollBars = ScrollBars.None
            };
            _textBox.TextChanged += (s, e) =>
            {
                UpdatePlaceholderVisibility();
                AdjustLayoutAndHeight();
                Invalidate();
            };
            _textBox.KeyDown += OnTextBoxKeyDown;

            _placeholderLabel = new Label
            {
                Text = "Écrire un message",
                Font = _fontInput,
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.SearchBackground,
                AutoSize = true,
                Cursor = Cursors.IBeam
            };
            _placeholderLabel.Click += (s, e) => _textBox.Focus();

            _emojiButton = CreateToolButton("☺", "Emojis", (s, e) => EmojiRequested?.Invoke());
            _attachmentButton = CreateToolButton("📎", "Joindre un fichier, une photo, une vidéo ou un audio", OnAttachmentClicked);
            _voiceButton = CreateToolButton("🎙", "Enregistrer un message vocal", (s, e) => VoiceRequested?.Invoke());

            Controls.Add(_placeholderLabel);
            Controls.Add(_textBox);
            Controls.Add(_emojiButton);
            Controls.Add(_attachmentButton);
            Controls.Add(_voiceButton);

            AdjustLayoutAndHeight();
        }

        private static Button CreateToolButton(string text, string tooltip, EventHandler handler)
        {
            var button = new Button
            {
                Text = text, FlatStyle = FlatStyle.Flat, BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary, Font = new Font("Segoe UI Emoji", 12f),
                Cursor = Cursors.Hand, TabStop = false, Size = new Size(32, 32)
            };
            button.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(button, tooltip);
            button.Click += handler;
            return button;
        }

        private void OnAttachmentClicked(object? sender, EventArgs e)
        {
            using var menu = new AttachmentMenu();
            menu.AttachmentRequested += (s, kind) => AttachmentRequested?.Invoke(kind);
            menu.Show(_attachmentButton, new Point(0, -menu.PreferredSize.Height));
        }

        private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (_editTarget != null)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    CancelEdit();
                    return;
                }
                if (_replyTarget != null)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    SetReplyTarget(null);
                    return;
                }
            }

            if (e.KeyCode == Keys.Enter)
            {
                if (e.Shift)
                {
                    // Maj+Entrée insère un saut de ligne standard
                }
                else
                {
                    // Entrée seule envoie le message et supprime le bip sonore
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    TriggerSend();
                }
            }
        }

        private void TriggerSend()
        {
            if (!Enabled)
            {
                return;
            }

            if (_editTarget != null)
            {
                string text = _textBox.Text.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    // Texte vide ou espaces : ignoré (ne supprime pas)
                    return;
                }
                if (text == _editTarget.Text)
                {
                    // Texte inchangé : quitte simplement le mode
                    CancelEdit();
                    return;
                }

                EditRequested?.Invoke(_editTarget.Id, text);
                return;
            }

            string content = _textBox.Text.Trim();
            if (!string.IsNullOrEmpty(content))
            {
                SendRequested?.Invoke(content, _replyTarget?.Id);
            }
        }

        /// <summary>
        /// Efface le texte de saisie.
        /// </summary>
        public void Clear()
        {
            _textBox.Text = string.Empty;
            UpdatePlaceholderVisibility();
            AdjustLayoutAndHeight();
            Invalidate();
        }

        /// <summary>
        /// Donne le focus au champ de saisie.
        /// </summary>
        public void FocusInput()
        {
            if (_textBox.CanFocus)
            {
                _textBox.Focus();
            }
        }

        /// <summary>
        /// Définit ou masque le message cité en réponse.
        /// </summary>
        /// <param name="item">Message cible ou null pour annuler la réponse.</param>
        public void SetReplyTarget(MessageViewItem? item)
        {
            if (item != null && _editTarget != null)
            {
                _editTarget = null;
                EditCancelled?.Invoke();
            }

            _replyTarget = item;
            AdjustLayoutAndHeight();
            Invalidate();
        }

        /// <summary>
        /// Active ou masque le mode modification pour un message donné.
        /// </summary>
        /// <param name="item">Message à modifier ou null pour annuler.</param>
        public void SetEditTarget(MessageViewItem? item)
        {
            _replyTarget = null;
            _editTarget = item;

            if (item != null)
            {
                _textBox.Text = item.Text;
                _textBox.SelectionStart = _textBox.Text.Length;
                _textBox.SelectionLength = 0;
                UpdatePlaceholderVisibility();
                AdjustLayoutAndHeight();
                Invalidate();
                FocusInput();
            }
            else
            {
                AdjustLayoutAndHeight();
                Invalidate();
            }
        }

        /// <summary>
        /// Annule le mode modification et réinitialise la cible.
        /// </summary>
        public void CancelEdit()
        {
            _editTarget = null;
            AdjustLayoutAndHeight();
            Invalidate();
            EditCancelled?.Invoke();
        }

        private void UpdatePlaceholderVisibility()
        {
            _placeholderLabel.Visible = string.IsNullOrEmpty(_textBox.Text);
        }

        private void AdjustLayoutAndHeight()
        {
            int padH = LogicalToDeviceUnits(16);
            int padV = LogicalToDeviceUnits(8);
            int sendBtnSize = LogicalToDeviceUnits(38);
            int gap = LogicalToDeviceUnits(10);

            bool showBanner = _replyTarget != null || _editTarget != null;
            int replyHeight = showBanner ? LogicalToDeviceUnits(46) : 0;

            int toolSize = LogicalToDeviceUnits(32);
            int toolGap = LogicalToDeviceUnits(4);
            int toolsWidth = (toolSize * 3) + (toolGap * 2) + gap;
            int containerWidth = Math.Max(LogicalToDeviceUnits(100), Width - (padH * 2) - sendBtnSize - gap - toolsWidth);
            int textInnerPadH = LogicalToDeviceUnits(12);
            int textInnerPadV = LogicalToDeviceUnits(8);
            int textBoxWidth = Math.Max(LogicalToDeviceUnits(80), containerWidth - (textInnerPadH * 2));

            // Calcul du nombre de lignes pour la hauteur dynamique (1 à 6 lignes)
            int singleLineHeight = TextRenderer.MeasureText("A", _fontInput).Height;
            string textToMeasure = string.IsNullOrEmpty(_textBox.Text) ? "A" : _textBox.Text;

            var measuredSize = TextRenderer.MeasureText(
                textToMeasure,
                _fontInput,
                new Size(textBoxWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            int estimatedLines = Math.Max(1, (int)Math.Ceiling((double)measuredSize.Height / singleLineHeight));
            int clampedLines = Math.Clamp(estimatedLines, 1, 6);

            _textBox.ScrollBars = estimatedLines > 6 ? ScrollBars.Vertical : ScrollBars.None;

            int textBoxHeight = clampedLines * singleLineHeight;
            int containerHeight = textBoxHeight + (textInnerPadV * 2);

            int totalHeight = replyHeight + containerHeight + (padV * 2);
            if (Height != totalHeight && totalHeight > 0)
            {
                Height = totalHeight;
            }

            // Disposition des rectangles
            int currentY = 0;
            if (showBanner)
            {
                _replyBannerRect = new Rectangle(0, 0, Width, replyHeight);
                int closeSize = LogicalToDeviceUnits(28);
                _replyCloseButtonRect = new Rectangle(Width - padH - closeSize, (replyHeight - closeSize) / 2, closeSize, closeSize);
                currentY = replyHeight;
            }
            else
            {
                _replyBannerRect = Rectangle.Empty;
                _replyCloseButtonRect = Rectangle.Empty;
            }

            int inputY = currentY + padV;
            _inputContainerRect = new Rectangle(padH + toolsWidth, inputY, containerWidth, containerHeight);

            int sendBtnY = inputY + (containerHeight - sendBtnSize) / 2;
            _sendButtonRect = new Rectangle(_inputContainerRect.Right + gap, sendBtnY, sendBtnSize, sendBtnSize);

            int toolY = inputY + (containerHeight - toolSize) / 2;
            _emojiButton.Bounds = new Rectangle(padH, toolY, toolSize, toolSize);
            _attachmentButton.Bounds = new Rectangle(padH + toolSize + toolGap, toolY, toolSize, toolSize);
            _voiceButton.Bounds = new Rectangle(padH + (toolSize + toolGap) * 2, toolY, toolSize, toolSize);
            _emojiButton.Enabled = Enabled;
            _attachmentButton.Enabled = Enabled;
            _voiceButton.Enabled = Enabled;

            // Positionnement du TextBox et du Placeholder
            _textBox.Bounds = new Rectangle(
                _inputContainerRect.X + textInnerPadH,
                _inputContainerRect.Y + textInnerPadV,
                textBoxWidth,
                textBoxHeight);

            _placeholderLabel.Location = new Point(
                _textBox.Left + LogicalToDeviceUnits(2),
                _textBox.Top + LogicalToDeviceUnits(1));
        }

        /// <inheritdoc/>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustLayoutAndHeight();
            Invalidate();
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Clic sur le bouton d'annulation de bandeau « × »
            bool showBanner = _replyTarget != null || _editTarget != null;
            if (showBanner && _replyCloseButtonRect.Contains(e.Location))
            {
                if (_editTarget != null)
                {
                    CancelEdit();
                }
                else
                {
                    SetReplyTarget(null);
                }
                return;
            }

            // Clic sur le bouton d'envoi
            if (_sendButtonRect.Contains(e.Location))
            {
                TriggerSend();
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool showBanner = _replyTarget != null || _editTarget != null;
            bool isOverClose = showBanner && _replyCloseButtonRect.Contains(e.Location);
            bool isOverSend = _sendButtonRect.Contains(e.Location) && !string.IsNullOrWhiteSpace(_textBox.Text);

            Cursor = (isOverClose || isOverSend) ? Cursors.Hand : Cursors.Default;
        }

        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Theme.Header);

            // Bordure supérieure
            using (var borderPen = new Pen(Theme.Separator))
            {
                g.DrawLine(borderPen, 0, 0, Width, 0);
            }

            // 1. Bandeau optionnel (réponse ou modification)
            if ((_replyTarget != null || _editTarget != null) && !_replyBannerRect.IsEmpty)
            {
                using (var brushBg = new SolidBrush(Theme.Hover))
                {
                    g.FillRectangle(brushBg, _replyBannerRect);
                }

                using (var sepPen = new Pen(Theme.Separator))
                {
                    g.DrawLine(sepPen, 0, _replyBannerRect.Bottom - 1, Width, _replyBannerRect.Bottom - 1);
                }

                int padH = LogicalToDeviceUnits(16);
                int barW = LogicalToDeviceUnits(3);
                int barH = _replyBannerRect.Height - LogicalToDeviceUnits(12);
                int barY = _replyBannerRect.Y + LogicalToDeviceUnits(6);

                Color barColor = Theme.Accent;
                using (var barBrush = new SolidBrush(barColor))
                {
                    g.FillRectangle(barBrush, padH, barY, barW, barH);
                }

                int textX = padH + barW + LogicalToDeviceUnits(8);
                int textW = _replyCloseButtonRect.Left - textX - LogicalToDeviceUnits(8);
                int halfH = barH / 2;

                string headerText = _editTarget != null ? "Modification du message" : (_replyTarget?.AuthorName ?? string.Empty);
                string previewRaw = _editTarget != null
                    ? (_editTarget.IsDeleted ? "Message supprimé" : _editTarget.Text)
                    : (_replyTarget != null ? (_replyTarget.IsDeleted ? "Message supprimé" : _replyTarget.Text) : string.Empty);
                string previewText = previewRaw.Replace("\r\n", " ").Replace("\n", " ");

                var nameRect = new Rectangle(textX, barY, textW, halfH);
                TextRenderer.DrawText(g, headerText, _fontReplyName, nameRect, Theme.Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                var previewRect = new Rectangle(textX, barY + halfH, textW, halfH);
                TextRenderer.DrawText(g, previewText, _fontReplyText, previewRect, Theme.TextSecondary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                // Bouton « × »
                TextRenderer.DrawText(g, "×", _fontClose, _replyCloseButtonRect, Theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            // 2. Conteneur arrondi du champ de saisie
            int radius = LogicalToDeviceUnits(10);
            DrawRoundedRectangle(g, _inputContainerRect, radius, Theme.SearchBackground);

            // 3. Bouton d'envoi rond
            bool canSend = !string.IsNullOrWhiteSpace(_textBox.Text) && Enabled;
            Color btnColor = canSend ? Theme.Accent : Theme.SearchBackground;
            Color arrowColor = canSend ? Theme.AccentText : Theme.TextSecondary;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var btnBrush = new SolidBrush(btnColor))
            {
                g.FillEllipse(btnBrush, _sendButtonRect);
            }

            // Flèche d'envoi dessinée en GDI+
            DrawSendArrow(g, _sendButtonRect, arrowColor);
        }

        private void DrawSendArrow(Graphics g, Rectangle bounds, Color arrowColor)
        {
            int cx = bounds.X + (bounds.Width / 2);
            int cy = bounds.Y + (bounds.Height / 2);
            int s = LogicalToDeviceUnits(6);

            var points = new[]
            {
                new Point(cx - s + LogicalToDeviceUnits(1), cy - s),
                new Point(cx + s + LogicalToDeviceUnits(1), cy),
                new Point(cx - s + LogicalToDeviceUnits(1), cy + s),
                new Point(cx - (s / 2) + LogicalToDeviceUnits(1), cy)
            };

            using var brush = new SolidBrush(arrowColor);
            g.FillPolygon(brush, points);
        }

        private static void DrawRoundedRectangle(Graphics g, Rectangle bounds, int radius, Color fillColor)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            using var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(fillColor);
            g.FillPath(brush, path);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontInput.Dispose();
                _fontReplyName.Dispose();
                _fontReplyText.Dispose();
                _fontClose.Dispose();
                _emojiButton.Dispose();
                _attachmentButton.Dispose();
                _voiceButton.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
