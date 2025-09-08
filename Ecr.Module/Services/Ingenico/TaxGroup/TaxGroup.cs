using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico
{
    public class TaxGroup
    {
        public IngenicoApiResponse<TaxGroupsInfoDto> TaxGroupsPairing(List<TaxGroups> taxGroupDbList)
        {
            //LogManager.Append("TaxGroupsPairing başlatıldı");
            var _taxGroupsInfoDto = new IngenicoApiResponse<TaxGroupsInfoDto>();
            _taxGroupsInfoDto.Data = new TaxGroupsInfoDto();
            ST_TAX_RATE[] stTaxRates = new ST_TAX_RATE[8];
            var numberOfTotalTaxratesReceived = 0;
            var numberOfTotalTaxRates = 0;


            //Json TaxGroups tablosu listeye atılıyor...

            if (!taxGroupDbList.Any())
            {
                _taxGroupsInfoDto.Data.ReturnCode = 9999;
                _taxGroupsInfoDto.Data.ReturnCodeMessage = "TaxGroups Boş Geçilemez!";
                _taxGroupsInfoDto.Data.ReturnMessage = "TaxGroups Boş Geçilemez!";
                return _taxGroupsInfoDto;
            }

            try
            {
                #region Yazarkasadan Kdv bilgileri alınıyor...
                _taxGroupsInfoDto.Data.ReturnCode = Json_GMPSmartDLL.FP3_GetTaxRates(DataStore.CurrentInterface, ref numberOfTotalTaxRates,
                    ref numberOfTotalTaxratesReceived, ref stTaxRates, 8);
                if (_taxGroupsInfoDto.Data.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    //LogManager.Append("Kdv grupları yazarkasadan alınamadı." + ErrorClass.DisplayErrorMessage(_taxGroupsInfoDto.ReturnCode));
                    _taxGroupsInfoDto.Data.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(_taxGroupsInfoDto.Data.ReturnCode);
                    _taxGroupsInfoDto.Data.ReturnMessage = ErrorClass.DisplayErrorMessage(_taxGroupsInfoDto.Data.ReturnCode);
                    return _taxGroupsInfoDto;
                }

                #endregion

                #region İlk  sıradaki kdv değerini baz alıp işlem yapmasi için değişkenler tanımlandı

                bool set0 = false;
                bool set1 = false;
                bool set8 = false;
                bool set18 = false;
                bool set5 = false;
                bool set10 = false;
                bool set20 = false;
                bool set24 = false;
                bool set16 = false;
                #endregion

                //LogManager.Append($"infinia " + Newtonsoft.Json.JsonConvert.SerializeObject(taxGroupDbList));

                #region kdv ile taxgroup eşleştirilip yeni bir taxgroup list oluşturuluyorr.

                for (int i = 0; i < stTaxRates.Length; i++)
                {
                    //LogManager.Append($"ingenico " + stTaxRates[i].taxRate.ToString());
                    var taxRate = stTaxRates[i].taxRate / 100;

                    switch (taxRate)
                    {
                        case 0:
                            {
                                if (set0 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 0)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";
                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set0 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 1:
                            {
                                if (set1 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 1)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";
                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set1 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 8:
                            {
                                if (set8 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 8)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();

                                            t.IGroupName = $"{item.GroupName}";
                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set8 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 18:
                            {
                                if (set18 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 18)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";
                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set18 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 5:
                            {
                                if (set5 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 5)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;

                                            t.IGroupName = $"{item.GroupName}";

                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set5 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 10:
                            {
                                if (set10 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 10)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";
                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set10 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 20:
                            {
                                if (set20 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 20)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";

                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set20 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 24:
                            {
                                if (set24 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 24)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";

                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set24 = true;
                                        }
                                    }
                                }
                                break;
                            }
                        case 16:
                            {
                                if (set16 == false)
                                {
                                    foreach (var item in taxGroupDbList)
                                    {
                                        if (item.TaxRate == 16)
                                        {
                                            IngenicoTaxs t = new IngenicoTaxs();
                                            t.ITaxGroupID = item.TaxGroupID;
                                            t.IGroupName = "KISIM %" + taxRate.ToString();
                                            t.IGroupName = $"{item.GroupName}";

                                            t.ITaxRate = taxRate;
                                            t.ITaxIndex = i;
                                            string JsonData = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                                            item.ingenico = JsonData;
                                            set16 = true;
                                        }
                                    }
                                }
                                break;
                            }
                    }
                }

                #endregion

                #region yazarkasaya gönderilen departman listesi oluşturulyor..

                ST_DEPARTMENT[] stDepartments = new ST_DEPARTMENT[taxGroupDbList.Count()];

                for (int i = 0; i < taxGroupDbList.Count(); i++)
                {
                    stDepartments[i] = new ST_DEPARTMENT();
                }

                var count = 0;
                foreach (var tax in taxGroupDbList)
                {
                    if (!string.IsNullOrEmpty(tax.ingenico))
                    {
                        var JsonData = Newtonsoft.Json.JsonConvert.DeserializeObject<IngenicoTaxs>(tax.ingenico);
                        if (JsonData != null)
                        {

                            stDepartments[count] = new ST_DEPARTMENT();
                            stDepartments[count].szDeptName = JsonData.IGroupName;
                            stDepartments[count].u8TaxIndex = Convert.ToByte(JsonData.ITaxIndex);
                            stDepartments[count].u64Limit = 99999999;
                            //departmana göre yemek çeki kullanımı için izin
                            //if (stDepartments[count].u8TaxIndex == 1)
                            stDepartments[count].bLuchVoucher = Convert.ToByte(1);
                            count++;
                        }
                    }
                }


                #endregion

                #region yazarkasaya güncel eşleşme listesi gönderiliyor...
                _taxGroupsInfoDto.Data.taxGroupList = taxGroupDbList;
                string defaultAdminPassword = "0000";
                if (!string.IsNullOrEmpty(SettingsValues.adminpassword))
                {
                    defaultAdminPassword = SettingsValues.adminpassword;
                }
                _taxGroupsInfoDto.Data.ReturnCode = Json_GMPSmartDLL.FP3_SetDepartments(DataStore.CurrentInterface, ref stDepartments, Convert.ToByte(taxGroupDbList.Count()), defaultAdminPassword);

                if (_taxGroupsInfoDto.Data.ReturnCode == Defines.TRAN_RESULT_OK)
                {

                    //LogManager.Append("Departman parametreleri yüklendi");
                }
                else
                {
                    //LogManager.Append("Departman parametreleri yüklenemedi. Hata : " + ErrorClass.DisplayErrorMessage(_taxGroupsInfoDto.ReturnCode));
                    _taxGroupsInfoDto.Data.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)_taxGroupsInfoDto.Data.ReturnCode);
                    _taxGroupsInfoDto.Data.ReturnMessage = ErrorClass.DisplayErrorMessage(_taxGroupsInfoDto.Data.ReturnCode);
                }

                #endregion
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "FiscalModule.Ingenico.GmpProvider.Pairings.TaxGroupsPairing");
            }

            DataStore.gmpResult.GmpInfo.TaxGroupsInfos = _taxGroupsInfoDto.Data;

            return _taxGroupsInfoDto;
        }


        public IngenicoApiResponse<TaxGroupsInfoDto> GettaxGroups()
        {
            var _taxGroupsInfoDto = new IngenicoApiResponse<TaxGroupsInfoDto>();
            _taxGroupsInfoDto.Data = new TaxGroupsInfoDto();
            ST_TAX_RATE[] stTaxRates = new ST_TAX_RATE[8];
            var numberOfTotalTaxratesReceived = 0;
            var numberOfTotalTaxRates = 0;

            _taxGroupsInfoDto.Data.ReturnCode = Json_GMPSmartDLL.FP3_GetTaxRates(DataStore.CurrentInterface, ref numberOfTotalTaxRates,
                    ref numberOfTotalTaxratesReceived, ref stTaxRates, 8);
            if (_taxGroupsInfoDto.Data.ReturnCode != Defines.TRAN_RESULT_OK)
            {
                _taxGroupsInfoDto.Data.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(_taxGroupsInfoDto.Data.ReturnCode);
                _taxGroupsInfoDto.Data.ReturnMessage = ErrorClass.DisplayErrorMessage(_taxGroupsInfoDto.Data.ReturnCode);
            }
            else
            {
                for (int i = 0; i < stTaxRates.Length; i++)
                {
                    _taxGroupsInfoDto.Data.taxRate.Add(stTaxRates[i]);
                }
            }
                return _taxGroupsInfoDto;
        }

        public IngenicoApiResponse<ST_DEPARTMENT[]> GetDepartmans()
        {

            var response = new IngenicoApiResponse<ST_DEPARTMENT[]>();

            ST_DEPARTMENT[] stDepartments = new ST_DEPARTMENT[12];
            int numberOfTotalDepartments = 0;
            int numberOfTotalDepartmentsReceived = 0;
            uint retcode = Json_GMPSmartDLL.FP3_GetDepartments(DataStore.gmpResult.GmpInfo.CurrentInterface, ref numberOfTotalDepartments, ref numberOfTotalDepartmentsReceived, ref stDepartments, 12);
            if (retcode != 0)
            {

                response.Status = false;
                response.ErrorCode = retcode.ToString();
                response.Message = ErrorClass.DisplayErrorMessage((uint)retcode);
                return response; 
            }
            response.Data = stDepartments; 
            return response;
        }
    }
}
