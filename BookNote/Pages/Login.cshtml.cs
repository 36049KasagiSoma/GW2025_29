using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BookCommunityApp.Pages {

    public class LoginModel : PageModel {
        private readonly IConfiguration _configuration;

        public string LoginUrl { get; set; }

        public LoginModel(IConfiguration configuration) {
            _configuration = configuration;
        }

        public IActionResult OnGet(string returnUrl = null) {
            var cognitoDomain = _configuration["AWS:Domain"];
            var clientId = _configuration["AWS:ClientId"];
            var callbackUrl = _configuration["AWS:CallbackUrl"];

            // 設定値の検証
            if (string.IsNullOrEmpty(cognitoDomain)) {
                return Content("エラー: AWS:Domain が設定されていません。appsettings.json を確認してください。");
            }
            if (string.IsNullOrEmpty(clientId)) {
                return Content("エラー: AWS:ClientId が設定されていません。appsettings.json を確認してください。");
            }
            if (string.IsNullOrEmpty(callbackUrl)) {
                return Content("エラー: AWS:CallbackUrl が設定されていません。appsettings.json を確認してください。");
            }

            var state = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";

            // AWSホストUIへのリダイレクトURL
            LoginUrl = $"https://{cognitoDomain}/login?" +
                      $"client_id={clientId}&" +
                      $"response_type=code&" +
                      $"scope=openid+email+profile&" +
                      $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                      $"state={Uri.EscapeDataString(state)}&" +
                      $"lang=ja";

            return Page();
        }
    }
}

