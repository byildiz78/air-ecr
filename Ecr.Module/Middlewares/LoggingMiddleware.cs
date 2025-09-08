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
using System.Runtime.InteropServices.ComTypes;
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
            // Request'i handle et
            var requestInfo = await CaptureRequest(context);

            // Response'u handle et
            var responseInfo = await CaptureResponse(context, async () => await Next.Invoke(context));

            // Log
            var correlationId = Guid.NewGuid().ToString();

            LogRequestResponse(context, requestInfo, responseInfo, correlationId);
        }

        private async Task<string> CaptureRequest(IOwinContext context)
        {
            var request = context.Request;

            if (request.Body == null || request.Body == Stream.Null)
                return "[No Request Body]";

            // Stream management
            Stream workingStream = request.Body;

            // Eğer seek edilemiyorsa, yeni stream oluştur
            if (!workingStream.CanSeek)
            {
                var memoryStream = new MemoryStream();
                await workingStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Önemli: Yeni stream'i context'e ata
                context.Request.Body = memoryStream;
                workingStream = memoryStream;
            }

            // Stream'den oku
            workingStream.Seek(0, SeekOrigin.Begin);

            string content;
            using (var reader = new StreamReader(workingStream, Encoding.UTF8, true, 1024, true))
            {
                content = await reader.ReadToEndAsync();
            }

            // Stream pozisyonunu sıfırla
            workingStream.Seek(0, SeekOrigin.Begin);

            // Context'e ekle
            context.Environment["RequestBody"] = content;

            return content;
        }

        private async Task<string> CaptureResponse(IOwinContext context, Func<Task> next)
        {
            var originalResponseStream = context.Response.Body;
            var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                // Middleware chain'i devam ettir
                await next();

                // Response'u oku
                responseBuffer.Seek(0, SeekOrigin.Begin);
                var responseContent = new StreamReader(responseBuffer).ReadToEnd();

                // Original stream'e kopyala
                responseBuffer.Seek(0, SeekOrigin.Begin);
                await responseBuffer.CopyToAsync(originalResponseStream);

                // Context'e ekle
                context.Environment["ResponseBody"] = responseContent;

                return responseContent;
            }
            finally
            {
                context.Response.Body = originalResponseStream;
                responseBuffer?.Dispose();
            }
        }


        private void LogRequestResponse(IOwinContext context, string requestBody, string responseBody, string correlationId)
        {
            _logger.Information(@"{correlationId} REQUEST:
Method: {@Method}
Path:   {@Path}
QueryString: {@QueryString}
{@requestBody}
",
correlationId,
context.Request.Method,
context.Request.Path.Value,
context.Request.QueryString.Value,
requestBody
);

            _logger.Information(@"{correlationId} RESPONSE:
StatusCode: {@StatusCode}
{@responseBody}
",
correlationId,
context.Response.StatusCode,
responseBody
);
        }


    }
}