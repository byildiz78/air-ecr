using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecr.Module.Services.Ingenico.Configuration
{
    /// <summary>
    /// Configuration file validator
    /// Validates GMP.XML and GMP.ini files
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Validate GMP.XML file
        /// </summary>
        public static ConfigurationValidationResult ValidateXmlFile(string filePath)
        {
            var result = new ConfigurationValidationResult
            {
                FilePath = filePath,
                FileType = ConfigurationFileType.XML
            };

            try
            {
                // File exists check
                if (!File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.AddError("File does not exist");
                    return result;
                }

                // XML parse check
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                // Root element check
                if (xmlDoc.DocumentElement == null || xmlDoc.DocumentElement.Name != "Settings")
                {
                    result.AddError("Invalid root element. Expected 'Settings'");
                    return result;
                }

                // Required elements check
                ValidateXmlRequiredElements(xmlDoc, result);

                // Value validation
                ValidateXmlValues(xmlDoc, result);

                result.IsValid = result.Errors.Count == 0;
            }
            catch (XmlException ex)
            {
                result.IsValid = false;
                result.AddError($"XML parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.AddError($"Validation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate required XML elements
        /// </summary>
        private static void ValidateXmlRequiredElements(XmlDocument xmlDoc, ConfigurationValidationResult result)
        {
            var requiredElements = new[]
            {
                "InterfaceNo",
                "UseInterface",
                "Timeout",
                "PairingPassword",
                "PairingSerialNumber"
            };

            foreach (var elementName in requiredElements)
            {
                var node = xmlDoc.SelectSingleNode($"//Settings/{elementName}");
                if (node == null)
                {
                    result.AddWarning($"Missing element: {elementName}");
                }
            }
        }

        /// <summary>
        /// Validate XML values
        /// </summary>
        private static void ValidateXmlValues(XmlDocument xmlDoc, ConfigurationValidationResult result)
        {
            // InterfaceNo validation
            var interfaceNode = xmlDoc.SelectSingleNode("//Settings/InterfaceNo");
            if (interfaceNode != null)
            {
                if (!uint.TryParse(interfaceNode.InnerText, out uint interfaceNo))
                {
                    result.AddError("InterfaceNo must be a valid unsigned integer");
                }
                else if (interfaceNo == 0)
                {
                    result.AddWarning("InterfaceNo is 0 - will be auto-selected");
                }
            }

            // UseInterface validation
            var useInterfaceNode = xmlDoc.SelectSingleNode("//Settings/UseInterface");
            if (useInterfaceNode != null)
            {
                string value = useInterfaceNode.InnerText.ToLower();
                if (value != "rs232" && value != "usb" && value != "ethernet")
                {
                    result.AddError($"Invalid UseInterface value: {value}. Must be RS232, USB, or ETHERNET");
                }
            }

            // Timeout validation
            var timeoutNode = xmlDoc.SelectSingleNode("//Settings/Timeout");
            if (timeoutNode != null)
            {
                if (!int.TryParse(timeoutNode.InnerText, out int timeout))
                {
                    result.AddError("Timeout must be a valid integer");
                }
                else if (timeout < 1000 || timeout > 60000)
                {
                    result.AddWarning($"Timeout {timeout}ms is outside recommended range (1000-60000ms)");
                }
            }

            // PairingPassword validation
            var passwordNode = xmlDoc.SelectSingleNode("//Settings/PairingPassword");
            if (passwordNode != null)
            {
                string password = passwordNode.InnerText;
                if (string.IsNullOrWhiteSpace(password))
                {
                    result.AddWarning("PairingPassword is empty");
                }
                else if (password.Length < 4)
                {
                    result.AddWarning("PairingPassword is too short (recommended minimum: 4 characters)");
                }
            }

            // PairingSerialNumber validation
            var serialNode = xmlDoc.SelectSingleNode("//Settings/PairingSerialNumber");
            if (serialNode != null)
            {
                string serial = serialNode.InnerText;
                if (string.IsNullOrWhiteSpace(serial))
                {
                    result.AddWarning("PairingSerialNumber is empty");
                }
            }
        }

        /// <summary>
        /// Validate GMP.ini file
        /// </summary>
        public static ConfigurationValidationResult ValidateIniFile(string filePath)
        {
            var result = new ConfigurationValidationResult
            {
                FilePath = filePath,
                FileType = ConfigurationFileType.INI
            };

            try
            {
                // File exists check
                if (!File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.AddError("File does not exist");
                    return result;
                }

                // Read all lines
                var lines = File.ReadAllLines(filePath);

                // Basic format validation
                bool hasValidContent = false;
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                        continue;

                    // Check for key=value format
                    if (trimmed.Contains("="))
                    {
                        hasValidContent = true;
                        break;
                    }
                }

                if (!hasValidContent)
                {
                    result.AddWarning("No valid configuration entries found");
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.AddError($"INI validation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate configuration directory
        /// </summary>
        public static DirectoryValidationResult ValidateConfigurationDirectory(string directoryPath)
        {
            var result = new DirectoryValidationResult
            {
                DirectoryPath = directoryPath
            };

            try
            {
                // Directory exists check
                if (!Directory.Exists(directoryPath))
                {
                    result.IsValid = false;
                    result.AddError("Configuration directory does not exist");
                    return result;
                }

                // Check for GMP.XML
                string xmlPath = Path.Combine(directoryPath, "GMP.XML");
                if (File.Exists(xmlPath))
                {
                    result.HasXmlFile = true;
                    result.XmlValidation = ValidateXmlFile(xmlPath);
                }
                else
                {
                    result.AddWarning("GMP.XML file not found");
                }

                // Check for GMP.ini
                string iniPath = Path.Combine(directoryPath, "GMP.ini");
                if (File.Exists(iniPath))
                {
                    result.HasIniFile = true;
                    result.IniValidation = ValidateIniFile(iniPath);
                }
                else
                {
                    result.AddWarning("GMP.ini file not found");
                }

                // At least one file should exist
                if (!result.HasXmlFile && !result.HasIniFile)
                {
                    result.IsValid = false;
                    result.AddError("No configuration files found (GMP.XML or GMP.ini)");
                }
                else
                {
                    // Valid if no errors
                    result.IsValid = result.Errors.Count == 0;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.AddError($"Directory validation error: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Configuration file types
    /// </summary>
    public enum ConfigurationFileType
    {
        XML,
        INI
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public string FilePath { get; set; }
        public ConfigurationFileType FileType { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public ConfigurationValidationResult()
        {
            IsValid = true;
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// Directory validation result
    /// </summary>
    public class DirectoryValidationResult
    {
        public bool IsValid { get; set; }
        public string DirectoryPath { get; set; }
        public bool HasXmlFile { get; set; }
        public bool HasIniFile { get; set; }
        public ConfigurationValidationResult XmlValidation { get; set; }
        public ConfigurationValidationResult IniValidation { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public DirectoryValidationResult()
        {
            IsValid = true;
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}