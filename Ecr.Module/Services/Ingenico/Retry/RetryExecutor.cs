using System;
using System.Threading;

namespace Ecr.Module.Services.Ingenico.Retry
{
    /// <summary>
    /// Retry logic executor
    /// Sonsuz döngü önleme ile retry mekanizması
    /// </summary>
    public static class RetryExecutor
    {
        /// <summary>
        /// Action'ı retry policy'e göre çalıştır
        /// </summary>
        public static RetryResult<T> Execute<T>(
            Func<T> action,
            Func<T, bool> isSuccess,
            RetryPolicy policy = null,
            Action<int, Exception> onRetry = null)
        {
            policy = policy ?? RetryPolicy.Default;
            int attemptCount = 0;
            Exception lastException = null;

            while (attemptCount < policy.MaxRetryCount)
            {
                attemptCount++;

                try
                {
                    T result = action();

                    // Success check
                    if (isSuccess(result))
                    {
                        return RetryResult<T>.SuccessResult(result, attemptCount);
                    }

                    // İlk attempt'de success değilse hemen retry
                    if (attemptCount >= policy.MaxRetryCount)
                    {
                        return RetryResult<T>.FailureResult(
                            new Exception($"Operation failed after {attemptCount} attempts"),
                            attemptCount,
                            "Maximum retry count reached"
                        );
                    }

                    // Delay
                    int delay = policy.GetDelayForAttempt(attemptCount);
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    onRetry?.Invoke(attemptCount, ex);

                    // Son attempt'de exception fırlat
                    if (attemptCount >= policy.MaxRetryCount)
                    {
                        return RetryResult<T>.FailureResult(ex, attemptCount);
                    }

                    // Delay
                    int delay = policy.GetDelayForAttempt(attemptCount);
                    Thread.Sleep(delay);
                }
            }

            return RetryResult<T>.FailureResult(
                lastException ?? new Exception("Maximum retry count reached"),
                attemptCount
            );
        }

        /// <summary>
        /// Void action'ı retry policy'e göre çalıştır
        /// </summary>
        public static RetryResult<bool> Execute(
            Action action,
            RetryPolicy policy = null,
            Action<int, Exception> onRetry = null)
        {
            return Execute(
                () =>
                {
                    action();
                    return true;
                },
                result => result,
                policy,
                onRetry
            );
        }

        /// <summary>
        /// Condition sağlanana kadar retry yap
        /// </summary>
        public static RetryResult<T> ExecuteUntil<T>(
            Func<T> action,
            Func<T, bool> condition,
            RetryPolicy policy = null,
            Action<int, T> onRetry = null)
        {
            policy = policy ?? RetryPolicy.Default;
            int attemptCount = 0;

            while (attemptCount < policy.MaxRetryCount)
            {
                attemptCount++;

                try
                {
                    T result = action();

                    if (condition(result))
                    {
                        return RetryResult<T>.SuccessResult(result, attemptCount);
                    }

                    onRetry?.Invoke(attemptCount, result);

                    if (attemptCount >= policy.MaxRetryCount)
                    {
                        return RetryResult<T>.FailureResult(
                            new Exception($"Condition not met after {attemptCount} attempts"),
                            attemptCount,
                            "Maximum retry count reached"
                        );
                    }

                    // Delay
                    int delay = policy.GetDelayForAttempt(attemptCount);
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    if (attemptCount >= policy.MaxRetryCount)
                    {
                        return RetryResult<T>.FailureResult(ex, attemptCount);
                    }

                    int delay = policy.GetDelayForAttempt(attemptCount);
                    Thread.Sleep(delay);
                }
            }

            return RetryResult<T>.FailureResult(
                new Exception("Maximum retry count reached"),
                attemptCount
            );
        }
    }
}