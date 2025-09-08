using Ecr.Module.Forms;
using Ecr.Module.Statics;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Ecr.Host
{
    internal static class Program
    {
        private static Mutex _mutex = null;

        [STAThread]
        static void Main()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Yakalanamayan hataları yakala
            GlobalExceptionHandler.Initialize();

            var currentPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

            // Klasör yolundan benzersiz bir Mutex adı oluşturuyoruz
            // Klasör yolundaki karakterleri Mutex adı için güvenli hale getiriyoruz
            var mutexName = "Global\\AirEcr_" + currentPath.GetHashCode().ToString("X");

            var createdNew = false;

            // Mutex'i oluşturuyoruz
            _mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
                return;

            try
            {
                Application.Run(new frmMain());
            }
            finally
            {
                // Uygulama kapanırken Mutex'i serbest bırakıyoruz
                if (_mutex != null && createdNew)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
        }
    }
}