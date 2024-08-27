using ABCBook.Model.Support;
using Microsoft.AspNetCore.Mvc;

namespace ABCBookShop.Controllers
{
    public class BookController : BaseWebController<BookController>
    {
        private readonly ABCBook.FEService.FE.IService.IBookService _bookService;

        public BookController(IHttpContextAccessor httpContextAccessor, ABCBook.FEService.FE.IService.IBookService bookService) : base(httpContextAccessor)
        {
            _bookService = bookService;
        }
        public async Task<IActionResult> Create()
        {
            ApiResultFE<Guid> result = await SetResponseAsync(() => _bookService.Create());
            return View();
        }
    }
}
