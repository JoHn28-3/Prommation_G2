#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using WHATSAPP_CHAPT.App.Validation;
using WHATSAPP_CHAPT.App.Controls;

namespace WHATSAPP_CHAPT.App.Forms
{
    /// <summary>
    /// Form to create a new group – title + contact selection.
    /// Emits <see cref="GroupCreated"/> when the user validates.
    /// </summary>
    public class NewGroupForm : Form
    {
        private readonly TextBox _titleBox = new();
        private readonly ContactPickerControl _picker;
        private readonly Button _okButton = new();
        private readonly Button _cancelButton = new();

        public NewGroupForm(IEnumerable<string> availableContacts)
        {
            Text = "Créer un groupe";
            Size = new Size(400, 500);
            StartPosition = FormStartPosition.CenterParent;

            _picker = new ContactPickerControl();
            _picker.SetContacts(availableContacts);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = new Padding(10)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // title
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // label
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // picker
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // buttons

            var titleLabel = new Label { Text = "Titre du groupe", AutoSize = true };
            _titleBox.Dock = DockStyle.Fill;

            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            _okButton.Text = "Créer";
            _okButton.DialogResult = DialogResult.OK;
            _okButton.Click += OkButton_Click;
            _cancelButton.Text = "Annuler";
            _cancelButton.DialogResult = DialogResult.Cancel;
            buttonPanel.Controls.Add(_okButton);
            buttonPanel.Controls.Add(_cancelButton);

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(_titleBox, 0, 1);
            layout.Controls.Add(_picker, 0, 2);
            layout.Controls.Add(buttonPanel, 0, 3);

            Controls.Add(layout);
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            var title = _titleBox.Text.Trim();
            var members = _picker.SelectedContacts.ToList();
            var validation = NewGroupValidation.Validate(title, members);
            if (!validation.IsValid)
            {
                MessageBox.Show(validation.ErrorMessage, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None; // keep dialog open
                return;
            }

            GroupCreated?.Invoke(this, new GroupCreatedEventArgs(title, members));
        }

        public event EventHandler<GroupCreatedEventArgs>? GroupCreated;
    }

    public class GroupCreatedEventArgs : EventArgs
    {
        public string Title { get; }
        public IReadOnlyList<string> Members { get; }

        public GroupCreatedEventArgs(string title, IReadOnlyList<string> members)
        {
            Title = title;
            Members = members;
        }
    }
}
