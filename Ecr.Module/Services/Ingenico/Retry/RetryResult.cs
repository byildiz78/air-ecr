using System;

namespace Ecr.Module.Services.Ingenico.Retry
{
    /// <summary>
    /// Retry işlemi sonucu
    /// </summary>
    public class RetryResult<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public int AttemptCount { get; set; }
        public Exception LastException { get; set; }
        public string ErrorMessage { get; set; }

        public RetryResult()
        {
            Success = false;
            AttemptCount = 0;
            ErrorMessage = string.Empty;
        }

        public static RetryResult<T> SuccessResult(T result, int attemptCount)
        {
            return new RetryResult<T>
            {
                Success = true,
                Result = result,
                AttemptCount = attemptCount
            };
        }

        public static RetryResult<T> FailureResult(Exception exception, int attemptCount, string errorMessage = "")
        {
            return new RetryResult<T>
            {
                Success = false,
                LastException = exception,
                AttemptCount = attemptCount,
                ErrorMessage = string.IsNullOrEmpty(errorMessage) ? exception?.Message : errorMessage
            };
        }
    }
}