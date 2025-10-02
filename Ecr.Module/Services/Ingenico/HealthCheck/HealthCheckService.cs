using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.GmpIngenico;
using Ecr.Module.Services.Ingenico.Retry;
using System;
using System.Diagnostics;

namespace Ecr.Module.Services.Ingenico.HealthCheck
{
    /// <summary>
    /// Health check service
    /// PDF Section 3.4 - PING/ECHO/BUSY Flow Control
    /// Recommended: PING-first strategy
    /// </summary>
    public class HealthCheckService
    {
        private readonly ConnectionManager _connectionManager;

        public HealthCheckService()
        {
            _connectionManager = ConnectionManager.Instance;
        }

        /// <summary>
        /// Health check yap (default: PING-first strategy)
        /// </summary>
        public HealthCheckResult PerformHealthCheck(
            HealthCheckStrategy strategy = HealthCheckStrategy.PingFirst,
            HealthCheckLevel level = HealthCheckLevel.Standard)
        {
            var result = new HealthCheckResult
            {
                Strategy = strategy,
                CheckTime = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var connectionState = _connectionManager.GetState();
                if (connectionState.CurrentInterface == 0)
                {
                    result.IsHealthy = false;
                    result.ErrorMessage = "No valid interface";
                    return result;
                }

                switch (strategy)
                {
                    case HealthCheckStrategy.PingOnly:
                        result = PerformPingOnly(connectionState.CurrentInterface, level);
                        break;

                    case HealthCheckStrategy.EchoOnly:
                        result = PerformEchoOnly(connectionState.CurrentInterface, level);
                        break;

                    case HealthCheckStrategy.PingFirst:
                        result = PerformPingFirst(connectionState.CurrentInterface, level);
                        break;

                    case HealthCheckStrategy.Both:
                        result = PerformBoth(connectionState.CurrentInterface, level);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.IsHealthy = false;
                result.ErrorMessage = $"Health check exception: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Sadece PING yap (hızlı check)
        /// PDF: Recommended for frequent monitoring
        /// </summary>
        private HealthCheckResult PerformPingOnly(uint interfaceHandle, HealthCheckLevel level)
        {
            var result = new HealthCheckResult
            {
                Strategy = HealthCheckStrategy.PingOnly
            };

            var pingResult = ExecutePing(interfaceHandle);
            result.PingResult = pingResult;
            result.IsHealthy = pingResult.Success;
            result.ErrorCode = pingResult.ReturnCode;
            result.ErrorMessage = pingResult.Message;

            return result;
        }

        /// <summary>
        /// Sadece ECHO yap (detaylı check)
        /// </summary>
        private HealthCheckResult PerformEchoOnly(uint interfaceHandle, HealthCheckLevel level)
        {
            var result = new HealthCheckResult
            {
                Strategy = HealthCheckStrategy.EchoOnly
            };

            var echoResult = ExecuteEcho(interfaceHandle);
            result.EchoResult = echoResult;
            result.IsHealthy = echoResult.Success;
            result.ErrorCode = echoResult.ReturnCode;
            result.ErrorMessage = echoResult.Message;

            return result;
        }

        /// <summary>
        /// PING-first strategy (recommended)
        /// PDF Section 3.4: "Use PING for quick check, ECHO for detailed info"
        /// </summary>
        private HealthCheckResult PerformPingFirst(uint interfaceHandle, HealthCheckLevel level)
        {
            var result = new HealthCheckResult
            {
                Strategy = HealthCheckStrategy.PingFirst
            };

            // 1. Önce PING dene (hızlı)
            var pingResult = ExecutePing(interfaceHandle);
            result.PingResult = pingResult;

            if (pingResult.Success)
            {
                // PING başarılı, detailed info gerekiyorsa ECHO da çağır
                if (level == HealthCheckLevel.Detailed)
                {
                    var echoResult = ExecuteEcho(interfaceHandle);
                    result.EchoResult = echoResult;
                }

                result.IsHealthy = true;
            }
            else
            {
                // PING başarısız, ECHO dene (daha detaylı error info için)
                var echoResult = ExecuteEcho(interfaceHandle);
                result.EchoResult = echoResult;
                result.IsHealthy = echoResult.Success;
                result.ErrorCode = echoResult.Success ? pingResult.ReturnCode : echoResult.ReturnCode;
                result.ErrorMessage = echoResult.Message;
            }

            return result;
        }

        /// <summary>
        /// Hem PING hem ECHO yap (en detaylı ama en yavaş)
        /// </summary>
        private HealthCheckResult PerformBoth(uint interfaceHandle, HealthCheckLevel level)
        {
            var result = new HealthCheckResult
            {
                Strategy = HealthCheckStrategy.Both
            };

            var pingResult = ExecutePing(interfaceHandle);
            var echoResult = ExecuteEcho(interfaceHandle);

            result.PingResult = pingResult;
            result.EchoResult = echoResult;

            // Her ikisi de başarılı olmalı
            result.IsHealthy = pingResult.Success && echoResult.Success;

            if (!result.IsHealthy)
            {
                result.ErrorCode = !pingResult.Success ? pingResult.ReturnCode : echoResult.ReturnCode;
                result.ErrorMessage = !pingResult.Success ? pingResult.Message : echoResult.Message;
            }

            return result;
        }

        /// <summary>
        /// PING execute et
        /// </summary>
        private PingCheckResult ExecutePing(uint interfaceHandle)
        {
            var result = new PingCheckResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Retry ile PING (PDF: 1100ms timeout)
                var retryResult = ConnectionRetryHelper.PingWithRetry(
                    interfaceHandle,
                    RetryPolicy.Conservative // PING için conservative (hızlı fail)
                );

                result.ReturnCode = retryResult.Result;
                result.Success = retryResult.Success;

                if (result.Success)
                {
                    result.Message = "Device is reachable";
                    _connectionManager.UpdateConnectionStatus(Models.ConnectionStatus.Connected);
                }
                else
                {
                    result.Message = ErrorClass.DisplayErrorMessage(result.ReturnCode);
                    _connectionManager.UpdateConnectionStatus(Models.ConnectionStatus.NotConnected);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"PING exception: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// ECHO execute et
        /// </summary>
        private EchoCheckResult ExecuteEcho(uint interfaceHandle)
        {
            var result = new EchoCheckResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ST_ECHO stEcho = new ST_ECHO();

                // Retry ile ECHO
                var retryResult = ConnectionRetryHelper.EchoWithRetry(
                    interfaceHandle,
                    ref stEcho,
                    Defines.TIMEOUT_ECHO,
                    RetryPolicy.Default
                );

                result.ReturnCode = retryResult.Result;
                result.Success = retryResult.Success;
                result.EchoData = stEcho;

                if (result.Success)
                {
                    // ECHO bilgilerini parse et
                    result.ActiveCashier = stEcho.activeCashier.name;
                    result.ActiveCashierNo = stEcho.activeCashier.index + 1;
                    result.EcrStatus = (int)stEcho.status;
                    result.EcrMode = stEcho.ecrMode;
                    result.Message = "Device detailed info retrieved";

                    _connectionManager.UpdateConnectionStatus(Models.ConnectionStatus.Connected);
                }
                else
                {
                    result.Message = ErrorClass.DisplayErrorMessage(result.ReturnCode);
                    _connectionManager.UpdateConnectionStatus(Models.ConnectionStatus.NotConnected);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"ECHO exception: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Quick health check (sadece connectivity)
        /// </summary>
        public bool IsDeviceReachable()
        {
            var result = PerformHealthCheck(HealthCheckStrategy.PingOnly, HealthCheckLevel.Basic);
            return result.IsHealthy;
        }

        /// <summary>
        /// Detailed health check (cashier info dahil)
        /// </summary>
        public HealthCheckResult GetDetailedDeviceStatus()
        {
            return PerformHealthCheck(HealthCheckStrategy.PingFirst, HealthCheckLevel.Detailed);
        }
    }
}