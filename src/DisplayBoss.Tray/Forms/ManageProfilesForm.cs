using DisplayBoss.Core.Models;
using DisplayBoss.Core.Services;

namespace DisplayBoss.Tray.Forms;

public class ManageProfilesForm : Form
{
    private readonly ProfileService _profileService;
    private readonly ListView _listView;
    private readonly Button _loadButton;
    private readonly Button _deleteButton;
    private readonly Button _closeButton;

    public ManageProfilesForm(ProfileService profileService)
    {
        _profileService = profileService;

        Text = "Manage Profiles - DisplayBoss";
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(650, 380);
        MinimumSize = new Size(500, 300);

        _listView = new ListView
        {
            Location = new Point(12, 12),
            Size = new Size(520, 350),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        _listView.Columns.Add("Name", 150);
        _listView.Columns.Add("Monitors", 80);
        _listView.Columns.Add("Created", 130);
        _listView.Columns.Add("Description", 150);

        _listView.DoubleClick += LoadButton_Click;
        _listView.SelectedIndexChanged += ListView_SelectedIndexChanged;

        _loadButton = new Button
        {
            Text = "Load",
            Location = new Point(545, 12),
            Size = new Size(90, 30),
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _loadButton.Click += LoadButton_Click;

        _deleteButton = new Button
        {
            Text = "Delete",
            Location = new Point(545, 52),
            Size = new Size(90, 30),
            Enabled = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        _deleteButton.Click += DeleteButton_Click;

        _closeButton = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.Cancel,
            Location = new Point(545, 332),
            Size = new Size(90, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };

        CancelButton = _closeButton;

        Controls.AddRange(new Control[]
        {
            _listView, _loadButton, _deleteButton, _closeButton,
        });

        RefreshList();
    }

    private void ListView_SelectedIndexChanged(object? sender, EventArgs e)
    {
        bool hasSelection = _listView.SelectedItems.Count > 0;
        _loadButton.Enabled = hasSelection;
        _deleteButton.Enabled = hasSelection;
    }

    private void LoadButton_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0)
            return;

        string name = _listView.SelectedItems[0].Text;

        try
        {
            var result = _profileService.ApplyProfileByName(name);
            if (result.Success)
            {
                MessageBox.Show(
                    result.Message,
                    "DisplayBoss",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    result.Message,
                    "DisplayBoss",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error applying profile: {ex.Message}",
                "DisplayBoss",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (_listView.SelectedItems.Count == 0)
            return;

        string name = _listView.SelectedItems[0].Text;

        var confirm = MessageBox.Show(
            $"Delete profile '{name}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        _profileService.DeleteProfile(name);
        RefreshList();
    }

    private void RefreshList()
    {
        _listView.Items.Clear();

        var profiles = _profileService.ListProfiles();
        foreach (var profile in profiles)
        {
            var item = new ListViewItem(profile.Name);
            item.SubItems.Add(profile.Summary);
            item.SubItems.Add(profile.CreatedAt.ToLocalTime().ToString("g"));
            item.SubItems.Add(profile.Description);
            _listView.Items.Add(item);
        }

        _loadButton.Enabled = false;
        _deleteButton.Enabled = false;
    }
}
