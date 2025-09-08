using Serilog;
using Serilog.Sinks.File.Archive;
using System;
using System.IO.Compression;
using System.Windows.Forms;

namespace Ecr.Host
{
    internal static class Program
    {
        public static ILogger Logger { get; private set; }

        [STAThread]
        static void Main()
        {
            Logger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .WriteTo.File("EcrLog\\log_.txt",
                                          rollingInterval: RollingInterval.Day,
                                          rollOnFileSizeLimit: true,
                                          fileSizeLimitBytes: 100_000_000,
                                          retainedFileCountLimit: 1,
                                          hooks: new ArchiveHooks(CompressionLevel.Fastest))
                            .MinimumLevel.Debug()
                            .CreateLogger();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}