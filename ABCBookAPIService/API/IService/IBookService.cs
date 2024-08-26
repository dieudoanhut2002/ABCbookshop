namespace ABCBook.Model.Support
{
    public interface IBookService
    {
        Task<APIResult<Guid>> Create(Guid id);
    }
}
