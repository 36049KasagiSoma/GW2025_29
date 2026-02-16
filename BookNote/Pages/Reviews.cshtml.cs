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
        private readonly IConfiguration _configuration;

        public List<BookReview> LatestReviews { get; set; } = new();
        public List<BookReview> PopularReviews { get; set; } = new();
        public List<BookReview> FollowingReviews { get; set; } = new();
        public List<BookReview> MyReviews { get; set; } = new();


        private readonly ILogger<ReviewsModel> _logger;

        public ReviewsModel(ILogger<ReviewsModel> logger, OracleConnection conn, IConfiguration configuration) {
            _logger = logger;
            _conn = conn;
            _configuration = configuration;
            _userIconGetter = new UserIconGetter(_configuration);
        }

        public async Task OnGetAsync() {
            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }
                var myid = AccountDataGetter.IsAuthenticated() ? AccountDataGetter.GetUserId() : null;
                LatestReviews.AddRange(await new LatestBook(_conn, myid).GetReview(20));
                PopularReviews.AddRange(await new PopularityBook(_conn, myid).GetReview(20));
                if (AccountDataGetter.IsAuthenticated() && myid != null) {
                    MyReviews.AddRange(await new MyBookReview(_conn).GetReview(myid, 20));
                }
                if (AccountDataGetter.IsAuthenticated()) {
                    FollowingReviews.AddRange(await new FollowingUserBook(_conn).GetReview(AccountDataGetter.GetUserId()));
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
