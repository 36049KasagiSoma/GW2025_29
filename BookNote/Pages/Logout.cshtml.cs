using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookNote.Pages
{
    [IgnoreAntiforgeryToken]
    public class LogoutModel : PageModel {
        private readonly IConfiguration _configuration;

        public LogoutModel(IConfiguration configuration) {
            _configuration = configuration;
        }

        public IActionResult OnGet() {
            return OnPost();
        }

        public IActionResult OnPost() {
            // セッションをクリア
            HttpContext.Session.Clear();

            var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
            var clientId = _configuration["BookNoteKeys:AWS:ClientId"];
            var logoutUrl = _configuration["BookNoteKeys:AWS:LogoutUrl"];

            // Cognitoからもログアウト
            var cognitoLogoutUrl = $"https://{cognitoDomain}/logout?" +
                                  $"client_id={clientId}&" +
                                  $"logout_uri={Uri.EscapeDataString(logoutUrl)}";

            return Redirect(cognitoLogoutUrl);
        }
    }
}
