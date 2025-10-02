using Ecr.Module.Services.Ingenico.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ecr.Module.Services.Ingenico.FiscalLogManager
{
    public enum LogFolderType
    {
        Waiting = 1,
        Completed = 2,
        Cancel = 3,
        Exception = 4,
        Return = 5
    }

    public class LogManagerOrder
    {
        private static string _logFolder = $"{System.Windows.Forms.Application.StartupPath}\\CommandBackup\\Waiting";
        private static string _logFolderCompleted = $"{System.Windows.Forms.Application.StartupPath}\\CommandBackup\\Completed";
        private static string _logFolderCancel = $"{System.Windows.Forms.Application.StartupPath}\\CommandBackup\\Cancel";
        private static string _logFolderException = $"{System.Windows.Forms.Application.StartupPath}\\CommandBackup\\Exception";
        private static string _logFolderReturn = $"{System.Windows.Forms.Application.StartupPath}\\CommandBackup\\Return";

        public static void Save(string data, string sourceId, string prefix = null, string afterfix = null, string subFolderName = null, string fileName = null, bool prependLogDate = true)
        {
            if (!data.EndsWith(Environment.NewLine))
                data += Environment.NewLine;

            var sourceDescription = "";

            var part = 0;
            var exceptionCount = 0;


            sourceDescription = $"{sourceId}";

            retry:

            if (part > 0)
                afterfix = $"_part-{part}";

            var folder = $"{_logFolder}";


            fileName = $"{sourceDescription}";


            fileName = fileName.Replace(@"/", "_").Replace(@"\", "_").Replace(@"*", "_");

            var fileDir = $"{folder}\\{fileName}.txt";
            var logDatePrefix = prependLogDate ? $"{DateTime.Now} : " : "";
            try
            {
                File.AppendAllText(fileDir, $"{logDatePrefix}{data}");
            }
            catch (DirectoryNotFoundException ex)
            {
                Directory.CreateDirectory(folder);

                goto retry;
            }
            catch (IOException ex)
            {
                if (part < 3)
                {
                    part++;
                    goto retry;
                }
            }
            catch (Exception ex)
            {
                if (exceptionCount < 3)
                {
                    prefix = "_EXCEPTION";

                    var errorDetail = "";
                    errorDetail += "Message: " + ex.Message + "\r\n\r\n";
                    errorDetail += "StackTrace: " + ex.StackTrace + "\r\n\r\n";

                    if (ex.InnerException != null)
                    {
                        errorDetail += "InnerException 1: " + ex.InnerException.Message + "\r\n\r\n";
                        if (ex.InnerException.InnerException != null)
                            errorDetail += "InnerException 2: " + ex.InnerException.InnerException.Message + "\r\n\r\n";
                    }

                    data += Environment.NewLine + "".PadLeft(20, '-') + Environment.NewLine + errorDetail;

                    exceptionCount++;

                    goto retry;
                }
            }
        }

        public static void SaveOrder(string orderData, string fileName, string sourceId)
        {
            if (DataStore._logRarLastEngineDate < DateTime.Now.Date)
            {
                ArchiveOldLogs();
                DataStore._logRarLastEngineDate = DateTime.Now.Date;
            }

            var subFolderName = $"{sourceId}";

            Save(orderData, sourceId, subFolderName: subFolderName, fileName: fileName, prependLogDate: false);
        }
        public static void ArchiveOldLogs()
        {
            var folders = new[]
            {
                _logFolder,
                _logFolderCompleted,
                _logFolderCancel,
                _logFolderException,
                _logFolderReturn
            };

            var today = DateTime.Today;
            var archiveDate = today.ToString("yyyy-MM-dd");

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                    continue;

                var files = Directory.GetFiles(folder, "*.txt");
                var oldFiles = new List<string>();

                if (files.Any())
                {
                    oldFiles = files.Where(x => File.GetLastWriteTime(x).Date < today).ToList();

                }
                if (oldFiles.Count == 0)
                    continue;

                
                
            }
        }

        public static void Exception(Exception ex, string commandName, string sourceId)
        {
            var result = "";
            result += "Message: " + ex.Message + "\r\n\r\n";
            result += "StackTrace: " + ex.StackTrace + "\r\n\r\n";

            if (ex.InnerException != null)
            {
                result += "InnerException 1: " + ex.InnerException.Message + "\r\n\r\n";
                if (ex.InnerException.InnerException != null)
                    result += "InnerException 2: " + ex.InnerException.InnerException.Message + "\r\n\r\n";
            }

            Save($"COMMAND NAME : {commandName} , EXCEPTION : {result}", sourceId, afterfix: "_EXCEPTION");
        }

        public static List<GmpCommand> GetOrderFile(string sourceId)
        {
            var commands = new List<GmpCommand>();

            try
            {

                if (File.Exists($"{_logFolder}\\{sourceId}.txt"))
                {
                    var lines = File.ReadAllLines($"{_logFolder}\\{sourceId}.txt");
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var jsonLine = line.Trim();

                            var command = JsonConvert.DeserializeObject<GmpCommand>(jsonLine);
                            if (command != null)
                            {
                                commands.Add(command);
                            }
                        }
                        catch (Exception ex)
                        {
                            return commands;
                        }

                    }
                }





            }
            catch (DirectoryNotFoundException ex)
            {
                return commands;
            }

            return commands;
        }

        public static List<GmpCommand> GetOrderFileComplated(string sourceId)
        {
            var commands = new List<GmpCommand>();

            try
            {

                if (File.Exists($"{_logFolderCompleted}\\{sourceId}.txt"))
                {
                    var lines = File.ReadAllLines($"{_logFolderCompleted}\\{sourceId}.txt");
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var jsonLine = line.Trim();

                            var command = JsonConvert.DeserializeObject<GmpCommand>(jsonLine);
                            if (command != null)
                            {
                                commands.Add(command);
                            }
                        }
                        catch (Exception ex)
                        {
                            return commands;
                        }

                    }
                }





            }
            catch (DirectoryNotFoundException ex)
            {
                return commands;
            }

            return commands;
        }

        public static List<string> GetLogFileNames()
        {
            try
            {
                if (Directory.Exists(_logFolder))
                {
                    return Directory.GetFiles(_logFolder)
                                   .Select(Path.GetFileName)
                                   .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }

            return new List<string>();
        }

        public static List<GmpPrintReceiptDto> GetOrderFileData(string sourceId)
        {
            var commands = new List<GmpPrintReceiptDto>();

            try
            {

                if (File.Exists($"{_logFolder}\\{sourceId}.txt"))
                {
                    var lines = File.ReadAllLines($"{_logFolder}\\{sourceId}.txt");
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var jsonLine = line.Trim();

                            var command = JsonConvert.DeserializeObject<GmpPrintReceiptDto>(jsonLine);
                            if (command != null)
                            {
                                commands.Add(command);
                            }
                        }
                        catch (Exception ex)
                        {
                            return commands;
                        }

                    }
                }





            }
            catch (DirectoryNotFoundException ex)
            {
                return commands;
            }

            return commands;
        }

        public static FiscalOrder GetOrderFileFiscal(string sourceId)
        {
            var commands = new FiscalOrder();

            try
            {

                if (File.Exists($"{_logFolder}\\{sourceId}.txt"))
                {
                    var lines = File.ReadAllLines($"{_logFolder}\\{sourceId}.txt");
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            var jsonLine = line.Trim();

                            commands = JsonConvert.DeserializeObject<FiscalOrder>(jsonLine);

                        }
                        catch (Exception ex)
                        {
                            return commands;
                        }

                    }
                }





            }
            catch (DirectoryNotFoundException ex)
            {
                return commands;
            }

            return commands;
        }

        public static bool RenameLog(string oldSourceId, string newSourceId)
        {
            try
            {
                // Dosya adlarındaki geçersiz karakterleri temizle
                string safeOldSourceId = CleanFileName(oldSourceId);
                string safeNewSourceId = CleanFileName(newSourceId);

                // Path sınıfını kullanarak güvenli dosya yolları oluştur
                string oldFilePath = Path.Combine(_logFolder, safeOldSourceId + ".txt");
                string newFilePath = Path.Combine(_logFolder, safeNewSourceId + ".txt");

                if (File.Exists(oldFilePath))
                {
                    // Eğer hedef dosya zaten varsa, önce onu siliyoruz
                    if (File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }

                    // Dosyayı yeni adıyla yeniden adlandırıyoruz
                    File.Copy(oldFilePath, newFilePath);
                    File.Delete(oldFilePath);
                    return true;
                }
                return false; // Kaynak dosya bulunamadı
            }
            catch (Exception ex)
            {
                // Hata durumunda exception loglanabilir
                Exception(ex, "RenameLog", oldSourceId);
                return false;
            }
        }

        private static string CleanFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unknown";

            // Dosya adında kullanılamayacak karakterleri temizle
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]+", invalidChars);

            // Geçersiz karakterleri alt çizgi ile değiştir
            string safeName = Regex.Replace(fileName, invalidRegStr, "_");

            // Dosya adı uzunluğunu sınırla (isteğe bağlı)
            if (safeName.Length > 100)
                safeName = safeName.Substring(0, 100);

            return safeName;
        }
        public static List<string> ListWaitingLogs(string excludeFileName)
        {
            List<string> logFiles = new List<string>();

            try
            {
                // Waiting klasörü var mı kontrol et
                if (!Directory.Exists(_logFolder))
                    return logFiles; // Boş liste döndür

                // Klasördeki tüm .txt dosyalarını al
                string[] files = Directory.GetFiles(_logFolder, "*.txt");

                // Dosya adlarını listeye ekle (uzantısız)
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // Filtreleme yap
                    if (string.IsNullOrEmpty(excludeFileName) || !fileName.Equals(excludeFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        logFiles.Add(fileName);
                    }
                }

                return logFiles;
            }
            catch (Exception ex)
            {
                // Hata durumunda exception logla
                Exception(ex, "ListWaitingLogs", "system");
                return logFiles; // Boş veya kısmi doldurulmuş liste döndür
            }
        }

        /// <summary>
        /// Waiting klasöründeki 2 dosyayı (Commands ve Fiscal) hedef klasöre taşır
        /// Eğer hedef dosya varsa, mevcut dosyayı _old yaparak yedekler
        /// </summary>
        public static bool MoveOrderFilesToCompleted(string sourceId)
        {
            try
            {
                string safeSourceId = CleanFileName(sourceId);
                string targetFolder = _logFolderCompleted;

                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                bool success = true;

                // 1. {OrderKey}.txt (Commands) dosyasını taşı
                string commandsSource = Path.Combine(_logFolder, safeSourceId + ".txt");
                if (File.Exists(commandsSource))
                {
                    string commandsTarget = Path.Combine(targetFolder, safeSourceId + ".txt");

                    // Eğer hedef dosya varsa _old yap
                    if (File.Exists(commandsTarget))
                    {
                        string oldFile = Path.Combine(targetFolder, safeSourceId + "_old.txt");
                        if (File.Exists(oldFile))
                            File.Delete(oldFile);
                        File.Move(commandsTarget, oldFile);
                    }

                    File.Move(commandsSource, commandsTarget);
                }
                else
                {
                    success = false;
                }

                // 2. {OrderKey}_Fiscal.txt dosyasını taşı
                string fiscalSource = Path.Combine(_logFolder, safeSourceId + "_Fiscal.txt");
                if (File.Exists(fiscalSource))
                {
                    string fiscalTarget = Path.Combine(targetFolder, safeSourceId + "_Fiscal.txt");

                    // Eğer hedef dosya varsa _old yap
                    if (File.Exists(fiscalTarget))
                    {
                        string oldFile = Path.Combine(targetFolder, safeSourceId + "_Fiscal_old.txt");
                        if (File.Exists(oldFile))
                            File.Delete(oldFile);
                        File.Move(fiscalTarget, oldFile);
                    }

                    File.Move(fiscalSource, fiscalTarget);
                }

                // 3. _Data.txt dosyası varsa sil (taşınmaz)
                string dataSource = Path.Combine(_logFolder, safeSourceId + "_Data.txt");
                if (File.Exists(dataSource))
                {
                    File.Delete(dataSource);
                }

                return success;
            }
            catch (Exception ex)
            {
                Exception(ex, "MoveOrderFilesToCompleted", sourceId);
                return false;
            }
        }

        public static bool MoveLogFile(string sourceId, LogFolderType folderType)
        {
            try
            {
                string safeSourceId = CleanFileName(sourceId);
                string sourceFilePath = Path.Combine(_logFolder, safeSourceId + ".txt");

                string targetFolder;
                switch (folderType)
                {
                    case LogFolderType.Waiting:
                        targetFolder = _logFolder;
                        break;
                    case LogFolderType.Completed:
                        targetFolder = _logFolderCompleted;
                        break;
                    case LogFolderType.Cancel:
                        targetFolder = _logFolderCancel;
                        break;
                    case LogFolderType.Exception:
                        targetFolder = _logFolderException;
                        break;
                    case LogFolderType.Return:
                        targetFolder = _logFolderReturn;
                        break;
                    default:
                        targetFolder = _logFolder;
                        break;
                }

                if (!File.Exists(sourceFilePath))
                    return false;

                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);

                // İlk hedef dosya yolu
                string targetFilePath = Path.Combine(targetFolder, safeSourceId + ".txt");

                // Eğer hedef dosya varsa sonuna _yeni ekle
                int counter = 1;
                while (File.Exists(targetFilePath))
                {
                    string newFileName = $"{safeSourceId}_yeni{(counter > 1 ? counter.ToString() : "")}.txt";
                    targetFilePath = Path.Combine(targetFolder, newFileName);
                    counter++;
                }

                File.Move(sourceFilePath, targetFilePath);

                return true;
            }
            catch (Exception ex)
            {
                Exception(ex, "MoveLogFile", sourceId);
                return false;
            }
        }
    }
}
