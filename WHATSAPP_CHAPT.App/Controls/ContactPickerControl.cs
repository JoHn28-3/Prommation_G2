#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Contrôle réutilisable pour rechercher et sélectionner des contacts (Phase D).
    /// Permet la multi-sélection et propose des actions contextuelles (Supprimer, Archiver, Voir le profil).
    /// </summary>
    public class ContactPickerControl : UserControl
    {
        private readonly TextBox _searchBox = new();
        private readonly ListBox _listBox = new();
        private readonly ContextMenuStrip _contextMenu = new();
        private readonly List<string> _allContacts = new();

        /// <summary>
        /// Contacts actuellement sélectionnés.
        /// </summary>
        public IReadOnlyList<string> SelectedContacts => _listBox.SelectedItems.Cast<string>().ToList();

        public ContactPickerControl()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Theme.Panel
            };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _searchBox.Dock = DockStyle.Fill;
            _searchBox.PlaceholderText = "Rechercher un contact...";
            _searchBox.BackColor = Theme.SearchBackground;
            _searchBox.ForeColor = Theme.TextPrimary;
            _searchBox.BorderStyle = BorderStyle.FixedSingle;
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            _listBox.Dock = DockStyle.Fill;
            _listBox.SelectionMode = SelectionMode.MultiExtended;
            _listBox.BackColor = Theme.Panel;
            _listBox.ForeColor = Theme.TextPrimary;
            _listBox.BorderStyle = BorderStyle.None;
            _listBox.MouseDown += ListBox_MouseDown;

            var miDelete = new ToolStripMenuItem("Supprimer");
            miDelete.Click += (s, e) => OnDeleteClicked();

            var miArchive = new ToolStripMenuItem("Archiver");
            miArchive.Click += (s, e) => OnArchiveClicked();

            var miView = new ToolStripMenuItem("Voir le profil");
            miView.Click += (s, e) => OnViewProfileClicked();

            _contextMenu.BackColor = Theme.Header;
            _contextMenu.ForeColor = Theme.TextPrimary;
            _contextMenu.Items.AddRange(new ToolStripItem[] { miDelete, miArchive, miView });

            panel.Controls.Add(_searchBox, 0, 0);
            panel.Controls.Add(_listBox, 0, 1);
            Controls.Add(panel);
        }

        private void ListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Left)
            {
                int index = _listBox.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    _listBox.SelectedIndex = index;
                    if (e.Button == MouseButtons.Right)
                    {
                        _contextMenu.Show(_listBox, e.Location);
                    }
                }
            }
        }

        private void ApplyFilter()
        {
            var filter = _searchBox.Text.Trim();
            _listBox.BeginUpdate();
            _listBox.Items.Clear();

            var matches = string.IsNullOrEmpty(filter)
                ? _allContacts
                : _allContacts.Where(c => c.Contains(filter, StringComparison.OrdinalIgnoreCase));

            foreach (var c in matches)
            {
                _listBox.Items.Add(c);
            }
            _listBox.EndUpdate();
        }

        public event EventHandler<string>? DeleteRequested;
        public event EventHandler<string>? ArchiveRequested;
        public event EventHandler<string>? ViewProfileRequested;

        private void OnDeleteClicked()
        {
            if (_listBox.SelectedItem is string contact)
            {
                DeleteRequested?.Invoke(this, contact);
            }
        }

        private void OnArchiveClicked()
        {
            if (_listBox.SelectedItem is string contact)
            {
                ArchiveRequested?.Invoke(this, contact);
            }
        }

        private void OnViewProfileClicked()
        {
            if (_listBox.SelectedItem is string contact)
            {
                ViewProfileRequested?.Invoke(this, contact);
            }
        }

        /// <summary>
        /// Remplit la liste des contacts disponibles.
        /// </summary>
        public void SetContacts(IEnumerable<string> contacts)
        {
            _allContacts.Clear();
            _allContacts.AddRange(contacts);
            ApplyFilter();
        }
    }
}
