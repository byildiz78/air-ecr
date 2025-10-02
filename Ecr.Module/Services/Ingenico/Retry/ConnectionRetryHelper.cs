using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;

namespace Ecr.Module.Services.Ingenico.Retry
{
    /// <summary>
    /// Connection işlemleri için özel retry helper
    /// </summary>
    public static class ConnectionRetryHelper
    {
        /// <summary>
        /// Ping işlemini retry ile yap
        /// </summary>
        public static RetryResult<uint> PingWithRetry(
            uint interfaceHandle,
            RetryPolicy policy = null)
        {
            return RetryExecutor.Execute(
                () => GmpConnectionWrapper.Ping(interfaceHandle, 1100),
                result => result == Defines.TRAN_RESULT_OK,
                policy ?? RetryPolicy.Default,
                (attempt, ex) =>
                {
                    // Log retry attempt
                    System.Diagnostics.Debug.WriteLine($"Ping retry attempt {attempt}: {ex?.Message}");
                }
            );
        }

        /// <summary>
        /// Echo işlemini retry ile yap
        /// </summary>
        public static RetryResult<uint> EchoWithRetry(
            uint interfaceHandle,
            ref ST_ECHO stEcho,
            int timeout,
            RetryPolicy policy = null)
        {
            // ref parametresi olduğu için özel handling
            ST_ECHO echoResult = stEcho;
            var result = RetryExecutor.Execute(
                () =>
                {
                    var tempEcho = echoResult;
                    uint retCode = GmpConnectionWrapper.Echo(interfaceHandle, ref tempEcho, timeout);
                    echoResult = tempEcho;
                    return retCode;
                },
                retCode => retCode == Defines.TRAN_RESULT_OK,
                policy ?? RetryPolicy.Default,
                (attempt, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Echo retry attempt {attempt}: {ex?.Message}");
                }
            );

            stEcho = echoResult;
            return result;
        }

        /// <summary>
        /// Pairing işlemini retry ile yap
        /// </summary>
        public static RetryResult<uint> PairingWithRetry(
            uint interfaceHandle,
            ref ST_GMP_PAIR pairing,
            ref ST_GMP_PAIR_RESP pairingResp,
            RetryPolicy policy = null)
        {
            // ref parametrelerini lambda öncesi kopyala
            ST_GMP_PAIR pairingCopy = pairing;
            ST_GMP_PAIR_RESP pairingResult = pairingResp;

            var result = RetryExecutor.Execute(
                () =>
                {
                    var tempPairing = pairingCopy;
                    var tempPairingResp = pairingResult;
                    uint retCode = GmpConnectionWrapper.StartPairingInit(
                        interfaceHandle,
                        ref tempPairing,
                        ref tempPairingResp
                    );
                    pairingResult = tempPairingResp;
                    return retCode;
                },
                retCode => retCode == Defines.TRAN_RESULT_OK,
                policy ?? RetryPolicy.Conservative, // Pairing için conservative policy
                (attempt, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Pairing retry attempt {attempt}: {ex?.Message}");
                }
            );

            pairingResp = pairingResult;
            return result;
        }

        /// <summary>
        /// GetTicket işlemini retry ile yap
        /// </summary>
        public static RetryResult<uint> GetTicketWithRetry(
            uint interfaceHandle,
            ulong transactionHandle,
            ref ST_TICKET ticket,
            int timeout,
            RetryPolicy policy = null)
        {
            ST_TICKET ticketResult = ticket;
            var result = RetryExecutor.Execute(
                () =>
                {
                    var tempTicket = ticketResult;
                    uint retCode = GmpConnectionWrapper.GetTicket(
                        interfaceHandle,
                        transactionHandle,
                        ref tempTicket,
                        timeout
                    );
                    ticketResult = tempTicket;
                    return retCode;
                },
                retCode => retCode == Defines.TRAN_RESULT_OK,
                policy ?? RetryPolicy.Default,
                (attempt, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"GetTicket retry attempt {attempt}: {ex?.Message}");
                }
            );

            ticket = ticketResult;
            return result;
        }

        /// <summary>
        /// Generic GMP operation'ı retry ile çalıştır
        /// </summary>
        public static RetryResult<T> ExecuteGmpOperation<T>(
            Func<T> operation,
            Func<T, bool> isSuccess,
            RetryPolicy policy = null,
            string operationName = "GMP Operation")
        {
            return RetryExecutor.Execute(
                operation,
                isSuccess,
                policy ?? RetryPolicy.Default,
                (attempt, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine($"{operationName} retry attempt {attempt}: {ex?.Message}");
                }
            );
        }
    }
}