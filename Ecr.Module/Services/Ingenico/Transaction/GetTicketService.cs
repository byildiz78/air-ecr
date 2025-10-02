using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Models;
using Ecr.Module.Services.Ingenico.Retry;
using System;

namespace Ecr.Module.Services.Ingenico.Transaction
{
    /// <summary>
    /// FP3_GetTicket işlemleri için service
    /// PDF Section 5.2 - FP3_GetTicket Usage
    /// </summary>
    public class GetTicketService
    {
        private readonly TransactionManager _transactionManager;
        private readonly ConnectionManager _connectionManager;

        public GetTicketService()
        {
            _transactionManager = TransactionManager.Instance;
            _connectionManager = ConnectionManager.Instance;
        }

        /// <summary>
        /// Ticket bilgisini al (retry ile)
        /// </summary>
        public GetTicketResult GetTicket(bool includeTransactionItems = false)
        {
            var result = new GetTicketResult();

            try
            {
                // Transaction kontrolü
                if (!_transactionManager.HasActiveTransaction())
                {
                    result.Success = false;
                    result.ErrorCode = Defines.APP_ERR_GMP3_NO_HANDLE;
                    result.ErrorMessage = "No active transaction";
                    return result;
                }

                var transaction = _transactionManager.GetCurrentTransaction();
                var connectionState = _connectionManager.GetState();

                if (connectionState.CurrentInterface == 0)
                {
                    result.Success = false;
                    result.ErrorMessage = "No valid interface";
                    return result;
                }

                // Retry ile FP3_GetTicket çağır
                var retryResult = ConnectionRetryHelper.GetTicketWithRetry(
                    connectionState.CurrentInterface,
                    transaction.Handle,
                    ref result.Ticket,
                    Defines.TIMEOUT_DEFAULT,
                    RetryPolicy.Default
                );

                result.ErrorCode = retryResult.Result;
                result.Success = retryResult.Success;
                result.AttemptCount = retryResult.AttemptCount;

                if (result.Success)
                {
                    // Ticket bilgisini transaction'a kaydet
                    _transactionManager.UpdateTicketInfo(result.Ticket);

                    // Ticket analizi yap
                    result.Analysis = AnalyzeTicket(result.Ticket);
                }
                else
                {
                    result.ErrorMessage = ErrorClass.DisplayErrorMessage(result.ErrorCode);

                    // Critical error ise transaction'ı error state'e al
                    if (IsCriticalError(result.ErrorCode))
                    {
                        _transactionManager.SetError(result.ErrorCode);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Ticket bilgisini al ve validate et
        /// </summary>
        public GetTicketResult GetAndValidateTicket()
        {
            var result = GetTicket();

            if (result.Success)
            {
                // Ticket consistency check
                var transaction = _transactionManager.GetCurrentTransaction();
                bool isConsistent = TransactionValidator.IsStateConsistent(
                    transaction,
                    result.Ticket
                );

                if (!isConsistent)
                {
                    result.Success = false;
                    result.ErrorMessage = "Ticket state inconsistent with transaction state";
                }
            }

            return result;
        }

        /// <summary>
        /// Transaction recovery için GetTicket kullan
        /// PDF Section 5.2.3 - Using GetTicket for Recovery
        /// </summary>
        public TransactionRecoveryResult RecoverWithGetTicket()
        {
            // TransactionManager'ın recovery metodunu kullan
            return _transactionManager.RecoverTransaction();
        }

        /// <summary>
        /// Ticket analizi yap
        /// </summary>
        private TicketAnalysis AnalyzeTicket(ST_TICKET ticket)
        {
            var analysis = new TicketAnalysis
            {
                HasTicket = ticket.FNo > 0,
                FiscalNumber = ticket.FNo,
                IsGmp3Transaction = (ticket.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_GMP3) != 0,
                IsHeaderPrinted = (ticket.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_TICKET_HEADER_PRINTED) != 0,
                IsInvoice = (ticket.TransactionFlags & (uint)ETransactionFlags.FLG_XTRANS_INVOICE_PARAMETERS_SET) != 0,
                HasPayment = ticket.TotalReceiptPayment > 0,
                ItemCount = ticket.totalNumberOfItems,
                TotalAmount = ticket.TotalReceiptAmount
            };

            // Transaction durumunu belirle
            if (analysis.HasTicket)
            {
                if (analysis.HasPayment && ticket.totalNumberOfItems > 0)
                {
                    analysis.TransactionComplete = true;
                }
                else if (!analysis.IsHeaderPrinted)
                {
                    analysis.TransactionComplete = false;
                    analysis.CanResume = true;
                }
            }

            return analysis;
        }

        /// <summary>
        /// Error critical mi?
        /// </summary>
        private bool IsCriticalError(uint errorCode)
        {
            var errorInfo = ErrorCodeCategorizer.GetErrorInfo(errorCode);
            return errorInfo.Category == ConnectionErrorCategory.Fatal;
        }
    }

    /// <summary>
    /// GetTicket sonucu
    /// </summary>
    public class GetTicketResult
    {
        public bool Success { get; set; }
        public ST_TICKET Ticket { get; set; }
        public uint ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public int AttemptCount { get; set; }
        public TicketAnalysis Analysis { get; set; }

        public GetTicketResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
            Analysis = new TicketAnalysis();
        }
    }

    /// <summary>
    /// Ticket analiz sonucu
    /// </summary>
    public class TicketAnalysis
    {
        public bool HasTicket { get; set; }
        public uint FiscalNumber { get; set; }
        public bool IsGmp3Transaction { get; set; }
        public bool IsHeaderPrinted { get; set; }
        public bool IsInvoice { get; set; }
        public bool HasPayment { get; set; }
        public uint ItemCount { get; set; }
        public uint TotalAmount { get; set; }
        public bool TransactionComplete { get; set; }
        public bool CanResume { get; set; }

        public TicketAnalysis()
        {
            HasTicket = false;
            TransactionComplete = false;
            CanResume = false;
        }
    }
}