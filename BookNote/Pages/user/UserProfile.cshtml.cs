using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BookNote.Pages.Users {
    public class UserProfileModel : PageModel {
        public bool UserExists { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public string UserPublicId { get; set; }
        public bool IsMyAccount { get; set; }

        public string ProfileDescription { get; set; }
        public int TotalReviewCount { get; set; }

        public byte[]? IconImageData { get; set; }

        public List<BookReview> RecentReviews { get; set; }
        public List<BookReview> HighRatedReviews { get; set; }
        public bool IsHighRatedReviewsPublic { get; set; }
        private readonly OracleConnection _conn;
        private readonly ILogger<UserProfileModel> _logger;
        private UserIconGetter _iconGetter;

        public UserProfileModel(ILogger<UserProfileModel> logger, OracleConnection conn) {
            _logger = logger;
            _iconGetter = new UserIconGetter();
            _conn = conn;
        }

        public async Task OnGetAsync(string userId) {
            var userPublicId = userId;
            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }
                var userGetter = new UserGetter(_conn);
                var user = await userGetter.GetUser(userPublicId);
                if (user == null) {
                    UserExists = false;
                    return;
                }
                UserExists = true;
                UserId = user.UserId;
                string? loginUserPublicId = await AccountDataGetter.GetDbUserPublicIdAsync(_conn);
                IsMyAccount = (userPublicId == loginUserPublicId);
                UserPublicId = userPublicId;
                UserName = user.UserName;
                ProfileDescription = user.UserProfile;
                TotalReviewCount = user.BookReviews.Count;
                IsHighRatedReviewsPublic = true; // 高評価リスト全体の公開設定
                RecentReviews = user.BookReviews.Take(5).ToList();
                HighRatedReviews = (await userGetter.GetUserGoodReviews(userPublicId)).Take(5).ToList();
                IconImageData = await _iconGetter.GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);
            } catch (Exception ex) {
                _logger?.LogError(ex.Message);
            }
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            Console.WriteLine(publicId);
            byte[]? imageData = await _iconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}