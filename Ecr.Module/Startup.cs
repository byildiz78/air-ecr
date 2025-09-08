using Ecr.Module.Middlewares;
using Owin;
using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;

namespace Ecr.Module
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {

            string processName = Process.GetCurrentProcess().ProcessName;
            var processes = Process.GetProcessesByName(processName);
            var lastProcess = processes.OrderByDescending(p => p.StartTime).FirstOrDefault();

            if (lastProcess != null && lastProcess.Id != Process.GetCurrentProcess().Id)
            {
                Console.WriteLine("Uygulama zaten çalışıyor. Yeni açılan kapanıyor.");
                Environment.Exit(0);
            }

            var config = new HttpConfiguration();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.Use<LoggingMiddleware>();
            appBuilder.UseWebApi(config);
        }
    }
}