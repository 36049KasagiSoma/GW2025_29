using BookNote.Scripts;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using static BookNote.Pages.ReviewDetailsModel;

namespace BookNote.Pages {
    public class ReviewsModel : PageModel {
        IConfiguration _configuration;

        public List<BookReview> LatestReviews { get; set; } = new();
        public List<BookReview> PopularReviews { get; set; } = new();
        public List<BookReview> FollowingReviews { get; set; } = new();

        private readonly ILogger<ReviewsModel> _logger;

        public ReviewsModel(ILogger<ReviewsModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task OnGetAsync() {
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    LatestReviews = await new LatestBook(connection).GetReview(20);
                    PopularReviews = await new PopularityBook(connection).GetReview();
                    // TODO ユーザId取得処理
                    FollowingReviews = await new FollowingUserBook(connection).GetReview("550e8400-e29b-41d4-a716-446655440000");
                }
            } catch (Exception ex) {
                _logger.LogInformation(ex,"オススメ取得エラー");
                LatestReviews = [];
                PopularReviews = [];
                FollowingReviews = [];
            }
        }
    }
}
