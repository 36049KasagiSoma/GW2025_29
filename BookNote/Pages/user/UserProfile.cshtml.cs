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
    [IgnoreAntiforgeryToken]
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

        // フォロー・ブロック状態
        public bool IsFollowing { get; set; }
        public bool IsBlocking { get; set; }

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
                IsHighRatedReviewsPublic = true;

                RecentReviews = user.BookReviews.Take(5).ToList();
                HighRatedReviews = (await userGetter.GetUserGoodReviews(userPublicId)).Take(5).ToList();
                IconImageData = await _iconGetter.GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);

                // フォロー・ブロック状態を取得
                if (!IsMyAccount && !string.IsNullOrEmpty(loginUserPublicId)) {
                    var loginUser = await userGetter.GetUser(loginUserPublicId);
                    if (loginUser != null) {
                        IsFollowing = await CheckFollowStatus(loginUser.UserId, UserId);
                        IsBlocking = await CheckBlockStatus(loginUser.UserId, UserId);
                    }
                }
            } catch (Exception ex) {
                _logger?.LogError(ex, "OnGetAsync error");
            }
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _iconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg");
            }
            return NotFound();
        }

        // フォロー切り替え
        public async Task<IActionResult> OnPostToggleFollowAsync([FromBody] FollowRequest request) {
            try {
                _logger?.LogInformation($"ToggleFollow called for: {request?.TargetUserPublicId}");

                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                var loginUserPublicId = await AccountDataGetter.GetDbUserPublicIdAsync(_conn);
                if (string.IsNullOrEmpty(loginUserPublicId)) {
                    return new JsonResult(new { success = false, message = "ログインが必要です" });
                }

                var userGetter = new UserGetter(_conn);
                var loginUser = await userGetter.GetUser(loginUserPublicId);
                var targetUser = await userGetter.GetUser(request.TargetUserPublicId);

                if (loginUser == null || targetUser == null) {
                    return new JsonResult(new { success = false, message = "ユーザが見つかりません" });
                }

                // ブロック中はフォローできない
                bool isBlocking = await CheckBlockStatus(loginUser.UserId, targetUser.UserId);
                if (isBlocking) {
                    return new JsonResult(new { success = false, message = "ブロック中のユーザーはフォローできません" });
                }

                bool isFollowing = await CheckFollowStatus(loginUser.UserId, targetUser.UserId);

                if (isFollowing) {
                    await UnfollowUser(loginUser.UserId, targetUser.UserId);
                    _logger?.LogInformation($"Unfollowed: {loginUser.UserId} -> {targetUser.UserId}");
                    return new JsonResult(new { success = true, isFollowing = false });
                } else {
                    await FollowUser(loginUser.UserId, targetUser.UserId);
                    _logger?.LogInformation($"Followed: {loginUser.UserId} -> {targetUser.UserId}");
                    return new JsonResult(new { success = true, isFollowing = true });
                }
            } catch (Exception ex) {
                _logger?.LogError(ex, "ToggleFollow error");
                return new JsonResult(new { success = false, message = $"エラーが発生しました: {ex.Message}" });
            }
        }

        // ブロック切り替え
        public async Task<IActionResult> OnPostToggleBlockAsync([FromBody] BlockRequest request) {
            try {
                _logger?.LogInformation($"ToggleBlock called for: {request?.TargetUserPublicId}");

                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                var loginUserPublicId = await AccountDataGetter.GetDbUserPublicIdAsync(_conn);
                if (string.IsNullOrEmpty(loginUserPublicId)) {
                    return new JsonResult(new { success = false, message = "ログインが必要です" });
                }

                var userGetter = new UserGetter(_conn);
                var loginUser = await userGetter.GetUser(loginUserPublicId);
                var targetUser = await userGetter.GetUser(request.TargetUserPublicId);

                if (loginUser == null || targetUser == null) {
                    return new JsonResult(new { success = false, message = "ユーザが見つかりません" });
                }

                bool isBlocking = await CheckBlockStatus(loginUser.UserId, targetUser.UserId);

                if (isBlocking) {
                    await UnblockUser(loginUser.UserId, targetUser.UserId);
                    _logger?.LogInformation($"Unblocked: {loginUser.UserId} -> {targetUser.UserId}");
                    return new JsonResult(new { success = true, isBlocking = false });
                } else {
                    await BlockUser(loginUser.UserId, targetUser.UserId);
                    await UnfollowUser(loginUser.UserId, targetUser.UserId);
                    _logger?.LogInformation($"Blocked: {loginUser.UserId} -> {targetUser.UserId}");
                    return new JsonResult(new { success = true, isBlocking = true });
                }
            } catch (Exception ex) {
                _logger?.LogError(ex, "ToggleBlock error");
                return new JsonResult(new { success = false, message = $"エラーが発生しました: {ex.Message}" });
            }
        }

        // フォロー状態確認（ログインユーザー → 対象ユーザー）
        private async Task<bool> CheckFollowStatus(string loginUserId, string targetUserId) {
            var sql = "SELECT COUNT(*) FROM UserFollow WHERE To_User_Id = :loginUserId AND For_User_Id = :targetUserId";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value = loginUserId;
            cmd.Parameters.Add(":targetUserId", OracleDbType.Char).Value = targetUserId;

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _logger?.LogInformation($"CheckFollowStatus: {loginUserId} -> {targetUserId} = {count > 0}");
            return count > 0;
        }

        // ブロック状態確認（ログインユーザー → 対象ユーザー）
        private async Task<bool> CheckBlockStatus(string loginUserId, string targetUserId) {
            var sql = "SELECT COUNT(*) FROM UserBlock WHERE To_User_Id = :loginUserId AND For_User_Id = :targetUserId";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value = loginUserId;
            cmd.Parameters.Add(":targetUserId", OracleDbType.Char).Value = targetUserId;

            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _logger?.LogInformation($"CheckBlockStatus: {loginUserId} -> {targetUserId} = {count > 0}");
            return count > 0;
        }

        // フォロー追加（ログインユーザー → 対象ユーザー）
        private async Task FollowUser(string loginUserId, string targetUserId) {
            var sql = "INSERT INTO UserFollow (To_User_Id, For_User_Id) VALUES (:loginUserId, :targetUserId)";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value = loginUserId;
            cmd.Parameters.Add(":targetUserId", OracleDbType.Char).Value = targetUserId;
            await cmd.ExecuteNonQueryAsync();
        }

        // フォロー解除（ログインユーザー → 対象ユーザー）
        private async Task UnfollowUser(string loginUserId, string targetUserId) {
            var sql = "DELETE FROM UserFollow WHERE To_User_Id = :loginUserId AND For_User_Id = :targetUserId";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value = loginUserId;
            cmd.Parameters.Add(":targetUserId", OracleDbType.Char).Value = targetUserId;
            await cmd.ExecuteNonQueryAsync();
        }

        // ブロック追加（ログインユーザー → 対象ユーザー）
        private async Task BlockUser(string loginUserId, string targetUserId) {
            var sql = "INSERT INTO UserBlock (To_User_Id, For_User_Id) VALUES (:loginUserId, :targetUserId)";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value = loginUserId;
            cmd.Parameters.Add(":targetUserId", OracleDbType.Char).Value = targetUserId;
            await cmd.ExecuteNonQueryAsync();
        }

        // ブロック解除（ログインユーザー → 対象ユーザー）
        private async Task UnblockUser(string loginUserId, string targetUserId) {
            var sql = "DELETE FROM UserBlock WHERE To_User_Id = :loginUserId AND For_User_Id = :targetUserId";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value = loginUserId;
            cmd.Parameters.Add(":targetUserId", OracleDbType.Char).Value = targetUserId;
            await cmd.ExecuteNonQueryAsync();
        }

        public class FollowRequest {
            public string TargetUserPublicId { get; set; }
        }

        public class BlockRequest {
            public string TargetUserPublicId { get; set; }
        }
    }
}