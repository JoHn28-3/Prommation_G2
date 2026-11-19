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
        private MessageViewItem? _replyTarget;

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

            Controls.Add(_placeholderLabel);
            Controls.Add(_textBox);

            AdjustLayoutAndHeight();
        }

        private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
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
            string content = _textBox.Text.Trim();
            if (!string.IsNullOrEmpty(content) && Enabled)
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
            _replyTarget = item;
            AdjustLayoutAndHeight();
            Invalidate();
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

            int replyHeight = _replyTarget != null ? LogicalToDeviceUnits(46) : 0;

            int containerWidth = Math.Max(LogicalToDeviceUnits(100), Width - (padH * 2) - sendBtnSize - gap);
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
            if (_replyTarget != null)
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
            _inputContainerRect = new Rectangle(padH, inputY, containerWidth, containerHeight);

            int sendBtnY = inputY + (containerHeight - sendBtnSize) / 2;
            _sendButtonRect = new Rectangle(padH + containerWidth + gap, sendBtnY, sendBtnSize, sendBtnSize);

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

            // Clic sur le bouton d'annulation de réponse « × »
            if (_replyTarget != null && _replyCloseButtonRect.Contains(e.Location))
            {
                SetReplyTarget(null);
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

            bool isOverClose = _replyTarget != null && _replyCloseButtonRect.Contains(e.Location);
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

            // 1. Bandeau de réponse optionnel
            if (_replyTarget != null && !_replyBannerRect.IsEmpty)
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

                var nameRect = new Rectangle(textX, barY, textW, halfH);
                TextRenderer.DrawText(g, _replyTarget.AuthorName, _fontReplyName, nameRect, Theme.Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                var previewRect = new Rectangle(textX, barY + halfH, textW, halfH);
                string previewText = _replyTarget.IsDeleted ? "Message supprimé" : _replyTarget.Text.Replace("\r\n", " ").Replace("\n", " ");
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
            }

            base.Dispose(disposing);
        }
    }
}
