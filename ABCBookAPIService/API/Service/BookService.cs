using ABCBook.Model.Support;

namespace ABCBookAPIService.API.Service
{
    public class BookService : IBookService
    {
        public async Task<APIResult<Guid>> Create(Guid id)
        {
            APIResult<Guid> result = new();
            return result;
        }
    }
}
