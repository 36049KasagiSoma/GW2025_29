using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using static BookNote.Pages.ReviewDetailsModel;

namespace BookNote.Pages {
    public class ReviewsModel : PageModel {
        IConfiguration _configuration;
        UserIconGetter _userIconGetter;

        public List<BookReview> LatestReviews { get; set; } = new();
        public List<BookReview> PopularReviews { get; set; } = new();
        public List<BookReview> FollowingReviews { get; set; } = new();

        private readonly ILogger<ReviewsModel> _logger;

        public ReviewsModel(ILogger<ReviewsModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
            _userIconGetter = new UserIconGetter();
        }

        public async Task OnGetAsync() {
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    LatestReviews = await new LatestBook(connection).GetReview(20);
                    PopularReviews = await new PopularityBook(connection).GetReview();
                    UserDataManager userDataManager = new UserDataManager();
                    FollowingReviews = await new FollowingUserBook(connection).GetReview(userDataManager.GetUserId());
                }
            } catch (Exception ex) {
                _logger.LogInformation(ex,"オススメ取得エラー");
                LatestReviews = [];
                PopularReviews = [];
                FollowingReviews = [];
            }
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _userIconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.LARGE);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}
