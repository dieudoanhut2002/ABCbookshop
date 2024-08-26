using ABCBook.Model.Support;
using Microsoft.AspNetCore.Mvc;

namespace ABCBook.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BookController : Controller
    {
        private readonly IBookService _bookService;
        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }
        [HttpGet]
        public Task<APIResult<Guid>> Create()
        {
            return _bookService.Create(Guid.NewGuid());
        }
    }
}
