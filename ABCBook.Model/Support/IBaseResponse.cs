namespace ABCBook.Model.Support
{
    public interface IBaseResponse
    {
        Task<ApiResultFE<TData>> SetResponseAsync<TData>(Func<Task<ApiResultFE<TData>>> func);
    }
}
