using Ecr.Module.Services.Ingenico.GmpIngenico;
using System.Collections.Generic;

namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// Error code'ları kategorize eden ve detay bilgisi sağlayan sınıf
    /// PDF GMP3-Workshop.pdf Section 4: Error Codes referans alınmıştır
    /// </summary>
    public static class ErrorCodeCategorizer
    {
        private static readonly Dictionary<uint, ConnectionErrorCategory> ErrorCategoryMap = new Dictionary<uint, ConnectionErrorCategory>
        {
            // Success
            { Defines.TRAN_RESULT_OK, ConnectionErrorCategory.Success },

            // Recoverable - Automatic reconnection possible
            { Defines.DLL_RETCODE_PORT_NOT_OPEN, ConnectionErrorCategory.Recoverable },
            { Defines.DLL_RETCODE_TIMEOUT, ConnectionErrorCategory.Recoverable },
            { Defines.DLL_RETCODE_ACK_NOT_RECEIVED, ConnectionErrorCategory.Recoverable },
            { Defines.DLL_RETCODE_PAIRING_REQUIRED, ConnectionErrorCategory.Recoverable },
            { Defines.APP_ERR_GMP3_PAIRING_REQUIRED, ConnectionErrorCategory.Recoverable },
            { Defines.APP_ERR_GMP3_INVALID_SEQUENCE_NUMBER, ConnectionErrorCategory.Recoverable },
            { Defines.APP_ERR_GMP3_NACK, ConnectionErrorCategory.Recoverable },
            { Defines.APP_ERR_GMP3_ACK, ConnectionErrorCategory.Recoverable },

            // User Action Required
            { Defines.TRAN_RESULT_NO_PAPER, ConnectionErrorCategory.UserActionRequired },
            { Defines.APP_ERR_APL_NO_PAPER, ConnectionErrorCategory.UserActionRequired },
            { Defines.APP_ERR_CASHIER_ENTRY_REQUIRED, ConnectionErrorCategory.UserActionRequired },
            { Defines.APP_ERR_DEVICE_CLOSED, ConnectionErrorCategory.UserActionRequired },

            // Timeout
            { Defines.TRAN_RESULT_TIMEOUT, ConnectionErrorCategory.Timeout },
            { Defines.APP_ERR_GMP3_TIMEOUT, ConnectionErrorCategory.Timeout },
            { Defines.DLL_RETCODE_INTERCHAR_TIMEOUT, ConnectionErrorCategory.Timeout },

            // Fatal
            { Defines.APP_ERR_GMP3_INVALID_HANDLE, ConnectionErrorCategory.Fatal },
            { Defines.APP_ERR_GMP3_NO_HANDLE, ConnectionErrorCategory.Fatal },
            { Defines.APP_ERR_GMP3_PROTOCOL, ConnectionErrorCategory.Fatal },
            { Defines.APP_ERR_INVALID_PARAMETER_TAXINDEX, ConnectionErrorCategory.Fatal },
            { Defines.APP_ERR_INVALID_PARAMETER_TAXRATE, ConnectionErrorCategory.Fatal },
            { Defines.APP_ERR_MISSING_PARAMETER, ConnectionErrorCategory.Fatal }
        };

        /// <summary>
        /// Error code'u kategorize eder
        /// </summary>
        public static ConnectionErrorCategory GetCategory(uint errorCode)
        {
            if (ErrorCategoryMap.ContainsKey(errorCode))
            {
                return ErrorCategoryMap[errorCode];
            }
            return ConnectionErrorCategory.Unknown;
        }

        /// <summary>
        /// Error code'un detaylı bilgisini döner
        /// </summary>
        public static ConnectionErrorInfo GetErrorInfo(uint errorCode)
        {
            var info = new ConnectionErrorInfo
            {
                ErrorCode = errorCode,
                ErrorMessage = ErrorClass.DisplayErrorMessage(errorCode),
                ErrorCodeMessage = ErrorClass.DisplayErrorCodeMessage(errorCode),
                Category = GetCategory(errorCode)
            };

            // Category'ye göre flag'leri ayarla
            switch (info.Category)
            {
                case ConnectionErrorCategory.Recoverable:
                    info.RequiresReconnection = true;
                    info.RequiresPairing = IsErrorRequiresPairing(errorCode);
                    break;

                case ConnectionErrorCategory.UserActionRequired:
                    info.RequiresUserAction = true;
                    info.UserActionMessage = GetUserActionMessage(errorCode);
                    break;

                case ConnectionErrorCategory.Timeout:
                    info.RequiresReconnection = true;
                    break;
            }

            return info;
        }

        /// <summary>
        /// Error pairing gerektiriyor mu?
        /// </summary>
        private static bool IsErrorRequiresPairing(uint errorCode)
        {
            return errorCode == Defines.DLL_RETCODE_PAIRING_REQUIRED ||
                   errorCode == Defines.APP_ERR_GMP3_PAIRING_REQUIRED ||
                   errorCode == Defines.APP_ERR_GMP3_INVALID_SEQUENCE_NUMBER;
        }

        /// <summary>
        /// Kullanıcıya gösterilecek aksiyon mesajı
        /// </summary>
        private static string GetUserActionMessage(uint errorCode)
        {
            switch (errorCode)
            {
                case Defines.TRAN_RESULT_NO_PAPER:
                case Defines.APP_ERR_APL_NO_PAPER:
                    return "Yazarkasaya kağıt yükleyiniz ve tekrar deneyiniz.";

                case Defines.APP_ERR_CASHIER_ENTRY_REQUIRED:
                    return "Lütfen yazarkasadan kasiyer girişi yapınız.";

                case Defines.APP_ERR_DEVICE_CLOSED:
                    return "Yazarkasa kapalı. Lütfen cihazı açınız.";

                default:
                    return "Lütfen hata mesajını kontrol ediniz ve gerekli aksiyonu alınız.";
            }
        }

        /// <summary>
        /// Error success mu?
        /// </summary>
        public static bool IsSuccess(uint errorCode)
        {
            return errorCode == Defines.TRAN_RESULT_OK;
        }

        /// <summary>
        /// Error recoverable mı?
        /// </summary>
        public static bool IsRecoverable(uint errorCode)
        {
            var category = GetCategory(errorCode);
            return category == ConnectionErrorCategory.Recoverable ||
                   category == ConnectionErrorCategory.Timeout;
        }
    }
}