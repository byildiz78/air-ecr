using Ecr.Module.Services.Ingenico.GmpIngenico;

namespace Ecr.Module.Services.Ingenico.Transaction
{
    /// <summary>
    /// Transaction validation işlemleri
    /// PDF Section 5.2 - Transaction Handle Validation
    /// </summary>
    public static class TransactionValidator
    {
        /// <summary>
        /// Handle geçerli mi kontrol et
        /// </summary>
        public static bool IsHandleValid(ulong handle)
        {
            if (handle == 0)
            {
                return false;
            }

            // Handle 0 değilse geçerli kabul et
            // Gerçek validasyonu FP3_GetTicket ile yapacağız
            return true;
        }

        /// <summary>
        /// Handle'ın gerçekten aktif olduğunu FP3_GetTicket ile kontrol et
        /// </summary>
        public static TransactionValidationResult ValidateWithGetTicket(
            uint interfaceHandle,
            ulong transactionHandle,
            uint timeout)
        {
            var result = new TransactionValidationResult
            {
                Handle = transactionHandle
            };

            if (transactionHandle == 0)
            {
                result.IsValid = false;
                result.ErrorCode = Defines.APP_ERR_GMP3_NO_HANDLE;
                result.ErrorMessage = "Transaction handle is 0";
                return result;
            }

            try
            {
                ST_TICKET ticket = new ST_TICKET();
                uint retCode = Json_GMPSmartDLL.FP3_GetTicket(
                    interfaceHandle,
                    transactionHandle,
                    ref ticket,
                    (int)timeout
                );

                result.ReturnCode = retCode;
                result.TicketInfo = ticket;

                // Return code'a göre validation
                switch (retCode)
                {
                    case Defines.TRAN_RESULT_OK:
                        result.IsValid = true;
                        result.IsActive = true;
                        break;

                    case Defines.APP_ERR_GMP3_NO_HANDLE:
                        result.IsValid = false;
                        result.IsActive = false;
                        result.ErrorMessage = "No active transaction found";
                        break;

                    case Defines.APP_ERR_GMP3_INVALID_HANDLE:
                        result.IsValid = false;
                        result.IsActive = false;
                        result.ErrorMessage = "Invalid transaction handle";
                        break;

                    default:
                        result.IsValid = false;
                        result.IsActive = false;
                        result.ErrorMessage = ErrorClass.DisplayErrorMessage(retCode);
                        break;
                }

                result.ErrorCode = retCode;
            }
            catch (System.Exception ex)
            {
                result.IsValid = false;
                result.IsActive = false;
                result.ErrorMessage = $"Exception during validation: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Transaction state'in tutarlı olduğunu kontrol et
        /// </summary>
        public static bool IsStateConsistent(TransactionInfo info, ST_TICKET ticket)
        {
            if (info == null || !info.IsHandleValid)
            {
                return false;
            }

            // Ticket bilgisi ile state tutarlı mı?
            switch (info.State)
            {
                case TransactionState.Completed:
                    // Completed state'te ticket'ın tamamlanmış olması gerekir
                    return ticket.FNo > 0;

                case TransactionState.Cancelled:
                    // Cancelled state'te ticket olmamalı veya iptal edilmiş olmalı
                    return true;

                case TransactionState.Error:
                    // Error state'te her şey olabilir
                    return true;

                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// Transaction validation sonucu
    /// </summary>
    public class TransactionValidationResult
    {
        public ulong Handle { get; set; }
        public bool IsValid { get; set; }
        public bool IsActive { get; set; }
        public uint ErrorCode { get; set; }
        public uint ReturnCode { get; set; }
        public string ErrorMessage { get; set; }
        public ST_TICKET TicketInfo { get; set; }

        public TransactionValidationResult()
        {
            IsValid = false;
            IsActive = false;
            ErrorMessage = string.Empty;
        }
    }
}