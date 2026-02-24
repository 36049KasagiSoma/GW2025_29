using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Pages {
    public class RecommendModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecommendModel> _logger;

        // ===== バインドされるデータ =====
        /// <summary>セクション1: 最近の閲覧履歴からのおすすめ</summary>
        public List<BookReview> HistoryRecommends { get; set; } = new();

        /// <summary>セクション2: フォローユーザーの傾向からのおすすめ</summary>
        public List<BookReview> FollowRecommends { get; set; } = new();

        /// <summary>セクション3: グッド評価の傾向からのおすすめ</summary>
        public List<BookReview> GoodRecommends { get; set; } = new();

        /// <summary>セクション4: 自分のレビュー内容からのおすすめ</summary>
        public List<BookReview> MyReviewRecommends { get; set; } = new();

        public RecommendModel(ILogger<RecommendModel> logger, OracleConnection conn, IConfiguration configuration) {
            _logger = logger;
            _conn = conn;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync() {
            // 未ログインはリダイレクト
            if (!AccountDataGetter.IsAuthenticated()) {
                return RedirectToPage("/Login");
            }
            HistoryRecommends = [];
            FollowRecommends = [];
            GoodRecommends = [];
            MyReviewRecommends = [];
            var userId = AccountDataGetter.GetUserId();

            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                var selectSimilarReview = new SelectSimilarReview(_conn, userId);
                var selectRecommend = new RecommendedBook(_conn, userId);
                var myBookReview = new MyBookReview(_conn, userId);
                var allReviews = await selectSimilarReview.GetAllReviews(30 * 5); //5カ月分のレビューを取得

                HistoryRecommends.AddRange(await LoadHistoryRecommendsAsync(allReviews, selectSimilarReview, selectRecommend));
                allReviews = allReviews.Where(r => !(HistoryRecommends.Select(re => re.ReviewId).Contains(r.ReviewId))).ToList();
                FollowRecommends.AddRange(await LoadFollowRecommendsAsync(allReviews, selectSimilarReview, selectRecommend));
                allReviews = allReviews.Where(r => !(FollowRecommends.Select(re => re.ReviewId).Contains(r.ReviewId))).ToList();
                GoodRecommends.AddRange(await LoadGoodRecommendsAsync(allReviews, selectSimilarReview, selectRecommend));
                allReviews = allReviews.Where(r => !(GoodRecommends.Select(re => re.ReviewId).Contains(r.ReviewId))).ToList();
                MyReviewRecommends.AddRange(await LoadMyReviewRecommendsAsync(allReviews, selectSimilarReview, myBookReview));

            } catch (Exception ex) {
                _logger.LogError(ex, "おすすめデータ取得エラー");
            }

            return Page();
        }

        private async Task<List<BookReview>> LoadHistoryRecommendsAsync(List<BookReview> target, SelectSimilarReview selectSimilarReview, RecommendedBook recommendedBook) {
            var ids = await recommendedBook.GetViewedReviewIds();
            List<BookReview> rtn = [];
            rtn.AddRange(selectSimilarReview.SortBySimilarity(target, ids, 15));
            return rtn;
        }

        private async Task<List<BookReview>> LoadFollowRecommendsAsync(List<BookReview> target, SelectSimilarReview selectSimilarReview, RecommendedBook recommendedBook) {
            var ids = await recommendedBook.GetFollowedUsersReviewIds();
            List<BookReview> rtn = [];
            rtn.AddRange(selectSimilarReview.SortBySimilarity(target, ids, 5));
            return rtn;
        }

        private async Task<List<BookReview>> LoadGoodRecommendsAsync(List<BookReview> target, SelectSimilarReview selectSimilarReview, RecommendedBook recommendedBook) {
            var ids = await recommendedBook.GetGoodedReviewIds();
            List<BookReview> rtn = [];
            rtn.AddRange(selectSimilarReview.SortBySimilarity(target, ids, 8));
            return rtn;
        }

        private async Task<List<BookReview>> LoadMyReviewRecommendsAsync(List<BookReview> target, SelectSimilarReview selectSimilarReview, MyBookReview myBookReview) {
            var reviews = await myBookReview.GetReview(15);
            var ids = reviews.Select(r => r.ReviewId).ToList();
            List<BookReview> rtn = [];
            rtn.AddRange(selectSimilarReview.SortBySimilarity(target, ids, 5));
            return rtn;
        }
    }
}