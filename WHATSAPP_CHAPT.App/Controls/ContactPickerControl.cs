#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WHATSAPP_CHAPT.App.Controls
{
    /// <summary>
    /// Simple contact picker control used in the group creation flow.
    /// Allows searching and multi‑selection of contacts.
    /// Left‑click on a contact opens a context menu with Delete, Archive, View Profile.
    /// </summary>
    public class ContactPickerControl : UserControl
    {
        private readonly TextBox _searchBox = new();
        private readonly ListBox _listBox = new();
        private readonly ContextMenuStrip _contextMenu = new();

        /// <summary>
        /// The contacts currently displayed (filtered).
        /// </summary>
        public IReadOnlyList<string> SelectedContacts => _listBox.SelectedItems.Cast<string>().ToList();

        public ContactPickerControl()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Layout
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Search box
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.PlaceholderText = "Rechercher un contact...";
            _searchBox.TextChanged += (s, e) => ApplyFilter();

            // List box
            _listBox.Dock = DockStyle.Fill;
            _listBox.SelectionMode = SelectionMode.MultiExtended;
            _listBox.MouseDown += ListBox_MouseDown;

            // Context menu
            var miDelete = new ToolStripMenuItem("Supprimer");
            miDelete.Click += (s, e) => OnDeleteClicked();
            var miArchive = new ToolStripMenuItem("Archiver");
            miArchive.Click += (s, e) => OnArchiveClicked();
            var miView = new ToolStripMenuItem("Voir le profil");
            miView.Click += (s, e) => OnViewProfileClicked();
            _contextMenu.Items.AddRange(new[] { miDelete, miArchive, miView });

            // Assemble
            panel.Controls.Add(_searchBox, 0, 0);
            panel.Controls.Add(_listBox, 0, 1);
            Controls.Add(panel);
        }

        private void ListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = _listBox.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    _listBox.SelectedIndex = index;
                    _contextMenu.Show(_listBox, e.Location);
                }
            }
        }

        private void ApplyFilter()
        {
            var filter = _searchBox.Text.Trim();
            // In real code, source would be injected; here we just keep existing items that match.
            for (int i = _listBox.Items.Count - 1; i >= 0; i--)
            {
                var item = _listBox.Items[i] as string;
                if (item != null && !item.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    // hide by removing – a real implementation would keep a master list.
                }
            }
            // For the prototype we assume the full list is already set by the caller.
        }

        // ---- Context‑menu actions (place‑holder) ----
        public event EventHandler<string>? DeleteRequested;
        public event EventHandler<string>? ArchiveRequested;
        public event EventHandler<string>? ViewProfileRequested;

        private void OnDeleteClicked()
        {
            var contact = _listBox.SelectedItem as string;
            if (contact != null) DeleteRequested?.Invoke(this, contact);
        }

        private void OnArchiveClicked()
        {
            var contact = _listBox.SelectedItem as string;
            if (contact != null) ArchiveRequested?.Invoke(this, contact);
        }

        private void OnViewProfileClicked()
        {
            var contact = _listBox.SelectedItem as string;
            if (contact != null) ViewProfileRequested?.Invoke(this, contact);
        }

        /// <summary>
        /// Populate the control with a list of contacts.
        /// </summary>
        public void SetContacts(IEnumerable<string> contacts)
        {
            _listBox.Items.Clear();
            foreach (var c in contacts)
                _listBox.Items.Add(c);
        }
    }
}
