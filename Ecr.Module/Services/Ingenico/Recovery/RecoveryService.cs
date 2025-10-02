using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.HealthCheck;
using Ecr.Module.Services.Ingenico.Interface;
using Ecr.Module.Services.Ingenico.Pairing;
using Ecr.Module.Services.Ingenico.Transaction;
using System;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Recovery
{
    /// <summary>
    /// Error recovery service
    /// Comprehensive error handling ve automatic recovery
    /// </summary>
    public class RecoveryService
    {
        private readonly ConnectionManager _connectionManager;
        private readonly InterfaceManager _interfaceManager;
        private readonly TransactionManager _transactionManager;
        private readonly HealthCheckService _healthCheckService;

        public RecoveryService()
        {
            _connectionManager = ConnectionManager.Instance;
            _interfaceManager = InterfaceManager.Instance;
            _transactionManager = TransactionManager.Instance;
            _healthCheckService = new HealthCheckService();
        }

        /// <summary>
        /// Error'dan recovery yap
        /// </summary>
        public RecoveryResult Recover(uint errorCode)
        {
            var result = new RecoveryResult
            {
                ErrorCode = errorCode,
                StartTime = DateTime.Now
            };

            try
            {
                // Recovery plan oluştur
                var plan = RecoveryPlanBuilder.BuildPlan(errorCode);
                result.Plan = plan;
                result.Strategy = plan.Strategy;

                // Plan'ı execute et
                result = ExecutePlan(plan, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Recovery exception: {ex.Message}";
            }

            result.EndTime = DateTime.Now;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;

            return result;
        }

        /// <summary>
        /// Recovery plan'ı execute et
        /// </summary>
        private RecoveryResult ExecutePlan(RecoveryPlan plan, RecoveryResult result)
        {
            // User action gerekiyorsa, mesajı kullanıcıya göster ve bekle
            if (plan.RequiresUserAction)
            {
                result.RequiresUserAction = true;
                result.UserActionMessage = plan.UserActionMessage;
                // Real implementation'da burada kullanıcıya mesaj gösterilir
                // ve kullanıcının aksiyonu beklenir
            }

            // Her action'ı sırayla execute et
            foreach (var action in plan.Actions)
            {
                var actionResult = ExecuteAction(action);
                result.ExecutedActions.Add(actionResult);

                // Required action başarısızsa recovery başarısız
                if (action.IsRequired && !actionResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Required action failed: {action.Description}";
                    return result;
                }

                // Başarılı ise devam et
                if (actionResult.Success)
                {
                    result.SuccessfulActionCount++;
                }
            }

            // Tüm action'lar tamamlandı
            result.Success = result.SuccessfulActionCount > 0;
            result.ErrorMessage = result.Success ? "Recovery completed successfully" : "Recovery failed";

            return result;
        }

        /// <summary>
        /// Single action execute et
        /// </summary>
        private RecoveryActionResult ExecuteAction(RecoveryAction action)
        {
            var result = new RecoveryActionResult
            {
                ActionType = action.ActionType,
                Description = action.Description,
                StartTime = DateTime.Now
            };

            try
            {
                switch (action.ActionType)
                {
                    case RecoveryActionType.Ping_Check:
                        result.Success = ExecutePingCheck();
                        break;

                    case RecoveryActionType.Echo_Check:
                        result.Success = ExecuteEchoCheck();
                        break;

                    case RecoveryActionType.Reselect_Interface:
                        result.Success = ExecuteReselectInterface();
                        break;

                    case RecoveryActionType.Perform_Pairing:
                        result.Success = ExecutePairing();
                        break;

                    case RecoveryActionType.Validate_Transaction:
                        result.Success = ExecuteValidateTransaction();
                        break;

                    case RecoveryActionType.Cancel_Transaction:
                        result.Success = ExecuteCancelTransaction();
                        break;

                    case RecoveryActionType.Reset_Connection:
                        result.Success = ExecuteResetConnection();
                        break;

                    case RecoveryActionType.Show_User_Message:
                        result.Success = true; // User message handled by caller
                        break;

                    case RecoveryActionType.Retry_Operation:
                        result.Success = true; // Retry handled by caller
                        break;

                    default:
                        result.Success = false;
                        result.ErrorMessage = "Unknown action type";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Action exception: {ex.Message}";
            }

            result.EndTime = DateTime.Now;
            result.DurationMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;

            return result;
        }

        #region Action Implementations

        private bool ExecutePingCheck()
        {
            var healthCheck = _healthCheckService.PerformHealthCheck(
                HealthCheckStrategy.PingOnly,
                HealthCheckLevel.Basic
            );
            return healthCheck.IsHealthy;
        }

        private bool ExecuteEchoCheck()
        {
            var healthCheck = _healthCheckService.PerformHealthCheck(
                HealthCheckStrategy.EchoOnly,
                HealthCheckLevel.Detailed
            );
            return healthCheck.IsHealthy;
        }

        private bool ExecuteReselectInterface()
        {
            var interfaceInfo = _interfaceManager.SelectBestInterface();
            if (interfaceInfo.IsValid)
            {
                _connectionManager.SetInterface(interfaceInfo.Handle);
                return true;
            }
            return false;
        }

        private bool ExecutePairing()
        {
            try
            {
                var pairingProvider = new PairingGmpProviderV2();
                var result = pairingProvider.GmpPairing();
                return result.ReturnCode == GmpIngenico.Defines.TRAN_RESULT_OK;
            }
            catch
            {
                return false;
            }
        }

        private bool ExecuteValidateTransaction()
        {
            var validation = _transactionManager.ValidateCurrentTransaction();
            return validation.IsValid;
        }

        private bool ExecuteCancelTransaction()
        {
            _transactionManager.CancelTransaction();
            return true;
        }

        private bool ExecuteResetConnection()
        {
            _connectionManager.Reset();
            _transactionManager.ResetTransaction();
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Recovery sonucu
    /// </summary>
    public class RecoveryResult
    {
        public bool Success { get; set; }
        public uint ErrorCode { get; set; }
        public RecoveryStrategy Strategy { get; set; }
        public RecoveryPlan Plan { get; set; }
        public string ErrorMessage { get; set; }
        public List<RecoveryActionResult> ExecutedActions { get; set; }
        public int SuccessfulActionCount { get; set; }
        public bool RequiresUserAction { get; set; }
        public string UserActionMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long DurationMs { get; set; }

        public RecoveryResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
            ExecutedActions = new List<RecoveryActionResult>();
            SuccessfulActionCount = 0;
            RequiresUserAction = false;
            UserActionMessage = string.Empty;
        }
    }

    /// <summary>
    /// Recovery action sonucu
    /// </summary>
    public class RecoveryActionResult
    {
        public RecoveryActionType ActionType { get; set; }
        public string Description { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long DurationMs { get; set; }

        public RecoveryActionResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
        }
    }
}