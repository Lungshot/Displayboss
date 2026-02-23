namespace DisplayBoss.Tray;

static class Program
{
    private const string MutexName = @"Global\DisplayBoss_SingleInstance";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            return;
        }

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext());
    }
}
