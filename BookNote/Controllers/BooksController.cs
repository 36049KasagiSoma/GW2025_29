using BookNote.Scripts.BooksAPI.BookSearch;
using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;
using BookNote.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookNote.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase {
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query) {
            if (string.IsNullOrWhiteSpace(query)) {
                return Ok(new List<object>());
            }

            List<BookSearchResult> result = await new BookSearchController().GetSearchBooks(query);

            return Ok(result.ToArray());
        }
    }
}
