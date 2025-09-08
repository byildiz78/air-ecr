using Serilog;
using Serilog.Sinks.File.Archive;
using System;
using System.IO;
using System.IO.Compression;

namespace Ecr.Module.Statics
{
    public static class AppStatics
    {
        public static ILogger GetLogger()
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EcrLog");

            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File($"{AppDomain.CurrentDomain.BaseDirectory}\\EcrLog\\log_.txt",
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
