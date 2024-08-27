namespace ABCBook.Model.Support
{
    public abstract class CallApiService<T>
    {
        protected readonly T _callApiSupport;
        public CallApiService(T callApiSupport)
        {
            _callApiSupport = callApiSupport;

        }
    }
}
