using BookNote.Scripts;
using BookNote.Scripts.BooksAPI.BookImage;
using BookNote.Scripts.BooksAPI.BookSearch;
using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BookNote.Pages {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly BookImageController _bookImageController;

        public List<BookReview> PopularityReviews { get; set; }
        public List<BookReview> RecommentedReviews { get; set; }



        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
            _bookImageController = new BookImageController();
        }

        public async Task OnGetAsync() {
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    PopularityBook pb = new PopularityBook(connection);
                    RecommentedBook rb = new RecommentedBook(connection);
                    PopularityReviews = await pb.GetReview();
                    RecommentedReviews = await rb.GetReview();
                }
            } catch (Exception ex) {
                _logger.LogError(ex,"オススメ取得エラー");
                PopularityReviews = [];
                RecommentedReviews = [];
            }
        }

        public async Task<IActionResult> OnGetImageAsync(string isbn) {
            byte[]? imageData = await _bookImageController.GetBookImageData(isbn);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}
