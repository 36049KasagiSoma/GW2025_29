using BookNote.Scripts;
using BookNote.Scripts.Models;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BookNote.Pages.Users {
    public class UserAllReviewsModel : PageModel {
        private const int PageSize = 20;

        public bool UserExists { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public bool IsHighRatedReviewsPublic { get; set; }

        public List<BookReview> AllReviews { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public List<BookReview> HighRatedReviews { get; set; }
        public int HighRatedCurrentPage { get; set; }
        public int HighRatedTotalPages { get; set; }

        public string CurrentTab { get; set; }
        private readonly ILogger<UserAllReviewsModel> _logger;
        private readonly IConfiguration _configuration;

        public UserAllReviewsModel(ILogger<UserAllReviewsModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        
        }

        public async Task OnGetAsync(string userId) {
            // クエリパラメータから直接取得
            int currentPage = 1;
            string currentTab = "all";

            if (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out int parsedPage)) {
                currentPage = parsedPage;
            }

            if (Request.Query.ContainsKey("tab")) {
                currentTab = Request.Query["tab"].ToString();
            }


            if (string.IsNullOrEmpty(userId)) {
                userId = Request.Query["userId"];
            }

            if (userId == null) {
                UserExists = false;
                return;
            }

            var allReviewsData = new List<BookReview>();
            var highRatedReviewsData = new List<BookReview>();

            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    var userGetter = new UserGetter(connection);
                    var user = await userGetter.GetUser(userId);
                    if (user == null) {
                        UserExists = false;
                        return;
                    }
                    UserExists = true;
                    UserId = userId;
                    UserName = user.UserName;
                    IsHighRatedReviewsPublic = true; // 高評価リスト全体の公開設定
                    allReviewsData.AddRange(user.BookReviews);
                    highRatedReviewsData.AddRange((await userGetter.GetUserGoodReviews(userId)).ToList());
                }
            } catch (Exception ex) {
                _logger?.LogError(ex.Message);
            }

            if (currentPage < 1) currentPage = 1;

            // すべてのレビュー用のページネーション
            var totalAll = allReviewsData.Count;
            TotalPages = (int)Math.Ceiling(totalAll / (double)PageSize);
            CurrentPage = currentTab == "all" ? currentPage : 1;

            if (CurrentPage > TotalPages && TotalPages > 0) {
                CurrentPage = TotalPages;
            }

            AllReviews = allReviewsData
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // 高評価レビュー用のページネーション
            if (IsHighRatedReviewsPublic) {
                var totalHighRated = highRatedReviewsData.Count;
                HighRatedTotalPages = (int)Math.Ceiling(totalHighRated / (double)PageSize);
                HighRatedCurrentPage = currentTab == "high-rated" ? currentPage : 1;

                if (HighRatedCurrentPage > HighRatedTotalPages && HighRatedTotalPages > 0) {
                    HighRatedCurrentPage = HighRatedTotalPages;
                }

                HighRatedReviews = highRatedReviewsData
                    .Skip((HighRatedCurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
        }

        private List<BookReview> GenerateAllReviewsData() {
            // 仮のデータ生成（実際はDBから取得）
            var reviews = new List<BookReview>();

            for (int i = 1; i <= 42; i++) {
                reviews.Add(new BookReview {
                    ReviewId = i,
                    Title = $"レビュータイトル {i}",
                    Book = new Book {
                        Title = $"書籍タイトル {i}",
                        Isbn = $"978{i:D10}",
                    },
                    Isbn = $"978{i:D10}",
                    Rating = i % 5 + 1,
                    IsSpoilers = i % 3 == 0 ? 1:0,
                    Review = $"これは{i}番目のレビューです。内容のプレビューがここに表示されます...",
                    PostingTime = DateTime.Now,
                });
            }

            return reviews;
        }
    }
}