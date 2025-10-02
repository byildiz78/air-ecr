using System;
using System.IO;

namespace Ecr.Module.Services.Ingenico.Configuration
{
    /// <summary>
    /// Configuration manager
    /// Manages configuration loading, validation, and change detection
    /// </summary>
    public class ConfigurationManager
    {
        private static readonly object _lock = new object();
        private static ConfigurationManager _instance;

        private string _configDirectory;
        private DateTime _lastXmlModifiedTime;
        private DateTime _lastIniModifiedTime;
        private bool _isInitialized;

        /// <summary>
        /// Configuration directory path
        /// </summary>
        public string ConfigurationDirectory
        {
            get => _configDirectory;
            set
            {
                _configDirectory = value;
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Event fired when configuration changes detected
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs> OnConfigurationChanged;

        private ConfigurationManager()
        {
            // Default configuration directory
            _configDirectory = AppDomain.CurrentDomain.BaseDirectory;
        }

        public static ConfigurationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ConfigurationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Initialize configuration manager
        /// </summary>
        public ConfigurationInitResult Initialize(string configDirectory = null)
        {
            var result = new ConfigurationInitResult();

            try
            {
                // Set directory
                if (!string.IsNullOrEmpty(configDirectory))
                {
                    _configDirectory = configDirectory;
                }

                // Validate directory
                var directoryValidation = ConfigurationValidator.ValidateConfigurationDirectory(_configDirectory);
                result.ValidationResult = directoryValidation;

                if (!directoryValidation.IsValid)
                {
                    result.Success = false;
                    result.Message = "Configuration directory validation failed";
                    return result;
                }

                // Record file modified times
                UpdateFileModifiedTimes();

                _isInitialized = true;
                result.Success = true;
                result.Message = "Configuration initialized successfully";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Initialization error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Check for configuration changes
        /// </summary>
        public bool HasConfigurationChanged()
        {
            if (!_isInitialized)
                return false;

            try
            {
                // Check GMP.XML
                string xmlPath = Path.Combine(_configDirectory, "GMP.XML");
                if (File.Exists(xmlPath))
                {
                    DateTime currentXmlTime = File.GetLastWriteTime(xmlPath);
                    if (currentXmlTime > _lastXmlModifiedTime)
                    {
                        return true;
                    }
                }

                // Check GMP.ini
                string iniPath = Path.Combine(_configDirectory, "GMP.ini");
                if (File.Exists(iniPath))
                {
                    DateTime currentIniTime = File.GetLastWriteTime(iniPath);
                    if (currentIniTime > _lastIniModifiedTime)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Reload configuration
        /// </summary>
        public ConfigurationReloadResult ReloadConfiguration()
        {
            var result = new ConfigurationReloadResult();

            try
            {
                // Validate first
                var validation = ConfigurationValidator.ValidateConfigurationDirectory(_configDirectory);
                result.ValidationResult = validation;

                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Message = "Configuration validation failed";
                    return result;
                }

                // Determine what changed
                result.XmlChanged = HasXmlFileChanged();
                result.IniChanged = HasIniFileChanged();

                // Update modified times
                UpdateFileModifiedTimes();

                // Fire change event
                if (result.XmlChanged || result.IniChanged)
                {
                    OnConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                    {
                        XmlChanged = result.XmlChanged,
                        IniChanged = result.IniChanged,
                        Timestamp = DateTime.Now
                    });
                }

                result.Success = true;
                result.Message = "Configuration reloaded successfully";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Reload error: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Check if XML file changed
        /// </summary>
        private bool HasXmlFileChanged()
        {
            string xmlPath = Path.Combine(_configDirectory, "GMP.XML");
            if (File.Exists(xmlPath))
            {
                DateTime currentTime = File.GetLastWriteTime(xmlPath);
                return currentTime > _lastXmlModifiedTime;
            }
            return false;
        }

        /// <summary>
        /// Check if INI file changed
        /// </summary>
        private bool HasIniFileChanged()
        {
            string iniPath = Path.Combine(_configDirectory, "GMP.ini");
            if (File.Exists(iniPath))
            {
                DateTime currentTime = File.GetLastWriteTime(iniPath);
                return currentTime > _lastIniModifiedTime;
            }
            return false;
        }

        /// <summary>
        /// Update file modified times
        /// </summary>
        private void UpdateFileModifiedTimes()
        {
            string xmlPath = Path.Combine(_configDirectory, "GMP.XML");
            if (File.Exists(xmlPath))
            {
                _lastXmlModifiedTime = File.GetLastWriteTime(xmlPath);
            }

            string iniPath = Path.Combine(_configDirectory, "GMP.ini");
            if (File.Exists(iniPath))
            {
                _lastIniModifiedTime = File.GetLastWriteTime(iniPath);
            }
        }

        /// <summary>
        /// Validate current configuration
        /// </summary>
        public DirectoryValidationResult ValidateCurrentConfiguration()
        {
            return ConfigurationValidator.ValidateConfigurationDirectory(_configDirectory);
        }

        /// <summary>
        /// Get configuration file path
        /// </summary>
        public string GetXmlFilePath()
        {
            return Path.Combine(_configDirectory, "GMP.XML");
        }

        /// <summary>
        /// Get INI file path
        /// </summary>
        public string GetIniFilePath()
        {
            return Path.Combine(_configDirectory, "GMP.ini");
        }
    }

    /// <summary>
    /// Configuration initialization result
    /// </summary>
    public class ConfigurationInitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DirectoryValidationResult ValidationResult { get; set; }

        public ConfigurationInitResult()
        {
            Success = false;
            Message = string.Empty;
        }
    }

    /// <summary>
    /// Configuration reload result
    /// </summary>
    public class ConfigurationReloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool XmlChanged { get; set; }
        public bool IniChanged { get; set; }
        public DirectoryValidationResult ValidationResult { get; set; }

        public ConfigurationReloadResult()
        {
            Success = false;
            Message = string.Empty;
        }
    }

    /// <summary>
    /// Configuration changed event arguments
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public bool XmlChanged { get; set; }
        public bool IniChanged { get; set; }
        public DateTime Timestamp { get; set; }
    }
}