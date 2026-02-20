using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Pages {
    public class UsersModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly UserIconGetter _userIconGetter;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersModel> _logger;

        public List<UserSummary> RecommendUsers { get; set; } = new();

        public UsersModel(ILogger<UsersModel> logger, OracleConnection conn, IConfiguration configuration) {
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

                // ランダムなユーザーを取得（自分以外）
                var myId = AccountDataGetter.IsAuthenticated() ? AccountDataGetter.GetUserId() : null;

                var query = myId != null
                    ? @"SELECT * FROM (
                            SELECT User_PublicId, User_Name, User_Profile
                            FROM Users
                            WHERE User_Id != :MyId
                            ORDER BY DBMS_RANDOM.VALUE
                        ) WHERE ROWNUM <= 20"
                    : @"SELECT * FROM (
                            SELECT User_PublicId, User_Name, User_Profile
                            FROM Users
                            ORDER BY DBMS_RANDOM.VALUE
                        ) WHERE ROWNUM <= 20";

                using (var command = new OracleCommand(query, _conn)) {
                    if (myId != null) {
                        command.Parameters.Add(new OracleParameter("MyId", myId));
                    }

                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            RecommendUsers.Add(new UserSummary {
                                UserPublicId = reader["User_PublicId"].ToString() ?? "",
                                UserName = reader["User_Name"].ToString() ?? "",
                                UserProfile = reader["User_Profile"] == DBNull.Value ? "" : reader["User_Profile"].ToString() ?? ""
                            });
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "ユーザー一覧取得エラー");
                RecommendUsers = new();
            }
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _userIconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.LARGE);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg");
            }
            return NotFound();
        }

        public class UserSummary {
            public string UserPublicId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string UserProfile { get; set; } = string.Empty;
        }
    }
}
