using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Pages.user {

    public class PrivacyUserInfo {
        public string UserId { get; set; } = "";
        public string UserPublicId { get; set; } = "";
        public string UserName { get; set; } = "";
    }

    public class MutedBookInfo {
        public string ISBN { get; set; } = "";
        public string Title { get; set; } = "";
    }

    public class PrivacyModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly IConfiguration _configuration;
        private UserIconGetter _iconGetter;

        public PrivacyModel(OracleConnection conn, IConfiguration configuration) {
            _conn = conn;
            _configuration = configuration;
            _iconGetter = new UserIconGetter(_configuration);
        }

        public List<PrivacyUserInfo> FollowingUsers { get; set; } = new();
        public List<PrivacyUserInfo> BlockedUsers { get; set; } = new();
        public List<MutedBookInfo> MutedBooks { get; set; } = new();
        public string? Message { get; set; }
        public bool IsError { get; set; }

        private async Task EnsureOpenAsync() {
            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();
        }

        public async Task<IActionResult> OnGetAsync() {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            await EnsureOpenAsync();
            var userId = AccountDataGetter.GetUserId();
            await LoadDataAsync(userId);
            return Page();
        }

        private async Task LoadDataAsync(string userId) {
            // フォロー一覧（自分がフォローしている相手）
            // UserFollow: To_User_Id = 自分, For_User_Id = フォロー先
            var followQuery = @"
                SELECT u.User_Id, u.User_PublicId, u.User_Name
                FROM UserFollow f
                JOIN Users u ON u.User_Id = f.For_User_Id
                WHERE f.To_User_Id = :UserId
                ORDER BY u.User_Name
            ";
            using (var cmd = new OracleCommand(followQuery, _conn)) {
                cmd.Parameters.Add(new OracleParameter("UserId", userId));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) {
                    FollowingUsers.Add(new PrivacyUserInfo {
                        UserId = reader["User_Id"] as string ?? "",
                        UserPublicId = reader["User_PublicId"] as string ?? "",
                        UserName = reader["User_Name"] as string ?? ""
                    });
                }
            }

            // ブロック一覧
            var blockQuery = @"
                SELECT u.User_Id, u.User_PublicId, u.User_Name
                FROM UserBlock b
                JOIN Users u ON u.User_Id = b.For_User_Id
                WHERE b.To_User_Id = :UserId
                ORDER BY u.User_Name
            ";
            using (var cmd = new OracleCommand(blockQuery, _conn)) {
                cmd.Parameters.Add(new OracleParameter("UserId", userId));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) {
                    BlockedUsers.Add(new PrivacyUserInfo {
                        UserId = reader["User_Id"] as string ?? "",
                        UserPublicId = reader["User_PublicId"] as string ?? "",
                        UserName = reader["User_Name"] as string ?? ""
                    });
                }
            }

            // ミュート書籍一覧
            var muteQuery = @"
                SELECT m.ISBN
                FROM BookMute m
                WHERE m.User_Id = :UserId
                ORDER BY m.ISBN
            ";
            using (var cmd = new OracleCommand(muteQuery, _conn)) {
                cmd.Parameters.Add(new OracleParameter("UserId", userId));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) {
                    MutedBooks.Add(new MutedBookInfo {
                        ISBN = reader["ISBN"] as string ?? "",
                    });
                }
            }
        }

        // フォロー解除
        public async Task<IActionResult> OnPostUnfollowAsync(string targetUserId) {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            await EnsureOpenAsync();
            var userId = AccountDataGetter.GetUserId();

            try {
                var sql = "DELETE FROM UserFollow WHERE To_User_Id = :Me AND For_User_Id = :Target";
                using var cmd = new OracleCommand(sql, _conn);
                cmd.Parameters.Add(new OracleParameter("Me", userId));
                cmd.Parameters.Add(new OracleParameter("Target", targetUserId));
                await cmd.ExecuteNonQueryAsync();
                Message = "フォローを解除しました。";
            } catch {
                Message = "フォロー解除に失敗しました。";
                IsError = true;
            }

            await LoadDataAsync(userId);
            return Page();
        }

        // ブロック解除
        public async Task<IActionResult> OnPostUnblockAsync(string targetUserId) {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            await EnsureOpenAsync();
            var userId = AccountDataGetter.GetUserId();

            try {
                var sql = "DELETE FROM UserBlock WHERE To_User_Id = :Me AND For_User_Id = :Target";
                using var cmd = new OracleCommand(sql, _conn);
                cmd.Parameters.Add(new OracleParameter("Me", userId));
                cmd.Parameters.Add(new OracleParameter("Target", targetUserId));
                await cmd.ExecuteNonQueryAsync();
                Message = "ブロックを解除しました。";
            } catch {
                Message = "ブロック解除に失敗しました。";
                IsError = true;
            }

            await LoadDataAsync(userId);
            return Page();
        }

        // 書籍ミュート追加
        public async Task<IActionResult> OnPostMuteBookAsync(string isbn) {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(isbn)) {
                Message = "ISBNが指定されていません。";
                IsError = true;
                await EnsureOpenAsync();
                await LoadDataAsync(AccountDataGetter.GetUserId());
                return Page();
            }

            await EnsureOpenAsync();
            var userId = AccountDataGetter.GetUserId();

            try {
                // 重複を無視して挿入
                var sql = @"
                    MERGE INTO BookMute m
                    USING DUAL ON (m.User_Id = :UserId AND m.ISBN = :ISBN)
                    WHEN NOT MATCHED THEN INSERT (User_Id, ISBN) VALUES (:UserId2, :ISBN2)
                ";
                using var cmd = new OracleCommand(sql, _conn);
                cmd.Parameters.Add(new OracleParameter("UserId", userId));
                cmd.Parameters.Add(new OracleParameter("ISBN", isbn));
                cmd.Parameters.Add(new OracleParameter("UserId2", userId));
                cmd.Parameters.Add(new OracleParameter("ISBN2", isbn));
                await cmd.ExecuteNonQueryAsync();
                Message = "書籍をミュートしました。";
            } catch {
                Message = "書籍ミュートに失敗しました。";
                IsError = true;
            }

            await LoadDataAsync(userId);
            return Page();
        }

        // 書籍ミュート解除
        public async Task<IActionResult> OnPostUnmuteBookAsync(string isbn) {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            await EnsureOpenAsync();
            var userId = AccountDataGetter.GetUserId();

            try {
                var sql = "DELETE FROM BookMute WHERE User_Id = :UserId AND ISBN = :ISBN";
                using var cmd = new OracleCommand(sql, _conn);
                cmd.Parameters.Add(new OracleParameter("UserId", userId));
                cmd.Parameters.Add(new OracleParameter("ISBN", isbn));
                await cmd.ExecuteNonQueryAsync();
                Message = "ミュートを解除しました。";
            } catch {
                Message = "ミュート解除に失敗しました。";
                IsError = true;
            }

            await LoadDataAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _iconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg");
            }

            return NotFound();
        }
    }
}
