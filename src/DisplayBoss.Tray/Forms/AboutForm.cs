namespace DisplayBoss.Tray.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About DisplayBoss";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(340, 200);

        var titleLabel = new Label
        {
            Text = "DisplayBoss",
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Location = new Point(12, 15),
            AutoSize = true,
        };

        var versionLabel = new Label
        {
            Text = "Version 1.0.0",
            Location = new Point(14, 50),
            AutoSize = true,
        };

        var descLabel = new Label
        {
            Text = "Display Profile Switcher for Windows",
            Location = new Point(14, 75),
            AutoSize = true,
        };

        var linkLabel = new LinkLabel
        {
            Text = "https://github.com/Lungshot/Displayboss",
            Location = new Point(12, 105),
            AutoSize = true,
        };
        linkLabel.LinkClicked += (_, _) =>
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Lungshot/Displayboss",
                UseShellExecute = true,
            });
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(130, 150),
            Size = new Size(80, 30),
        };

        AcceptButton = okButton;
        CancelButton = okButton;

        Controls.AddRange(new Control[]
        {
            titleLabel, versionLabel, descLabel, linkLabel, okButton,
        });
    }
}
