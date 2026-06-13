using FiveTogether.UI;

namespace FiveTogether;

/// <summary>
/// Application entry point.
/// Configures high DPI, visual styles, and launches the main form.
/// </summary>
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Ensure proper cleanup on unhandled exceptions
        Application.ThreadException += (_, e) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will attempt to restore all controllers.",
                "FiveTogether — Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"A critical error occurred:\n\n{ex.Message}\n\nPlease restart the application. Controllers should be automatically restored.",
                    "FiveTogether — Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        Application.Run(new MainForm());
    }
}
