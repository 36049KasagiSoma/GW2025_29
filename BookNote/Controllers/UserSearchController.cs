using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Controllers {
    [Route("api/users")]
    [ApiController]
    public class UserSearchController : ControllerBase {
        private readonly OracleConnection _conn;
        private readonly ILogger<UserSearchController> _logger;

        public UserSearchController(OracleConnection conn, ILogger<UserSearchController> logger) {
            _conn = conn;
            _logger = logger;
        }

        // ユーザー検索
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string keyword,
            [FromQuery] string searchType = "name") {

            if (string.IsNullOrWhiteSpace(keyword)) {
                return BadRequest(new { error = "キーワードを入力してください" });
            }

            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                string query;
                if (searchType == "id") {
                    // ID前方一致検索
                    query = @"
                        SELECT User_PublicId, User_Name, User_Profile
                        FROM Users
                        WHERE UPPER(User_PublicId) LIKE UPPER(:Keyword)
                        AND ROWNUM <= 30
                        ORDER BY User_Name ASC";
                } else {
                    // 名前部分一致検索
                    query = @"
                        SELECT User_PublicId, User_Name, User_Profile
                        FROM Users
                        WHERE UPPER(User_Name) LIKE UPPER(:Keyword)
                        AND ROWNUM <= 30
                        ORDER BY User_Name ASC";
                }

                var users = new List<object>();

                using (var command = new OracleCommand(query, _conn)) {
                    var param = searchType == "id"
                        ? keyword.TrimStart('@') + "%"   // ID検索は前方一致
                        : "%" + keyword + "%";           // 名前検索は部分一致

                    command.Parameters.Add(new OracleParameter("Keyword", param));

                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            var profile = reader["User_Profile"] == DBNull.Value
                                ? ""
                                : reader["User_Profile"].ToString() ?? "";

                            users.Add(new {
                                userPublicId = reader["User_PublicId"].ToString(),
                                userName = reader["User_Name"].ToString(),
                                userProfile = profile.Length > 60 ? profile[..60] + "…" : profile
                            });
                        }
                    }
                }

                return Ok(users);
            } catch (Exception ex) {
                _logger.LogError(ex, "ユーザー検索エラー");
                return StatusCode(500, new { error = "検索に失敗しました" });
            }
        }
    }
}
