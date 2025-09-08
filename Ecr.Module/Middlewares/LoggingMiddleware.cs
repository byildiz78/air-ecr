using Ecr.Module.Statics;
using Microsoft.Owin;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecr.Module.Middlewares
{
    public class LoggingMiddleware : OwinMiddleware
    {
        private static ILogger _logger;

        public LoggingMiddleware(OwinMiddleware next) : base(next)
        {
            _logger = AppStatics.GetLogger("EcrReqResLog");
        }

        public override async Task Invoke(IOwinContext context)
        {
            // Correlation ID ekle
            var correlationId = Guid.NewGuid().ToString("N");
            context.Set("CorrelationId", correlationId);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                // Request logging
                await LogRequest(context, correlationId);

                // Zamanlayıcı başlat
                var stopwatch = Stopwatch.StartNew();

                // Response'u yakalamak için stream'i değiştir
                var originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    try
                    {
                        // Sonraki middleware'e devam et
                        await Next.Invoke(context);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
                        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
                        {
                            _logger.Error(ex, "Request processing failed. Path: {Path}, Method: {Method}, ElapsedMs: {ElapsedMs}",
                                context.Request.Path, context.Request.Method, stopwatch.ElapsedMilliseconds);
                        }
                        throw;
                    }

                    stopwatch.Stop();

                    // Response logging
                    await LogResponse(context, responseBody, stopwatch.ElapsedMilliseconds, correlationId);

                    // Original stream'e geri yaz
                    responseBody.Position = 0;
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
        }

        private async Task LogRequest(IOwinContext context, string correlationId)
        {
            var request = context.Request;
            var requestBody = "";

            // Request body'yi oku (eğer varsa)
            if (request.Body != null && request.Body.Length > 0)
            {
                // Stream'i okuyabilmek için enable buffering
                var stream = request.Body;
                if (stream.CanSeek)
                {
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
                    {
                        requestBody = await reader.ReadToEndAsync();
                        stream.Position = 0;
                    }
                }
            }

            // Structured logging kullan
            var requestLog = new
            {
                CorrelationId = correlationId,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Scheme = request.Scheme,
                Host = request.Host.Value,
                RemoteIpAddress = request.RemoteIpAddress,
                UserAgent = request.Headers.Get("User-Agent"),
                ContentType = request.ContentType,
                ContentLength = request.Body.Length,
                Headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Body = SanitizeBody(requestBody, request.ContentType)
            };

            // Log level'ı path'e göre belirle
            var logLevel = DetermineLogLevel(request.Path.Value);

            if (logLevel == LogEventLevel.Debug)
            {
                _logger.Debug("Request received {@Request}", requestLog);
            }
            else
            {
                _logger.Information("Request received {@Request}", requestLog);
            }
        }

        private async Task LogResponse(IOwinContext context, MemoryStream responseBody, long elapsedMs, string correlationId)
        {
            var response = context.Response;
            responseBody.Position = 0;
            var responseBodyText = "";

            // Response body'yi oku
            if (responseBody.Length > 0)
            {
                using (var reader = new StreamReader(responseBody, Encoding.UTF8, true, 1024, true))
                {
                    responseBodyText = await reader.ReadToEndAsync();
                    responseBody.Position = 0;
                }
            }

            var responseLog = new
            {
                CorrelationId = correlationId,
                StatusCode = response.StatusCode,
                ReasonPhrase = response.ReasonPhrase,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength ?? responseBody.Length,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                Body = TruncateBody(responseBodyText, 2000), // Max 2000 karakter
                ElapsedMilliseconds = elapsedMs
            };

            // Status code'a göre log level belirle
            if (response.StatusCode >= 500)
            {
                _logger.Error("Response {@Response}", responseLog);
            }
            else if (response.StatusCode >= 400)
            {
                _logger.Warning("Response {@Response}", responseLog);
            }
            else if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                _logger.Information("Response {@Response}", responseLog);
            }
            else
            {
                _logger.Debug("Response {@Response}", responseLog);
            }

            // Performans uyarısı
            if (elapsedMs > 1000)
            {
                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    _logger.Warning("{CorrelationId} Slow API response. Path: {Path}, ElapsedMs: {ElapsedMs}",
                        correlationId, context.Request.Path, elapsedMs);
                }
            }
        }

        private string SanitizeBody(string body, string contentType)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            // Hassas bilgileri maskele
            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.Contains("application/json"))
                {
                    // JSON içindeki hassas alanları maskele
                    // Örnek: password, token, secret gibi alanlar
                    body = MaskSensitiveData(body);
                }
            }

            return TruncateBody(body, 5000);
        }

        private string MaskSensitiveData(string json)
        {
            // Basit bir maskeleme - gerçek uygulamada daha gelişmiş olmalı
            var sensitiveFields = new[] { "password", "token", "secret", "apikey", "authorization" };

            foreach (var field in sensitiveFields)
            {
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
                json = System.Text.RegularExpressions.Regex.Replace(
                    json,
                    pattern,
                    $"\"{field}\":\"***MASKED***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return json;
        }

        private string TruncateBody(string body, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            if (body.Length <= maxLength)
                return body;

            return body.Substring(0, maxLength) + "... (truncated)";
        }

        private LogEventLevel DetermineLogLevel(string path)
        {
            // Health check endpoint'leri için debug level kullan
            if (path != null && (path.Contains("/health") || path.Contains("/ping")))
            {
                return LogEventLevel.Debug;
            }

            return LogEventLevel.Information;
        }
    }
}