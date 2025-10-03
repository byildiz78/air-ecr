namespace Ecr.Module.Models
{
    /// <summary>
    /// Request model for voiding a specific payment
    /// </summary>
    public class VoidPaymentRequest
    {
        /// <summary>
        /// Index of the payment to void (starts from 0)
        /// First payment = 0, Second payment = 1, etc.
        /// </summary>
        public ushort PaymentIndex { get; set; }
    }
}
