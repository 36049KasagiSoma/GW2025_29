using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using BookNote.Scripts;
using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Pages.user {
    public class WithdrawModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly IConfiguration _configuration;
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;

        public WithdrawModel(OracleConnection conn, IConfiguration configuration, IAmazonCognitoIdentityProvider cognitoClient) {
            _conn = conn;
            _configuration = configuration;
            _cognitoClient = cognitoClient;
        }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet() {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync() {
            if (!AccountDataGetter.IsAuthenticated())
                return RedirectToPage("/Login");

            var userId = AccountDataGetter.GetUserId();

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            using var transaction = _conn.BeginTransaction();

            try {
                // すでに退会済みの場合はスキップ（2重送信対策）
                var checkSql = "SELECT User_StatusId FROM Users WHERE User_Id = :UserId";
                using var checkCmd = new OracleCommand(checkSql, _conn);
                checkCmd.Transaction = transaction;
                checkCmd.Parameters.Add(new OracleParameter("UserId", userId));
                var currentStatus = await checkCmd.ExecuteScalarAsync();

                if (currentStatus != null && Convert.ToDecimal(currentStatus) == 4)
                    return await SignOutAndRedirectToCognito();

                // 1. 自分のレビューへのいいねを削除
                await ExecuteNonQuery(transaction,
                    "DELETE FROM GoodReview WHERE Review_Id IN (SELECT Review_Id FROM BookReview WHERE User_Id = :UserId)",
                    userId);

                // 2. 自分のレビューへのコメントを削除
                await ExecuteNonQuery(transaction,
                    "DELETE FROM ReviewComment WHERE Review_Id IN (SELECT Review_Id FROM BookReview WHERE User_Id = :UserId)",
                    userId);

                // 3. 自分が他のレビューにつけたいいねを削除
                await ExecuteNonQuery(transaction,
                    "DELETE FROM GoodReview WHERE User_Id = :UserId",
                    userId);

                // 4. 自分が他のレビューにつけたコメントを削除
                await ExecuteNonQuery(transaction,
                    "DELETE FROM ReviewComment WHERE User_Id = :UserId",
                    userId);

                // 5. 自分のレビューを一時非公開（Status_Id = 3: hidden）に変更
                await ExecuteNonQuery(transaction,
                    "UPDATE BookReview SET Status_Id = 3 WHERE User_Id = :UserId",
                    userId);

                // 6. フォロー関係を削除（To / For どちらに入っていても）
                await ExecuteNonQuery(transaction,
                    "DELETE FROM UserFollow WHERE To_User_Id = :UserId OR For_User_Id = :UserId",
                    userId);

                // 7. ブロック関係を削除（To / For どちらに入っていても）
                await ExecuteNonQuery(transaction,
                    "DELETE FROM UserBlock WHERE To_User_Id = :UserId OR For_User_Id = :UserId",
                    userId);

                // 8. 書籍ミュートを削除
                await ExecuteNonQuery(transaction,
                    "DELETE FROM BookMute WHERE User_Id = :UserId",
                    userId);

                // 9. ユーザーステータスを退会済み（4）に更新
                await ExecuteNonQuery(transaction,
                    "UPDATE Users SET User_StatusId = 4 WHERE User_Id = :UserId",
                    userId);

                await transaction.CommitAsync();

                // 10. CognitoユーザーをUserPoolから削除
                var userPoolId = _configuration["BookNoteKeys:AWS:UserPoolId"];
                var deleteRequest = new AdminDeleteUserRequest {
                    UserPoolId = userPoolId,
                    Username = userId
                };
                await _cognitoClient.AdminDeleteUserAsync(deleteRequest);

                // 11. アクティビティログを記録（サインアウト前に）
                ActivityTracer.LogActivity(ActivityType.LOGOUT, userId);

                // 12. クッキー認証からサインアウト・セッションクリア
                return await SignOutAndRedirectToCognito();

            } catch {
                await transaction.RollbackAsync();
                ErrorMessage = "退会処理に失敗しました。しばらく時間をおいてから再度お試しください。";
                return Page();
            }
        }

        private async Task ExecuteNonQuery(OracleTransaction transaction, string sql, string userId) {
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Transaction = transaction;
            cmd.Parameters.Add(new OracleParameter("UserId", userId));
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<IActionResult> SignOutAndRedirectToCognito() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
            var clientId = _configuration["BookNoteKeys:AWS:ClientId"];
            var logoutUrl = _configuration["BookNoteKeys:AWS:LogoutUrl"];

            var cognitoLogoutUrl = $"https://{cognitoDomain}/logout?" +
                                   $"client_id={clientId}&" +
                                   $"logout_uri={Uri.EscapeDataString(logoutUrl ?? "")}";

            return Redirect(cognitoLogoutUrl);
        }
    }
}