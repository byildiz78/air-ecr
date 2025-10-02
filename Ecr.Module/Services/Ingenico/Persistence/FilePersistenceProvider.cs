using Newtonsoft.Json;
using System;
using System.IO;

namespace Ecr.Module.Services.Ingenico.Persistence
{
    /// <summary>
    /// File-based persistence provider
    /// JSON format kullanır
    /// </summary>
    public class FilePersistenceProvider : IPersistenceProvider
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">File name (default: gmp_state.json)</param>
        public FilePersistenceProvider(string fileName = "gmp_state.json")
        {
            // Application directory'de kaydet
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            _filePath = Path.Combine(appDir, "State", fileName);

            // State klasörü yoksa oluştur
            string stateDir = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(stateDir))
            {
                Directory.CreateDirectory(stateDir);
            }
        }

        public bool Save(PersistedState state)
        {
            lock (_lock)
            {
                try
                {
                    state.SavedAt = DateTime.Now;

                    // JSON serialize et
                    string json = JsonConvert.SerializeObject(state, Formatting.Indented);

                    // Backup önceki file (güvenlik için)
                    if (File.Exists(_filePath))
                    {
                        string backupPath = _filePath + ".bak";
                        File.Copy(_filePath, backupPath, true);
                    }

                    // Yeni file'ı kaydet
                    File.WriteAllText(_filePath, json);

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Persistence save error: {ex.Message}");
                    return false;
                }
            }
        }

        public PersistedState Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        return null;
                    }

                    string json = File.ReadAllText(_filePath);
                    var state = JsonConvert.DeserializeObject<PersistedState>(json);

                    // Version check (future compatibility)
                    if (state.Version != 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"Unsupported state version: {state.Version}");
                        return null;
                    }

                    return state;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Persistence load error: {ex.Message}");

                    // Backup'tan restore dene
                    try
                    {
                        string backupPath = _filePath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            string json = File.ReadAllText(backupPath);
                            return JsonConvert.DeserializeObject<PersistedState>(json);
                        }
                    }
                    catch
                    {
                        // Backup'tan da restore edilemedi
                    }

                    return null;
                }
            }
        }

        public bool Exists()
        {
            lock (_lock)
            {
                return File.Exists(_filePath);
            }
        }

        public bool Clear()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_filePath))
                    {
                        File.Delete(_filePath);
                    }

                    // Backup'ı da sil
                    string backupPath = _filePath + ".bak";
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Persistence clear error: {ex.Message}");
                    return false;
                }
            }
        }

        public DateTime? GetLastSaveTime()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_filePath))
                    {
                        return null;
                    }

                    return File.GetLastWriteTime(_filePath);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}