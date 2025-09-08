using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Helper;
using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class StartCommand
    {
        public static List<BatchCommandResultDto> RunCommand(IEnumerable<GmpCommand> commandList, int commandCount = 15)
        {
            BatchCommandResultDto command = new BatchCommandResultDto();
            List<BatchCommandResultDto> commandResultList = new List<BatchCommandResultDto>();
            bool IsSubGmpCommandErrorCode = false;
            int partNumber = 1;
            //LogManager.Append($"RunCommand metodu çalıştırılıyor..{commandList.Count()}/{commandCount}");

            try
            {
                IEnumerable<List<GmpCommand>> PartCommadList = CommandHelperGmpProvider.InSetsOf(commandList, commandCount);

                foreach (var partList in PartCommadList)
                {
                    //LogManager.Append($"RunCommand.{partNumber.ToString()}.Paket.partList : {Newtonsoft.Json.JsonConvert.SerializeObject(partList)}");

                    command = RunCCommand.RunBatchCommand(partList);
                    commandResultList.Add(command);

                   // LogManager.Append($"RunCommand.{partNumber.ToString()}.Paket.partList.return : {Newtonsoft.Json.JsonConvert.SerializeObject(command)}");

                    //Ana komutta hata döndü ise 
                    if (command.ReturnCode != Defines.TRAN_RESULT_OK)
                    {
                        break;
                    }

                    //Alt komutta hata döndü ise 
                    IsSubGmpCommandErrorCode = command.GmpCommandInfo.Any(w => w.ReturnCode != Defines.TRAN_RESULT_OK);
                    if (IsSubGmpCommandErrorCode)
                    {
                        break;
                    }

                    partNumber++;
                }
            }
            catch (Exception ex)
            {
                //LogManager.Exception(ex, "PrintReceiptGmpProvider.RunCommand");
            }

            return commandResultList;
        }

    }
}
