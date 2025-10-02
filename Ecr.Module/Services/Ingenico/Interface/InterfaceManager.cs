using Ecr.Module.Services.Ingenico.GmpIngenico;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecr.Module.Services.Ingenico.Interface
{
    /// <summary>
    /// Interface yönetimi - seçim, validation, monitoring
    /// </summary>
    public class InterfaceManager
    {
        private static readonly object _lock = new object();
        private static InterfaceManager _instance;

        private InterfaceManager() { }

        public static InterfaceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new InterfaceManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Tüm interface'leri listele
        /// </summary>
        public List<InterfaceInfo> GetAllInterfaces()
        {
            var interfaces = new List<InterfaceInfo>();

            try
            {
                uint[] interfaceList = new uint[20];
                uint count = GMPSmartDLL.FP3_GetInterfaceHandleList(interfaceList, (uint)interfaceList.Length);

                for (uint i = 0; i < count; i++)
                {
                    var info = InterfaceValidator.GetInterfaceInfo(interfaceList[i]);
                    interfaces.Add(info);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllInterfaces error: {ex.Message}");
            }

            return interfaces;
        }

        /// <summary>
        /// Geçerli interface'leri listele
        /// </summary>
        public List<InterfaceInfo> GetValidInterfaces()
        {
            return GetAllInterfaces().Where(i => i.IsValid).ToList();
        }

        /// <summary>
        /// İlk geçerli interface'i seç
        /// </summary>
        public InterfaceInfo SelectFirstValidInterface()
        {
            var validInterfaces = GetValidInterfaces();

            if (validInterfaces.Count == 0)
            {
                return new InterfaceInfo
                {
                    IsValid = false,
                    ErrorMessage = "No valid interface found"
                };
            }

            return validInterfaces.First();
        }

        /// <summary>
        /// En iyi interface'i seç (PING ile test ederek)
        /// </summary>
        public InterfaceInfo SelectBestInterface()
        {
            var validInterfaces = GetValidInterfaces();

            if (validInterfaces.Count == 0)
            {
                return new InterfaceInfo
                {
                    IsValid = false,
                    ErrorMessage = "No valid interface found"
                };
            }

            // Her interface için PING test et
            foreach (var iface in validInterfaces)
            {
                if (InterfaceValidator.CanPing(iface.Handle))
                {
                    return iface;
                }
            }

            // Hiçbiri PING'e cevap vermiyorsa ilkini döndür
            return validInterfaces.First();
        }

        /// <summary>
        /// Interface değişti mi kontrol et
        /// </summary>
        public bool HasInterfaceChanged(uint currentHandle)
        {
            if (currentHandle == 0)
            {
                return true;
            }

            // Mevcut handle hala geçerli mi?
            if (!InterfaceValidator.IsValid(currentHandle))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Interface'i validate et ve gerekirse yeni seç
        /// </summary>
        public InterfaceInfo ValidateOrSelectNew(uint currentHandle)
        {
            // Mevcut handle geçerli mi kontrol et
            if (currentHandle > 0 && InterfaceValidator.IsValid(currentHandle))
            {
                var info = InterfaceValidator.GetInterfaceInfo(currentHandle);
                if (info.IsValid)
                {
                    return info;
                }
            }

            // Geçersizse yeni seç
            return SelectBestInterface();
        }
    }
}