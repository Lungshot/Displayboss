namespace DisplayBoss.Tray.Forms;

public class SaveProfileForm : Form
{
    private readonly TextBox _nameTextBox;
    private readonly TextBox _descriptionTextBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public string ProfileName => _nameTextBox.Text.Trim();
    public string ProfileDescription => _descriptionTextBox.Text.Trim();

    public SaveProfileForm()
    {
        Text = "Save Current Profile";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(400, 220);

        var nameLabel = new Label
        {
            Text = "Profile Name:",
            Location = new Point(12, 15),
            AutoSize = true,
        };

        _nameTextBox = new TextBox
        {
            Location = new Point(12, 35),
            Size = new Size(370, 23),
        };

        var descLabel = new Label
        {
            Text = "Description (optional):",
            Location = new Point(12, 70),
            AutoSize = true,
        };

        _descriptionTextBox = new TextBox
        {
            Location = new Point(12, 90),
            Size = new Size(370, 70),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };

        _okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(220, 175),
            Size = new Size(75, 30),
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(305, 175),
            Size = new Size(75, 30),
        };

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _okButton.Click += OkButton_Click;

        Controls.AddRange(new Control[]
        {
            nameLabel, _nameTextBox,
            descLabel, _descriptionTextBox,
            _okButton, _cancelButton,
        });
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        string name = _nameTextBox.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(
                "Please enter a profile name.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        // Check for invalid filename characters
        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show(
                "Profile name contains invalid characters.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }
    }
}
