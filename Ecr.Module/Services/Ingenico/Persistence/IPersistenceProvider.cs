using System;

namespace Ecr.Module.Services.Ingenico.Persistence
{
    /// <summary>
    /// Persistence provider interface
    /// </summary>
    public interface IPersistenceProvider
    {
        /// <summary>
        /// State'i kaydet
        /// </summary>
        bool Save(PersistedState state);

        /// <summary>
        /// State'i yükle
        /// </summary>
        PersistedState Load();

        /// <summary>
        /// State var mı?
        /// </summary>
        bool Exists();

        /// <summary>
        /// State'i sil
        /// </summary>
        bool Clear();

        /// <summary>
        /// Son kaydedilme zamanı
        /// </summary>
        DateTime? GetLastSaveTime();
    }
}