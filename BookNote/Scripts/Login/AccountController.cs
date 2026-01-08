using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookNote.Scripts.Login {
    public class AccountController : Controller {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AccountController(IConfiguration configuration, IHttpClientFactory httpClientFactory) {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null) {
            var cognitoDomain = _configuration["AWS:Domain"];
            var clientId = _configuration["AWS:ClientId"];
            var callbackUrl = _configuration["AWS:CallbackUrl"];
            var state = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";

            // AWSホストUIへのリダイレクトURL
            var loginUrl = $"https://{cognitoDomain}/login?" +
                          $"client_id={clientId}&" +
                          $"response_type=code&" +
                          $"scope=openid+email+profile&" +
                          $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                          $"state={Uri.EscapeDataString(state)}";

            ViewData["LoginUrl"] = loginUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string code, string state) {
            if (string.IsNullOrEmpty(code)) {
                return BadRequest("認証コードが取得できませんでした。");
            }

            try {
                var cognitoDomain = _configuration["AWS:Domain"];
                var clientId = _configuration["AWS:ClientId"];
                var callbackUrl = _configuration["AWS:CallbackUrl"];

                // 認可コードをトークンに交換
                var tokenUrl = $"https://{cognitoDomain}/oauth2/token";
                var requestBody = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", clientId },
                    { "code", code },
                    { "redirect_uri", callbackUrl }
                };

                var response = await _httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(requestBody));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) {
                    return BadRequest($"トークン取得に失敗しました: {responseContent}");
                }

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                // セッションにトークンを保存
                HttpContext.Session.SetString("AccessToken", tokenResponse.access_token);
                HttpContext.Session.SetString("IdToken", tokenResponse.id_token);
                HttpContext.Session.SetString("RefreshToken", tokenResponse.refresh_token);

                // ユーザー情報を取得してセッションに保存
                var userInfo = await GetUserInfoAsync(tokenResponse.access_token);
                if (userInfo != null) {
                    if (!string.IsNullOrEmpty(userInfo.name)) {
                        HttpContext.Session.SetString("Name", userInfo.name);
                    }
                }

                // リダイレクト
                var returnUrl = !string.IsNullOrEmpty(state) ? state : "/";
                if (Url.IsLocalUrl(returnUrl)) {
                    return Redirect(returnUrl);
                } else {
                    return RedirectToAction("Index", "Home");
                }
            } catch (Exception ex) {
                return BadRequest($"ログイン処理に失敗しました: {ex.Message}");
            }
        }

        private async Task<UserInfo> GetUserInfoAsync(string accessToken) {
            try {
                var cognitoDomain = _configuration["AWS:Domain"];
                var userInfoUrl = $"https://{cognitoDomain}/oauth2/userInfo";

                var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode) {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserInfo>(content);
                }
            } catch { }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout() {
            HttpContext.Session.Clear();

            var cognitoDomain = _configuration["AWS:Domain"];
            var clientId = _configuration["AWS:ClientId"];
            var logoutUrl = _configuration["AWS:LogoutUrl"];

            // Cognitoからもログアウト
            var cognitoLogoutUrl = $"https://{cognitoDomain}/logout?" +
                                  $"client_id={clientId}&" +
                                  $"logout_uri={Uri.EscapeDataString(logoutUrl)}";

            return Redirect(cognitoLogoutUrl);
        }

        private class TokenResponse {
            public string access_token { get; set; }
            public string id_token { get; set; }
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }

        private class UserInfo {
            public string sub { get; set; }
            public string email { get; set; }
            public string username { get; set; }
            public string name { get; set; }
        }
    }
}