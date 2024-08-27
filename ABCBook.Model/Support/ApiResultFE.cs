namespace ABCBook.Model.Support
{
    public class ApiResultFE<TDataResult>
    {
        public APIResult<TDataResult?> apiResult { get; set; }
        public HttpResponseMessage httpResponseMessage { get; set; }
    }
}
