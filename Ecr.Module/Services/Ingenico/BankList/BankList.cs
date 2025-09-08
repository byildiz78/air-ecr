using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.BankList
{
    public class BankList
    {
        public IngenicoApiResponse<List<BankInfoDto>> GetBankList()
        {
            var response = new IngenicoApiResponse<List<BankInfoDto>>();

            try
            {

                byte numberOfTotalRecords = 0;
                byte numberOfTotalRecordsReceived = 0;
                ST_PAYMENT_APPLICATION_INFO[] stPaymentApplicationInfo = new ST_PAYMENT_APPLICATION_INFO[24];
                BankInfoDto _bankInfo = new BankInfoDto();
                List<BankInfoDto> _bankInfoList = new List<BankInfoDto>();
                uint retvalue = Json_GMPSmartDLL.FP3_GetPaymentApplicationInfo(DataStore.CurrentInterface, ref numberOfTotalRecords, ref numberOfTotalRecordsReceived, ref stPaymentApplicationInfo, 24);

                //LogManager.Append($"ReturnCode : {retvalue}", "CommandHelperGmpProvider -> GmpBankList() -> FP3_GetPaymentApplicationInfo");

                if (retvalue == Defines.TRAN_RESULT_OK)
                {
                    for (int i = 0; i < numberOfTotalRecordsReceived; i++)
                    {
                        _bankInfo = new BankInfoDto
                        {
                            Name = Encoding.Default.GetString(stPaymentApplicationInfo[i].name).TrimEnd('\0'),
                            u16BKMId = stPaymentApplicationInfo[i].u16BKMId.ToString(),
                            Status = stPaymentApplicationInfo[i].Status.ToString(),
                            Priority = stPaymentApplicationInfo[i].Priority.ToString()
                        };

                        _bankInfoList.Add(_bankInfo);
                    }
                    response.Data = _bankInfoList;
                    response.Status = true;
                    response.ErrorCode = Defines.TRAN_RESULT_OK.ToString();
                    response.Message = "TRAN_RESULT_OK";
                }
                else
                {
                    response.Status = false;
                    response.ErrorCode = retvalue.ToString();
                    response.Message = ErrorClass.DisplayErrorMessage(retvalue) + " - " + ErrorClass.DisplayErrorCodeMessage(retvalue);
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
    }
}
