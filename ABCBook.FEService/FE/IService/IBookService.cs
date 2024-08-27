using ABCBook.Model.Support;

namespace ABCBook.FEService.FE.IService
{
    public interface IBookService
    {
        Task<ApiResultFE<Guid>> Create();
    }
}
