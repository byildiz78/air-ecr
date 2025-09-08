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
    public class RunCCommand
    {
        public static BatchCommandResultDto RunBatchCommand(List<GmpCommand> commandList)
        {
            //LogManager.Append("RunBatchCommand  : çalıştırılıyor..");
            var _batchCommandResult = new BatchCommandResultDto();
            ST_TICKET ticket = new ST_TICKET();
            uint retcode = Defines.TRAN_RESULT_OK;
            byte[] sendBuffer = new byte[1024 * 16];
            ushort sendBufferLen = 0;
            ST_MULTIPLE_RETURN_CODE[] stReturnCodes = new ST_MULTIPLE_RETURN_CODE[1024];
            uint msgCommandType = 0;
            byte[] msgBuffer = new byte[1024 * 16];	// this is buffer for just one msg type (exp: GMP_FISCAL_PRINTER_REQ or GMP_EXT_DEVICE_GET ...
            ushort msgBufferLen = 0;
            ushort numberOfreturnCodes = 512;

            try
            {
                //LogManager.Append("GetBatchCommand  : çalıştırılıyor..");
                sendBufferLen = (ushort)GetBatchCommand(sendBuffer, commandList);
                //LogManager.Append("GetBatchCommand  : bitti..");
                if (sendBufferLen > 0)
                {
                    ushort proccessedBufferReadLen = 0;
                    ushort tempNumberOfreturnCodes = 0;
                    while (proccessedBufferReadLen < sendBufferLen && retcode == Defines.TRAN_RESULT_OK)
                    {
                        proccessedBufferReadLen += GMPSmartDLL.gmpReadTLVtag(ref msgCommandType, sendBuffer, proccessedBufferReadLen);
                        proccessedBufferReadLen += GMPSmartDLL.gmpReadTLVlen_HL(ref msgBufferLen, sendBuffer, proccessedBufferReadLen);
                        Buffer.BlockCopy(sendBuffer, proccessedBufferReadLen, msgBuffer, 0, msgBufferLen);
                        proccessedBufferReadLen += msgBufferLen;
                        tempNumberOfreturnCodes = 0;

                        bool isPrepareStart = commandList.Any(x => x.Command == "prepare_Start");
                        if (isPrepareStart)
                        {
                            DataStore.ActiveTransactionHandle = 0;
                        }

                        //LogManager.Append($"FiscalPrinter_MultipleCommand  : çalıştırılıyor.. ActiveTransactionHandle : {ActiveTransactionHandle}");
                        // Send to ECR and wait for the response (one error code for each sub command until one of them is failed !!)
                        retcode = Json_GMPSmartDLL.FP3_MultipleCommand(DataStore.CurrentInterface, ref DataStore.ActiveTransactionHandle, ref stReturnCodes, ref numberOfreturnCodes, msgBuffer, msgBufferLen, ref ticket, 1000 * 500); //300 //900

                        //LogManager.Append(string.Format("FiscalPrinter_MultipleCommand  : {0}", ErrorClass.DisplayErrorMessage(retcode)));
                        numberOfreturnCodes += tempNumberOfreturnCodes;
                    }
                    byte[] returnCodeStringMessage = new byte[256];
                    GMPSmartDLL.GetErrorMessage(retcode, returnCodeStringMessage);

                    _batchCommandResult.ReturnCode = retcode;
                    _batchCommandResult.ReturnMessage = GMP_Tools.SetEncoding(returnCodeStringMessage).Replace("\u0000", "");
                    _batchCommandResult.ReceiptInfo = ticket;
                   // LogManager.Append($"RunBatchCommand._batchCommandResult.ReceiptInfo : {Newtonsoft.Json.JsonConvert.SerializeObject(ticket)}");
                   // LogManager.Append($"RunBatchCommand._batchCommandResult.stReturnCodes : {Newtonsoft.Json.JsonConvert.SerializeObject(stReturnCodes)}");
                    if (stReturnCodes != null)
                    {
                        for (int i = 0; i < stReturnCodes.Count(); i++)
                        {
                            if (stReturnCodes[i] == null)
                                continue;
                            byte[] returnCodeString = new byte[256];
                            // This is not a result of subCommand (it is a tag value in Get Response )
                            if (stReturnCodes[i].indexOfSubCommand == 0)
                                continue;

                            // This is not a result of subCommand (it is a tag value in Get Response )
                            if (stReturnCodes[i].subCommand == 0)
                                continue;

                            GMPSmartDLL.GetErrorMessage(stReturnCodes[i].retcode, returnCodeString);

                            commandList[i].ReturnCode = (int)stReturnCodes[i].retcode;
                            commandList[i].ReturnValue = GMP_Tools.SetEncoding(returnCodeString).Replace("\u0000", "");
                            commandList[i].SubCommand = (int)stReturnCodes[i].subCommand;
                            commandList[i].SubCommandIndex = stReturnCodes[i].indexOfSubCommand;
                            _batchCommandResult.GmpCommandInfo = commandList;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               // LogManager.Exception(ex, "BatchCommadGmpProvider.RunBatchCommand");
            }

            return _batchCommandResult;
        }

        public static int GetBatchCommand(byte[] sendBuffer, IEnumerable<GmpCommand> commandList)
        {
            uint msgCommandType = 0;
            int sendBufferLen = 0;
            byte[] msgBuffer = new byte[1024 * 16]; // this is buffer for just one msg type (exp: GMP_FISCAL_PRINTER_REQ or GMP_EXT_DEVICE_GET ...
            ushort msgBufferLen = 0;
            try
            {
                foreach (var item in commandList)
                {
                    byte[] ptrData = new byte[1024];
                    int ptrDataLen = 0;
                    ushort dataLen = 0;
                    uint dataCommandType;
                    string rowData = item.BufferData;
                    CommandHelperGmpProvider.StringToByteArray(rowData, ptrData, ref ptrDataLen);

                    if (ptrDataLen == 0)
                        // There is no data to send
                        continue;

                    // This is the data to be sent to ECR.
                    string CommandType = "";
                    for (int j = ptrDataLen - 4; j < ptrDataLen; j++)
                    {
                        CommandType += ptrData[j].ToString("X2");
                    }
                    dataCommandType = uint.Parse(CommandType, System.Globalization.NumberStyles.HexNumber);

                    string dataLenStr = "";
                    for (int j = ptrDataLen - 6; j < ptrDataLen - 4; j++)
                    {
                        dataLenStr += ptrData[j].ToString("X2");
                    }
                    dataLen = ushort.Parse(dataLenStr, System.Globalization.NumberStyles.HexNumber);

                    if (msgCommandType == 0)
                        msgCommandType = dataCommandType;

                    if (msgCommandType != dataCommandType)
                    {

                        // this means that the msgCommandType is changing in the list, so close the previous package..
                        sendBufferLen += GMPSmartDLL.gmpSetTLV_HLEx(sendBuffer, sendBufferLen, sendBuffer.Length - sendBufferLen, msgCommandType, msgBuffer, msgBufferLen);
                        msgBufferLen = 0;
                        msgCommandType = dataCommandType;
                    }

                    CommandHelperGmpProvider.StringToByteArray_Rev(rowData, ptrData, ref ptrDataLen);

                    string msgBufferStr = "";
                    for (int j = 6; j < dataLen; j++)
                    {
                        msgBufferStr += ptrData[j].ToString("X2");
                    }

                    Buffer.BlockCopy(ptrData, 6, msgBuffer, msgBufferLen, dataLen);
                    msgBufferLen += dataLen;
                }

                if (msgBufferLen != 0)
                {
                    // this means that the msgCommandType is changing in the list, so close the privous package..
                    sendBufferLen += GMPSmartDLL.gmpSetTLV_HLEx(sendBuffer, sendBufferLen, sendBuffer.Length - sendBufferLen, msgCommandType, msgBuffer, msgBufferLen);
                    msgBufferLen = 0;
                }
            }
            catch (Exception ex)
            {
               // LogManager.Exception(ex, "BatchCommadGmpProvider.GetBatchCommand");
            }

            return sendBufferLen;
        }
    }
}
