using BookNote.Scripts.BooksAPI.BookSearch;
using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;
using BookNote.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace BookNote.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase {

        // セッションIDごとに進捗を管理
        private static readonly ConcurrentDictionary<string, float> _progressMap = new();

        private string GetSessionId() {
            const string key = "SearchSessionId";
            var id = HttpContext.Session.GetString(key);
            if (string.IsNullOrEmpty(id)) {
                id = Guid.NewGuid().ToString();
                HttpContext.Session.SetString(key, id);
            }
            return id;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query) {
            if (string.IsNullOrWhiteSpace(query)) {
                return Ok(new List<object>());
            }

            var sessionId = GetSessionId();
            _progressMap[sessionId] = 0f;

            var controller = new BookSearchController();
            controller.OnProgressChanged += p => { _progressMap[sessionId] = p; };

            List<BookSearchResult> result = await controller.GetSearchBooks(query);

            _progressMap[sessionId] = 1f;
            await Task.Delay(500);
            return Ok(result.ToArray());
        }


        [HttpGet("search-progress")]
        public IActionResult GetProgress([FromQuery] bool reset = false) {
            var sessionId = GetSessionId();
            if (reset) _progressMap[sessionId] = 0f;
            var progress = _progressMap.TryGetValue(sessionId, out var p) ? p : 0f;
            return Ok(new { progress });
        }
    }
}