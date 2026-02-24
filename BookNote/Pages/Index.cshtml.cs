using BookNote.Scripts;
using BookNote.Scripts.BooksAPI.BookImage;
using BookNote.Scripts.BooksAPI.BookSearch;
using BookNote.Scripts.BooksAPI.BookSearch.Fetcher;
using BookNote.Scripts.BooksAPI.Moderation;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BookNote.Pages {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;
        private readonly OracleConnection _conn;
        private readonly BookImageController _bookImageController;
        private readonly UserIconGetter _userIconGetter;
        private readonly IConfiguration _config;

        public List<BookReview> PopularityReviews { get; set; }
        public List<BookReview> RecommentedReviews { get; set; }

        public IndexModel(ILogger<IndexModel> logger, OracleConnection conn, IConfiguration config) {
            _logger = logger;
            _conn = conn;
            _config = config;
            _bookImageController = new BookImageController(_config);
            _userIconGetter = new UserIconGetter(_config);

        }

        public async Task OnGetAsync() {
            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }
                var myid = AccountDataGetter.IsAuthenticated() ? AccountDataGetter.GetUserId() : null;
                PopularityBook pb = new PopularityBook(_conn, myid);
                RecommendedBook rb = new RecommendedBook(_conn, myid);
                PopularityReviews = await pb.GetReview(6);
                RecommentedReviews = await rb.GetReview(6);
            } catch (Exception ex) {
                _logger.LogError(ex, "オススメ取得エラー");
                PopularityReviews = [];
                RecommentedReviews = [];
                throw;
            }
        }

        public async Task<IActionResult> OnGetImageAsync(string isbn) {
            byte[]? imageData = await _bookImageController.GetBookImageData(isbn);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _userIconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}
