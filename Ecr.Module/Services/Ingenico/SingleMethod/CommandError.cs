using Ecr.Module.Services.Ingenico.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Services.Ingenico.SingleMethod
{
    public class CommandError
    {
        public static GmpPrintReceiptDto CommandErrorMessage(int Code, string CodeMessage, string StringMessage , GmpPrintReceiptDto printResult = null)
        {
            var result = new GmpPrintReceiptDto();
            if (printResult != null)
            {
                result = printResult;
            }
            result.ReturnCode = (uint)Code; result.ReturnCodeMessage = CodeMessage; result.ReturnStringMessage = StringMessage;

            return result;
        }
    }
}
