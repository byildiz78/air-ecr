using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Ecr.Module.Statics
{
    public static class ApplicationHelper
    {
        public static void RestartApplication()
        {
            try
            {
                // NotifyIcon'u temizle
                CleanupNotifyIcons();

                // Yeni uygulama instance'ını başlat
                var startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // Mevcut uygulamayı kapat
                Application.Exit();

                // Process'i tamamen sonlandır
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama yeniden başlatılırken hata oluştu: {ex.Message}",
                              "Hata",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private static void CleanupNotifyIcons()
        {
            // Tüm form'lardaki NotifyIcon'ları bul ve temizle
            foreach (Form form in Application.OpenForms)
            {
                var notifyIcons = form.Controls.OfType<NotifyIcon>();
                foreach (var icon in notifyIcons)
                {
                    icon.Visible = false;
                    icon.Dispose();
                }

                // Form üzerinde field olarak tanımlı NotifyIcon'ları da temizle
                var fields = form.GetType().GetFields(
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(NotifyIcon))
                    {
                        var notifyIcon = field.GetValue(form) as NotifyIcon;
                        if (notifyIcon != null)
                        {
                            notifyIcon.Visible = false;
                            notifyIcon.Icon?.Dispose();
                            notifyIcon.Dispose();
                        }
                    }
                }
            }
        }
    }
}
