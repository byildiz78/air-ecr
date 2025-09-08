using Serilog;
using Serilog.Sinks.File.Archive;
using System;
using System.IO;
using System.IO.Compression;

namespace Ecr.Module.Statics
{
    public static class AppStatics
    {
        public static ILogger GetLogger(string directory)
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directory);

            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Application", "Ecr.Module")
                .WriteTo.Console()
                .WriteTo.File($"{AppDomain.CurrentDomain.BaseDirectory}\\{directory}\\log_.txt",
                              rollingInterval: RollingInterval.Day,
                              rollOnFileSizeLimit: true,
                              fileSizeLimitBytes: 100_000_000,
                              retainedFileCountLimit: 1,
                              hooks: new ArchiveHooks(CompressionLevel.Fastest))
                .MinimumLevel.Debug()
                .CreateLogger();

            return logger;
        }
    }
}
