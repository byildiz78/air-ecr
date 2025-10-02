using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;

namespace Ecr.Module.Services.Ingenico.Interface
{
    /// <summary>
    /// Interface validation işlemleri
    /// </summary>
    public static class InterfaceValidator
    {
        /// <summary>
        /// Interface handle geçerli mi kontrol et
        /// </summary>
        public static bool IsValid(uint interfaceHandle)
        {
            if (interfaceHandle == 0)
            {
                return false;
            }

            try
            {
                byte[] id = new byte[64];
                uint result = GMPSmartDLL.FP3_GetInterfaceID(interfaceHandle, id, (uint)id.Length);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Interface bilgilerini al
        /// </summary>
        public static InterfaceInfo GetInterfaceInfo(uint interfaceHandle)
        {
            var info = new InterfaceInfo
            {
                Handle = interfaceHandle
            };

            if (interfaceHandle == 0)
            {
                info.IsValid = false;
                info.ErrorMessage = "Interface handle is 0";
                return info;
            }

            try
            {
                byte[] id = new byte[64];
                uint result = GMPSmartDLL.FP3_GetInterfaceID(interfaceHandle, id, (uint)id.Length);

                if (result == 0)
                {
                    info.InterfaceId = GMP_Tools.SetEncoding(id);
                    info.IsValid = true;
                }
                else
                {
                    info.IsValid = false;
                    info.ErrorMessage = $"FP3_GetInterfaceID failed with code: {result}";
                }
            }
            catch (Exception ex)
            {
                info.IsValid = false;
                info.ErrorMessage = $"Exception: {ex.Message}";
            }

            return info;
        }

        /// <summary>
        /// Interface'in PING ile erişilebilir olduğunu kontrol et
        /// </summary>
        public static bool CanPing(uint interfaceHandle, int timeout = 1100)
        {
            if (!IsValid(interfaceHandle))
            {
                return false;
            }

            try
            {
                uint result = GMPSmartDLL.FP3_Ping(interfaceHandle, timeout);
                return result == Defines.TRAN_RESULT_OK;
            }
            catch
            {
                return false;
            }
        }
    }
}