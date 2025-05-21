using PasswordManager;
using System;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using (var loginForm = new LoginForm())
        {
            try
            {
                Application.Run(loginForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Необработанная ошибка: {(e.ExceptionObject as Exception)?.Message}",
                      "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(1);
    }
}