using Microsoft.Extensions.DependencyInjection;

namespace LPGDataAnalyzer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<AppSettingManager>();
            services.AddTransient<MainForm>();

            using var serviceProvider = services.BuildServiceProvider();

            var mainForm = serviceProvider.GetRequiredService<MainForm>();

            Application.Run(mainForm);


        }
    }
}