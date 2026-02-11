using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static BookNote.Pages.ReviewDetailsModel;

namespace BookNote.Pages {
    public class ReviewsModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly UserIconGetter _userIconGetter;

        public List<BookReview> LatestReviews { get; set; } = new();
        public List<BookReview> PopularReviews { get; set; } = new();
        public List<BookReview> FollowingReviews { get; set; } = new();

        private readonly ILogger<ReviewsModel> _logger;

        public ReviewsModel(ILogger<ReviewsModel> logger, OracleConnection conn) {
            _logger = logger;
            _userIconGetter = new UserIconGetter();
            _conn = conn;
        }

        public async Task OnGetAsync() {
            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }
                var myid = AccountDataGetter.IsAuthenticated() ? AccountDataGetter.GetUserId() : null;
                LatestReviews = await new LatestBook(_conn, myid).GetReview(20);
                PopularReviews = await new PopularityBook(_conn, myid).GetReview();
                if (AccountDataGetter.IsAuthenticated()) {
                    FollowingReviews = await new FollowingUserBook(_conn).GetReview(AccountDataGetter.GetUserId());
                } else {
                    FollowingReviews = [];
                }
            } catch (Exception ex) {
                _logger.LogInformation(ex, "オススメ取得エラー");
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
