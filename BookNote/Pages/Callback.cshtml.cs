using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BookNote.Pages {
    public class CallbackModel : PageModel {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public CallbackModel(IConfiguration configuration, IHttpClientFactory httpClientFactory) {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IActionResult> OnGetAsync(string code, string state) {
            if (string.IsNullOrEmpty(code)) {
                return BadRequest("認証コードが取得できませんでした。");
            }

            try {
                var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
                var clientId = _configuration["BookNoteKeys:AWS:ClientId"] ?? "";
                var callbackUrl = _configuration["BookNoteKeys:AWS:CallbackUrl"] ?? "";

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

                if (tokenResponse != null) {
                    // セッションにトークンを保存
                    HttpContext.Session.SetString("AccessToken", tokenResponse.access_token);
                    HttpContext.Session.SetString("IdToken", tokenResponse.id_token);
                    HttpContext.Session.SetString("RefreshToken", tokenResponse.refresh_token);
                    // ユーザー情報を取得してセッションに保存
                    var userInfo = await GetUserInfoAsync(tokenResponse.access_token);
                    if (userInfo != null) {
                        HttpContext.Session.SetString("Username", userInfo.username ?? userInfo.email ?? "");
                        if (!string.IsNullOrEmpty(userInfo.name)) {
                            HttpContext.Session.SetString("Name", userInfo.name);
                        }
                    }
                }

                // リダイレクト
                var returnUrl = !string.IsNullOrEmpty(state) ? state : "/";
                if (Url.IsLocalUrl(returnUrl)) {
                    return Redirect(returnUrl);
                } else {
                    return RedirectToPage("/Index");
                }
            } catch (Exception ex) {
                return BadRequest($"ログイン処理に失敗しました: {ex.Message}");
            }
        }

        private async Task<UserInfo?> GetUserInfoAsync(string accessToken) {
            try {
                var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
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

        private class TokenResponse {
            public string access_token { get; set; } = string.Empty;
            public string id_token { get; set; } = string.Empty;
            public string refresh_token { get; set; } = string.Empty;
            public int expires_in { get; set; }
            public string token_type { get; set; } = string.Empty;
        }

        private class UserInfo {
            public string sub { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
            public string username { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
        }
    }
}
