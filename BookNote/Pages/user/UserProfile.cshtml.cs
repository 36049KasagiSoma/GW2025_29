using BookNote.Scripts;
using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBookReview;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BookNote.Pages.Users {
    public class UserProfileModel : PageModel {
        public bool UserExists { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public string UserPublicId { get; set; }
        public string ProfileDescription { get; set; }
        public int TotalReviewCount { get; set; }

        public List<BookReview> RecentReviews { get; set; }
        public List<BookReview> HighRatedReviews { get; set; }
        public bool IsHighRatedReviewsPublic { get; set; }
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserProfileModel> _logger;
        private UserIconGetter _iconGetter;

        public UserProfileModel(ILogger<UserProfileModel> logger,IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
            _iconGetter = new UserIconGetter();
        }

        public async Task OnGetAsync(string userId) {
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
                    UserPublicId = user.UserPublicId;
                    UserName = user.UserName;
                    ProfileDescription = user.UserProfile;
                    TotalReviewCount = user.BookReviews.Count;
                    IsHighRatedReviewsPublic = true; // 高評価リスト全体の公開設定
                    RecentReviews = user.BookReviews.Take(5).ToList();
                    HighRatedReviews = (await userGetter.GetUserGoodReviews(userId)).Take(5).ToList();
                }
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

        public async Task<IActionResult> OnGetUserLargeIconAsync(string publicId) {
            Console.WriteLine(publicId);
            byte[]? imageData = await _iconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.LARGE);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg"); // または "image/png"
            }

            return NotFound();
        }
    }
}