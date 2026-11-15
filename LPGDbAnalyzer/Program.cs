using LPGDbAnalyzer.Data;
using LPGDbAnalyzer.Forms;

namespace LPGDbAnalyzer
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Database database;
            try
            {
                var settings = AppSettings.Load();
                database = new Database(settings.ResolvedDatabasePath);
                database.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "LPG DB Analyzer - startup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm(database));
        }
    }
}
