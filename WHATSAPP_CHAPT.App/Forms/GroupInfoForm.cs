#nullable enable

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WHATSAPP_CHAPT.App.Drawing;
using WHATSAPP_CHAPT.App.Models;
using WHATSAPP_CHAPT.Core.Models;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Fenêtre affichant les détails d'un groupe et la gestion de ses membres.
    /// </summary>
    public sealed class GroupInfoForm : Form
    {
        private readonly GroupInfoViewModel _viewModel;
        private readonly ListView _membersListView;
        private readonly Button _btnAddMember;
        private readonly Button _btnLeaveGroup;
        private readonly ContextMenuStrip _memberContextMenu;

        public event EventHandler<Guid>? RemoveMemberRequested;
        public event EventHandler? AddMemberRequested;
        public event EventHandler? LeaveGroupRequested;

        public GroupInfoForm(GroupInfoViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            Text = "Infos du groupe";
            Size = new Size(420, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.AppBackground;
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var titleLabel = new Label
            {
                Text = _viewModel.Title,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Dock = DockStyle.Top,
                Height = 45,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var countLabel = new Label
            {
                Text = $"{_viewModel.MemberCount} participants",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };

            _membersListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BackColor = Theme.Panel,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _membersListView.Columns.Add("Nom", 230);
            _membersListView.Columns.Add("Rôle", 120);

            _memberContextMenu = new ContextMenuStrip
            {
                BackColor = Theme.Header,
                ForeColor = Theme.TextPrimary
            };
            var removeMenuItem = new ToolStripMenuItem("Retirer du groupe");
            removeMenuItem.Click += (s, e) =>
            {
                if (_membersListView.SelectedItems.Count > 0 &&
                    _membersListView.SelectedItems[0].Tag is GroupMemberItem item &&
                    !item.IsCurrentUser)
                {
                    RemoveMemberRequested?.Invoke(this, item.UserId);
                }
            };
            _memberContextMenu.Items.Add(removeMenuItem);
            _membersListView.ContextMenuStrip = _memberContextMenu;

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.Panel,
                Padding = new Padding(12)
            };

            _btnAddMember = new Button
            {
                Text = "Ajouter un membre",
                BackColor = Theme.Accent,
                ForeColor = Theme.AccentText,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Left,
                Width = 150,
                Enabled = _viewModel.IsCurrentUserAdmin
            };
            _btnAddMember.FlatAppearance.BorderSize = 0;
            _btnAddMember.Click += (s, e) => AddMemberRequested?.Invoke(this, EventArgs.Empty);

            _btnLeaveGroup = new Button
            {
                Text = "Quitter le groupe",
                BackColor = Color.FromArgb(180, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 140
            };
            _btnLeaveGroup.FlatAppearance.BorderSize = 0;
            _btnLeaveGroup.Click += (s, e) =>
            {
                var res = MessageBox.Show(this, "Voulez-vous vraiment quitter ce groupe ?", "Quitter le groupe",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    LeaveGroupRequested?.Invoke(this, EventArgs.Empty);
                    Close();
                }
            };

            bottomPanel.Controls.Add(_btnAddMember);
            bottomPanel.Controls.Add(_btnLeaveGroup);

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.Header
            };
            topPanel.Controls.Add(countLabel);
            topPanel.Controls.Add(titleLabel);

            Controls.Add(_membersListView);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            PopulateMembers();
        }

        private void PopulateMembers()
        {
            _membersListView.Items.Clear();
            foreach (var m in _viewModel.Members)
            {
                var lvi = new ListViewItem(m.DisplayName);
                lvi.SubItems.Add(m.RoleDescription);
                lvi.Tag = m;
                if (m.IsCurrentUser)
                {
                    lvi.ForeColor = Theme.Accent;
                }
                _membersListView.Items.Add(lvi);
            }
        }
    }
}
