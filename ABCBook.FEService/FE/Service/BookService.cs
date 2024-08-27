using ABCBook.FEService.FE.IService;
using ABCBook.Model.Support;

namespace ABCBook.FEService.FE.Service
{
    public class BookService : CallApiService<BaseCallApiSupport>, IBookService
    {
        public BookService(BaseCallApiSupport callApiSupport) : base(callApiSupport)
        {
        }

        public async Task<ApiResultFE<Guid>> Create()
        {
            Task<ApiResultFE<Guid>> result = _callApiSupport.API<Guid>("", null, HttpMethod.Get);
            return result.Result;
        }
    }
}
