using Ecr.Module.Services.Ingenico.Connection;
using Ecr.Module.Services.Ingenico.GmpIngenico;

namespace Ecr.Module.Services.Ingenico.Recovery
{
    /// <summary>
    /// Recovery plan oluşturan builder
    /// Error code'a göre uygun recovery stratejisi belirler
    /// </summary>
    public static class RecoveryPlanBuilder
    {
        /// <summary>
        /// Error code'a göre recovery plan oluştur
        /// </summary>
        public static RecoveryPlan BuildPlan(uint errorCode)
        {
            var errorInfo = ErrorCodeCategorizer.GetErrorInfo(errorCode);
            var plan = new RecoveryPlan();

            switch (errorInfo.Category)
            {
                case ConnectionErrorCategory.Success:
                    plan.Strategy = RecoveryStrategy.None;
                    plan.Description = "No recovery needed";
                    break;

                case ConnectionErrorCategory.Recoverable:
                    plan = BuildRecoverablePlan(errorCode, errorInfo);
                    break;

                case ConnectionErrorCategory.UserActionRequired:
                    plan = BuildUserActionPlan(errorCode, errorInfo);
                    break;

                case ConnectionErrorCategory.Timeout:
                    plan = BuildTimeoutPlan(errorCode, errorInfo);
                    break;

                case ConnectionErrorCategory.Fatal:
                    plan = BuildFatalPlan(errorCode, errorInfo);
                    break;

                default:
                    plan = BuildUnknownPlan(errorCode, errorInfo);
                    break;
            }

            return plan;
        }

        /// <summary>
        /// Recoverable error için plan
        /// </summary>
        private static RecoveryPlan BuildRecoverablePlan(uint errorCode, ConnectionErrorInfo errorInfo)
        {
            var plan = new RecoveryPlan
            {
                Strategy = RecoveryStrategy.AutoReconnect,
                Description = "Automatic reconnection attempt"
            };

            // Pairing gerekiyorsa
            if (errorInfo.RequiresPairing)
            {
                plan.Strategy = RecoveryStrategy.RequiresPairing;
                plan.AddAction(RecoveryActionType.Reselect_Interface, "Re-select valid interface", 1, true);
                plan.AddAction(RecoveryActionType.Perform_Pairing, "Perform pairing", 2, true);
                plan.AddAction(RecoveryActionType.Echo_Check, "Verify connection with ECHO", 3, false);
            }
            else
            {
                // Standart reconnection
                plan.AddAction(RecoveryActionType.Ping_Check, "Check device connectivity", 1, true);
                plan.AddAction(RecoveryActionType.Reselect_Interface, "Re-select interface if needed", 2, false);
                plan.AddAction(RecoveryActionType.Retry_Operation, "Retry failed operation", 3, true);
            }

            return plan;
        }

        /// <summary>
        /// User action required için plan
        /// </summary>
        private static RecoveryPlan BuildUserActionPlan(uint errorCode, ConnectionErrorInfo errorInfo)
        {
            var plan = new RecoveryPlan
            {
                Strategy = RecoveryStrategy.RequiresUserAction,
                Description = "User action required",
                RequiresUserAction = true,
                UserActionMessage = errorInfo.UserActionMessage
            };

            plan.AddAction(RecoveryActionType.Show_User_Message, errorInfo.UserActionMessage, 1, true);

            // Specific error handling
            switch (errorCode)
            {
                case Defines.TRAN_RESULT_NO_PAPER:
                case Defines.APP_ERR_APL_NO_PAPER:
                    plan.Description = "No paper - user must load paper";
                    plan.AddAction(RecoveryActionType.Ping_Check, "Wait and check device", 2, false);
                    break;

                case Defines.APP_ERR_CASHIER_ENTRY_REQUIRED:
                    plan.Description = "Cashier login required";
                    plan.AddAction(RecoveryActionType.Echo_Check, "Verify cashier login", 2, false);
                    break;

                case Defines.APP_ERR_DEVICE_CLOSED:
                    plan.Description = "Device is closed - user must turn on";
                    plan.AddAction(RecoveryActionType.Ping_Check, "Wait and check device", 2, false);
                    plan.AddAction(RecoveryActionType.Perform_Pairing, "Re-pair if needed", 3, false);
                    break;
            }

            return plan;
        }

        /// <summary>
        /// Timeout error için plan
        /// </summary>
        private static RecoveryPlan BuildTimeoutPlan(uint errorCode, ConnectionErrorInfo errorInfo)
        {
            var plan = new RecoveryPlan
            {
                Strategy = RecoveryStrategy.AutoReconnect,
                Description = "Timeout recovery"
            };

            plan.AddAction(RecoveryActionType.Ping_Check, "Quick connectivity check", 1, true);
            plan.AddAction(RecoveryActionType.Reselect_Interface, "Try different interface", 2, false);
            plan.AddAction(RecoveryActionType.Retry_Operation, "Retry with extended timeout", 3, true);

            return plan;
        }

        /// <summary>
        /// Fatal error için plan
        /// </summary>
        private static RecoveryPlan BuildFatalPlan(uint errorCode, ConnectionErrorInfo errorInfo)
        {
            var plan = new RecoveryPlan
            {
                Strategy = RecoveryStrategy.ManualIntervention,
                Description = "Fatal error - manual intervention required"
            };

            // Transaction handle errors
            if (errorCode == Defines.APP_ERR_GMP3_INVALID_HANDLE ||
                errorCode == Defines.APP_ERR_GMP3_NO_HANDLE)
            {
                plan.Strategy = RecoveryStrategy.TransactionRecovery;
                plan.AddAction(RecoveryActionType.Validate_Transaction, "Validate transaction state", 1, true);
                plan.AddAction(RecoveryActionType.Cancel_Transaction, "Cancel if invalid", 2, false);
            }
            else
            {
                plan.AddAction(RecoveryActionType.Reset_Connection, "Reset connection", 1, true);
                plan.AddAction(RecoveryActionType.Show_User_Message, $"Fatal error: {errorInfo.ErrorMessage}", 2, true);
            }

            return plan;
        }

        /// <summary>
        /// Unknown error için plan
        /// </summary>
        private static RecoveryPlan BuildUnknownPlan(uint errorCode, ConnectionErrorInfo errorInfo)
        {
            var plan = new RecoveryPlan
            {
                Strategy = RecoveryStrategy.ManualIntervention,
                Description = "Unknown error - conservative recovery"
            };

            plan.AddAction(RecoveryActionType.Ping_Check, "Check connectivity", 1, true);
            plan.AddAction(RecoveryActionType.Echo_Check, "Get detailed status", 2, false);
            plan.AddAction(RecoveryActionType.Show_User_Message, $"Unknown error: {errorInfo.ErrorMessage}", 3, true);

            return plan;
        }
    }
}