namespace Ecr.Module.Services.Ingenico.Models
{

    public class IngenicoApiResponse
    {
        public bool Status { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class IngenicoApiResponse<T> where T : class
    {
        public bool Status { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
