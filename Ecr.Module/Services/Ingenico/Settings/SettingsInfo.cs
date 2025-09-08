using Ecr.Module.Services.Ingenico.Models;
using System;
using System.IO;
using System.Xml;

namespace Ecr.Module.Services.Ingenico.Settings
{
    public class SettingsInfo
    {
       

        private static string path = System.Windows.Forms.Application.StartupPath + "\\Modules\\ingenico";
        public static void getIniValues()
        {
            try
            {
                SettingsValues.serialnumber = iniValues.getValue("serialnumber");
                SettingsValues.brand = iniValues.getValue("brand");
                SettingsValues.model = iniValues.getValue("model");
                SettingsValues.cashiercode = iniValues.getValue("cashiercode");
                SettingsValues.cashierpassword = iniValues.getValue("cashierpassword");
                SettingsValues.adminpassword = iniValues.getValue("adminpassword");
                SettingsValues.messagetext = iniValues.getValue("messagetext");
                SettingsValues.currency = iniValues.getValue("currency");
                SettingsValues.printmode = iniValues.getValue("printmode");
                SettingsValues.bankpayment = iniValues.getValue("bankpayment");
                SettingsValues.salexml = iniValues.getValue("salexml");
                SettingsValues.barcode = iniValues.getValue("barcode");
                SettingsValues.fiscalbarcode = iniValues.getValue("fiscalbarcode");
                SettingsValues.fiscalreceiptno = iniValues.getValue("fiscalreceiptno");
                SettingsValues.uniqid = iniValues.getValue("uniqid");
                SettingsValues.log = iniValues.getValue("log");
                SettingsValues.alterfiscalsize = iniValues.getValue("alterfiscalsize");
                SettingsValues.installment = iniValues.getValue("installment");
                SettingsValues.cardtimeout = iniValues.getValue("cardtimeout");
                SettingsValues.messagetimeout = iniValues.getValue("messagetimeout");
                SettingsValues.automaticZReportTime = iniValues.getValue("automaticZReportTime");
                SettingsValues.receiptWeightNotDetails = iniValues.getValue("receiptWeightNotDetails");

                if (iniValues.getValue("AddZreport") == "-1")
                {
                    SettingsValues.AddZreport = true;
                }
                else
                {
                    SettingsValues.AddZreport = (iniValues.getValue("AddZreport") == "1" ? true : false);
                }


                if (iniValues.getValue("changedv11") == "-1")
                {
                    SettingsValues.changedv11 = true;
                }
                else
                {
                    SettingsValues.changedv11 = (iniValues.getValue("changedv11") == "1" ? true : false);
                }

                if (string.IsNullOrEmpty(SettingsValues.adminpassword))
                {
                    SettingsValues.cashierpassword = "0000";
                }
                if (string.IsNullOrEmpty(SettingsValues.cashierpassword))
                {
                    SettingsValues.cashierpassword = "0001";
                }
                if (string.IsNullOrEmpty(SettingsValues.cashiercode))
                {
                    SettingsValues.cashierpassword = "0";
                }
                if (iniValues.getValue("automaticZReport") == "-1")
                {
                    SettingsValues.automaticZReport = true;
                }
                else
                {
                    SettingsValues.automaticZReport = (iniValues.getValue("automaticZReport") == "1" ? true : false);
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "getIniValues");
            }
        }
        public static void setXmlValues()
        {
            try
            {
                var GmpLogPath = path + @"\GmpLogs";


                var Xmlpath = DataStore.gmpxml;

                XmlDocument doc = new XmlDocument();
                doc.Load(Xmlpath);

                if (doc != null)
                {
                    XmlElement formData = (XmlElement)doc.SelectSingleNode("//GMP//INTERFACE");

                    if (formData != null)
                    {
                        if (!System.IO.Directory.Exists(GmpLogPath))
                        {
                            System.IO.Directory.CreateDirectory(GmpLogPath);
                        }

                        //bağlantı ayarları
                        string connectionType = "FALSE";
                        if (SettingsValues.IsTcpConnection == "0")
                        {
                            connectionType = "FALSE";
                        }
                        else
                        {
                            connectionType = "TRUE";
                        }
                        formData.SelectSingleNode("IsTcpConnection").InnerText = connectionType;
                        formData.SelectSingleNode("IP").InnerText = SettingsValues.IP;

                        if (!string.IsNullOrEmpty(SettingsValues.Port))
                        {
                            formData.SelectSingleNode("Port").InnerText = SettingsValues.Port;
                        }
                        else
                        {
                            formData.SelectSingleNode("Port").InnerText = "7500";
                        }

                        formData.SelectSingleNode("PortName").InnerText = SettingsValues.PortName;



                        //gmp loglama ayarları
                        var logOpen = "FALSE";

                        if (SettingsValues.GmpLog)
                        {
                            logOpen = "TRUE";
                        }
                        else
                        {
                            logOpen = "FALSE";
                        }

                        XmlElement logPath = (XmlElement)doc.SelectSingleNode("//GMP//DLL");
                        logPath.SelectSingleNode("LogPath").InnerText = GmpLogPath;

                        XmlElement logMain = (XmlElement)doc.SelectSingleNode("//GMP//LOG");

                        logMain.SelectSingleNode("LogGeneralOpen").InnerText = logOpen;
                        logMain.SelectSingleNode("LogGmp3TagsOpen").InnerText = logOpen;
                        logMain.SelectSingleNode("LogJsonDataOpen").InnerText = logOpen;
                        logMain.SelectSingleNode("LogJsonOpen").InnerText = logOpen;
                        logMain.SelectSingleNode("LogFunctionOpen").InnerText = logOpen;
                        logMain.SelectSingleNode("LogPrintToFileOpen").InnerText = logOpen;
                        logMain.SelectSingleNode("LogThreadOpen").InnerText = logOpen;
                        XmlElement logInterface = (XmlElement)doc.SelectSingleNode("//GMP//INTERFACE//LOG");

                        if (logInterface != null)
                        {
                            logInterface.SelectSingleNode("LogGeneralOpen").InnerText = logOpen;
                            logInterface.SelectSingleNode("LogGmp3TagsOpen").InnerText = logOpen;
                            logInterface.SelectSingleNode("LogJsonDataOpen").InnerText = logOpen;
                            logInterface.SelectSingleNode("LogJsonOpen").InnerText = logOpen;
                            logInterface.SelectSingleNode("LogFunctionOpen").InnerText = logOpen;
                            logMain.SelectSingleNode("LogPrintToFileOpen").InnerText = logOpen;
                            logMain.SelectSingleNode("LogThreadOpen").InnerText = logOpen;

                        }


                        doc.Save(Xmlpath);
                    } 

                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "setXmlValues");
            }
        }
        public static void getGMPIniValues()
        {

             if (File.Exists(DataStore.gmpini))
            {
                string prtn = GmpiniValues.getValue("CONNECTION", "PortName");
                try { SettingsValues.PortName = prtn.Replace("\\", "").Replace(".", ""); } catch { SettingsValues.PortName = "COM1"; }
                try { SettingsValues.IsTcpConnection = GmpiniValues.getValue("CONNECTION", "IsTcpConnection"); } catch { SettingsValues.IsTcpConnection = "0"; }
                try { SettingsValues.IP = GmpiniValues.getValue("CONNECTION", "IP"); } catch { SettingsValues.IP = "192.168.0.0"; }
                try { SettingsValues.Port = GmpiniValues.getValue("CONNECTION", "Port"); } catch { SettingsValues.Port = "7500"; }

                try
                {
                    SettingsValues.GmpLog = false;
                    string logpath = GmpiniValues.getValue("LOG", "LogPath");
                    if (!string.IsNullOrEmpty(logpath))
                    {
                        string LogGeneralOpen = GmpiniValues.getValue("LOG", "LogGeneralOpen");
                        string LogPrintToFileOpen = GmpiniValues.getValue("LOG", "LogPrintToFileOpen");
                        string LogGmp3TagsOpen = GmpiniValues.getValue("LOG", "LogGmp3TagsOpen");
                        string LogJsonDataOpen = GmpiniValues.getValue("LOG", "LogJsonDataOpen");
                        string LogPrintVersionOpen = GmpiniValues.getValue("LOG", "LogPrintVersionOpen");
                        if (LogGeneralOpen.Contains("1") || LogPrintToFileOpen.Contains("1") || LogGmp3TagsOpen.Contains("1") || LogJsonDataOpen.Contains("1"))
                        {
                            SettingsValues.GmpLog = true;
                        }
                    }
                    else
                    {
                        SettingsValues.GmpLog = false;
                    }
                }
                catch (Exception ex)
                {
                    //LogManager.Exception(ex, "getGMPIniValues");
                }
            }
            else
            {
                //MessageManager.ShowMessage("UYARI", "Program yolunda GMP.INI dosyası bulunamadı!", false, 0);
            }
        }
    }
}
