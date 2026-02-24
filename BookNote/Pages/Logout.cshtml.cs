using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BookNote.Pages {
    [IgnoreAntiforgeryToken]
    public class LogoutModel : PageModel {
        private readonly IConfiguration _configuration;

        public LogoutModel(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGet() {
            return await OnPostAsync();
        }

        public async Task<IActionResult> OnPostAsync() {
            // アクティビティログを記録（ログアウト前に）
            if (AccountDataGetter.IsAuthenticated()) {
                ActivityTracer.LogActivity(Scripts.ActivityTrace.ActivityType.LOGOUT, AccountDataGetter.GetUserId());
            }

            // クッキー認証からサインアウト
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // セッションをクリア
            HttpContext.Session.Clear();

            var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
            var clientId = _configuration["BookNoteKeys:AWS:ClientId"];
            var logoutUrl = _configuration["BookNoteKeys:AWS:LogoutUrl"];

            // Cognitoからもログアウト
            var cognitoLogoutUrl = $"https://{cognitoDomain}/logout?" +
                                  $"client_id={clientId}&" +
                                  $"logout_uri={Uri.EscapeDataString(logoutUrl??"")}";

            return Redirect(cognitoLogoutUrl);
        }
    }
}