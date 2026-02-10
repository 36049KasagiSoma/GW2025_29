using BookNote.Scripts;
using BookNote.Scripts.Models;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BookNote.Pages.Users {
    public class UserAllReviewsModel : PageModel {
        private const int PageSize = 20;

        private readonly OracleConnection _conn;

        public bool UserExists { get; set; }
        public string UserName { get; set; }
        public string UserPublicId { get; set; }
        public bool IsHighRatedReviewsPublic { get; set; }

        public List<BookReview> AllReviews { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        public List<BookReview> HighRatedReviews { get; set; }
        public int HighRatedCurrentPage { get; set; }
        public int HighRatedTotalPages { get; set; }

        public byte[]? IconImageData { get; set; }

        private UserIconGetter _userIconGetter { get; set; }

        public string CurrentTab { get; set; }
        private readonly ILogger<UserAllReviewsModel> _logger;

        public UserAllReviewsModel(ILogger<UserAllReviewsModel> logger, OracleConnection conn) {
            _logger = logger;
            _userIconGetter = new UserIconGetter();
            _conn = conn;
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
                string? id = Request.Query["userId"];

                if (id == null) {
                    UserExists = false;
                    return;
                }
                userId = id;
            }


            var allReviewsData = new List<BookReview>();
            var highRatedReviewsData = new List<BookReview>();

            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }
                var userGetter = new UserGetter(_conn);
                var user = await userGetter.GetUser(userId);
                if (user == null) {
                    UserExists = false;
                    return;
                }
                UserExists = true;
                UserPublicId = userId;
                UserName = user.UserName;
                IsHighRatedReviewsPublic = true; // 高評価リスト全体の公開設定
                allReviewsData.AddRange(user.BookReviews);
                highRatedReviewsData.AddRange((await userGetter.GetUserGoodReviews(userId)).ToList());

                IconImageData = await _userIconGetter.GetIconImageData(user.UserPublicId, UserIconGetter.IconSize.LARGE);

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

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            Console.WriteLine(publicId);
            byte[]? imageData = await _userIconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}