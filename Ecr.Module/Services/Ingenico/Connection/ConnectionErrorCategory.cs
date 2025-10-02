namespace Ecr.Module.Services.Ingenico.Connection
{
    /// <summary>
    /// Error kategorileri - her kategori için farklı handling stratejisi
    /// </summary>
    public enum ConnectionErrorCategory
    {
        /// <summary>
        /// Başarılı işlem - error yok
        /// </summary>
        Success,

        /// <summary>
        /// Otomatik recovery yapılabilir - pairing, reconnection
        /// </summary>
        Recoverable,

        /// <summary>
        /// Kullanıcı aksiyonu gerekli - kağıt yok, kasiyer girişi
        /// </summary>
        UserActionRequired,

        /// <summary>
        /// Fatal hata - transaction iptal edilmeli
        /// </summary>
        Fatal,

        /// <summary>
        /// Timeout - retry yapılabilir
        /// </summary>
        Timeout,

        /// <summary>
        /// Bilinmeyen hata
        /// </summary>
        Unknown
    }
}