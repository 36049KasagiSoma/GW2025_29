using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase {
        private readonly IConfiguration _configuration;

        public ReviewsController(IConfiguration configuration) {
            _configuration = configuration;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] string sortOrder = "date") {
            var results = new List<BookReview>();
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    var myid = AccountDataGetter.IsAuthenticated() ? AccountDataGetter.GetUserId() : null;
                    results = await new SearchBooks(connection,myid).GetReview(keyword, 20, sortOrder);
                }
            } catch (Exception) {
            }

            return Ok(results);
        }
    }
}
