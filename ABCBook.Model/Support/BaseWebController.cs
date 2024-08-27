using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace ABCBook.Model.Support
{
    public abstract class BaseWebController<T> : Controller, IBaseResponse where T : BaseWebController<T>
    {
        protected ILogger<BaseWebController<T>> _logger;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected BaseWebController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        [NonAction]
        public async Task<ApiResultFE<TData>> SetResponseAsync<TData>(Func<Task<ApiResultFE<TData>>> func)
        {
            ApiResultFE<TData> result = new();
            try
            {
                result = await func();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return result;
        }
    }
}
