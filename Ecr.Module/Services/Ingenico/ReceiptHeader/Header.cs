using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;

namespace Ecr.Module.Services.Ingenico.ReceiptHeader
{
    public class Header
    {
        public IngenicoApiResponse<FiscalHeaderDto> GmpGetReceiptHeader()
        {
            var response = new IngenicoApiResponse<FiscalHeaderDto>();
            try
            {
                ST_TICKET_HEADER stTicketHeader = new ST_TICKET_HEADER();
                ushort totalNumberOfHeaderPlaces = 0;

                var returnCode = Json_GMPSmartDLL.FP3_GetTicketHeader(DataStore.CurrentInterface, 0xFF, ref stTicketHeader, ref totalNumberOfHeaderPlaces, Defines.TIMEOUT_DEFAULT);

                DataStore.gmpResult.GmpInfo.fiscalHeader.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage(returnCode);
                DataStore.gmpResult.GmpInfo.fiscalHeader.ReturnStringMessage = ErrorClass.DisplayErrorMessage(returnCode);
                
                //LogManager.Append($"ReturnCode : {DataStore.gmpResult.GmpInfo.fiscalHeader.ReturnCode}-{DataStore.gmpResult.GmpInfo.fiscalHeader.ReturnCodeMessage}", "CommandHelperGmpProvider -> GmpReceiptHeader() -> GmpReceiptHeader");

                if (returnCode == Defines.TRAN_RESULT_OK)
                {
                    DataStore.gmpResult.GmpInfo.fiscalHeader.ReceiptTitle1 = stTicketHeader.szMerchName1;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.ReceiptTitle2 = stTicketHeader.szMerchName2;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.ReceiptAdres = stTicketHeader.szMerchAddr1;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.ReceiptAdres2 = stTicketHeader.szMerchAddr2;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.ReceiptAdres3 = stTicketHeader.szMerchAddr3;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.MersisNo = stTicketHeader.MersisNo;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.TicariSicilNo = stTicketHeader.TicariSicilNo;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.VATNumber = stTicketHeader.VATNumber;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.VATOffice = stTicketHeader.VATOffice;
                    DataStore.gmpResult.GmpInfo.fiscalHeader.WebAddress = stTicketHeader.WebAddress;

                    response.Data = DataStore.gmpResult.GmpInfo.fiscalHeader;
                    response.Status = true;
                    response.ErrorCode = "TRAN_RESULT_OK";
                    response.Message = "";
                }
                else
                {
                    response.Status = false;
                    response.ErrorCode = returnCode.ToString();
                    response.Message = ErrorClass.DisplayErrorMessage(returnCode) + " - " + ErrorClass.DisplayErrorCodeMessage(returnCode);
                }

            }
            catch (Exception ex)
            {
                response.Status = false;
                response.ErrorCode = "9999";
                response.Message = ex.Message;
            }

            return response;
        }

        public IngenicoApiResponse<FiscalHeaderDto> GmpReceiptHeaderSend(FiscalHeaderDto _headers)
        {
            var response = new IngenicoApiResponse<FiscalHeaderDto>();

            try
            {
                ST_TICKET_HEADER stTicketHeader = new ST_TICKET_HEADER();
                ushort UsedNumberOfHeaderPlaces = 0;
                ushort totalNumberOfHeaderPlaces = 0;

                _headers.ReturnCode = Json_GMPSmartDLL.FP3_GetTicketHeader(DataStore.gmpResult.GmpInfo.CurrentInterface, 0xFF, ref stTicketHeader, ref totalNumberOfHeaderPlaces, Defines.TIMEOUT_DEFAULT);
                if (_headers.ReturnCode != Defines.TRAN_RESULT_OK)
                {
                    response.Message = ErrorClass.DisplayErrorCodeMessage((uint)_headers.ReturnCode) +" "+ ErrorClass.DisplayErrorMessage(_headers.ReturnCode);
                    response.Data = _headers;  
                    response.Status = false;
                    _headers.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)_headers.ReturnCode);
                    _headers.ReturnStringMessage = ErrorClass.DisplayErrorMessage(_headers.ReturnCode);
                    return response;

                }

                stTicketHeader = new ST_TICKET_HEADER
                {
                    szMerchName1 = _headers.ReceiptTitle1,
                    szMerchName2 = _headers.ReceiptTitle2,
                    szMerchAddr1 = _headers.ReceiptAdres,
                    szMerchAddr2 = _headers.ReceiptAdres2,
                    szMerchAddr3 = _headers.ReceiptAdres3,
                    MersisNo = _headers.MersisNo,
                    TicariSicilNo = _headers.TicariSicilNo,
                    VATNumber = _headers.VATNumber,
                    VATOffice = _headers.VATOffice,
                    WebAddress = _headers.WebAddress
                };

                string defaultAdminPassword = "0000";
                if (!string.IsNullOrEmpty(SettingsValues.adminpassword))
                {
                    defaultAdminPassword = SettingsValues.adminpassword;
                }

                _headers.ReturnCode = Json_GMPSmartDLL.FP3_FunctionChangeTicketHeader(DataStore.gmpResult.GmpInfo.CurrentInterface, defaultAdminPassword, ref totalNumberOfHeaderPlaces, ref UsedNumberOfHeaderPlaces, ref stTicketHeader, Defines.TIMEOUT_DEFAULT);

                response.Message = ErrorClass.DisplayErrorCodeMessage((uint)_headers.ReturnCode) + " " + ErrorClass.DisplayErrorMessage(_headers.ReturnCode);

                _headers.ReturnCodeMessage = ErrorClass.DisplayErrorCodeMessage((uint)_headers.ReturnCode);
                _headers.ReturnStringMessage = ErrorClass.DisplayErrorMessage(_headers.ReturnCode);
                
                if (_headers.ReturnCode == Defines.TRAN_RESULT_OK)
                {
                    _headers.ReceiptTitle1 = stTicketHeader.szMerchName1;
                    _headers.ReceiptTitle2 = stTicketHeader.szMerchName2;
                    _headers.ReceiptAdres = stTicketHeader.szMerchAddr1;
                    _headers.ReceiptAdres2 = stTicketHeader.szMerchAddr2;
                    _headers.ReceiptAdres3 = stTicketHeader.szMerchAddr3;
                    _headers.MersisNo = stTicketHeader.MersisNo;
                    _headers.TicariSicilNo = stTicketHeader.TicariSicilNo;
                    _headers.VATNumber = stTicketHeader.VATNumber;
                    _headers.VATOffice = stTicketHeader.VATOffice;
                    _headers.WebAddress = stTicketHeader.WebAddress;
                }
                response.Data = _headers;
            }
            catch (Exception ex)
            {
            }
            return response;
        }
    }
}
