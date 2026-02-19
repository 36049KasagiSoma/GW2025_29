using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Models;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BookNote.Scripts.Login {
    [Route("[controller]")]
    public class AccountController : Controller {
        private readonly IConfiguration _configuration;
        private readonly OracleConnection _conn;
        private readonly HttpClient _httpClient;

        public AccountController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, OracleConnection conn) {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            AccountDataGetter.Initialize(httpContextAccessor, _configuration);
            _conn = conn;
        }

        // ========== ログイン/ログアウト処理 ==========

        [HttpGet("Login")]
        public IActionResult Login(string returnUrl = null) {
            var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
            var clientId = _configuration["BookNoteKeys:AWS:ClientId"];
            var callbackUrl = _configuration["BookNoteKeys:AWS:CallbackUrl"];
            var state = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";

            var loginUrl = $"https://{cognitoDomain}/login?" +
                          $"lang=ja&" +
                          $"client_id={clientId}&" +
                          $"response_type=code&" +
                          $"scope=openid+email+profile&" +
                          $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                          $"state={Uri.EscapeDataString(state)}";

            ViewData["LoginUrl"] = loginUrl;
            return View();
        }

        [HttpGet("Callback")]
        public async Task<IActionResult> Callback(string code, string state) {
            if (string.IsNullOrEmpty(code)) {
                return BadRequest("認証コードが取得できませんでした。");
            }

            try {
                var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
                var clientId = _configuration["BookNoteKeys:AWS:ClientId"];
                var callbackUrl = _configuration["BookNoteKeys:AWS:CallbackUrl"];

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

                // ユーザー情報を取得
                var userInfo = await GetUserInfoAsync(tokenResponse.access_token);

                // セッションに保存
                HttpContext.Session.SetString("AccessToken", tokenResponse.access_token);
                HttpContext.Session.SetString("IdToken", tokenResponse.id_token);
                HttpContext.Session.SetString("RefreshToken", tokenResponse.refresh_token);

                if (userInfo != null) {
                    if (!string.IsNullOrEmpty(userInfo.sub)) {
                        HttpContext.Session.SetString("UserId", userInfo.sub);
                    }
                    if (!string.IsNullOrEmpty(userInfo.name)) {
                        HttpContext.Session.SetString("Name", userInfo.name);
                    }
                    if (!string.IsNullOrEmpty(userInfo.username)) {
                        HttpContext.Session.SetString("Username", userInfo.username);
                    }
                }

                // クッキー認証
                try {
                    // 修正: userInfo.subを直接使ってDBから取得
                    UserGetter userGetter = new UserGetter(_conn);
                    var dbUser = await userGetter.GetUserToSub(userInfo?.sub);
                    var userName = dbUser?.UserName;
                    var userPublicId = dbUser?.UserPublicId;

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userInfo?.sub ?? "unknown"),
                        new Claim(ClaimTypes.Email, userInfo?.email ?? ""),
                        new Claim(ClaimTypes.Name, userInfo?.name ?? userInfo?.username ?? ""),
                        new Claim("access_token", tokenResponse.access_token),
                        new Claim("id_token", tokenResponse.id_token),
                        new Claim("refresh_token", tokenResponse.refresh_token),
                        new Claim("db_username", userName ?? "名無しユーザー"),
                        new Claim("user_public_id", userPublicId ?? ""),
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                } catch (Exception ex) {
                    Console.WriteLine($"ERROR: SignInAsync failed - {ex.Message}");
                }

                if (AccountDataGetter.IsAuthenticated())
                    ActivityTracer.LogActivity(ActivityTrace.ActivityType.LOGIN, AccountDataGetter.GetUserId());

                // リダイレクト
                var returnUrl = !string.IsNullOrEmpty(state) ? state : "/";

                if (Url.IsLocalUrl(returnUrl)) {
                    return Redirect(returnUrl);
                } else {
                    return RedirectToAction("Index", "Home");
                }
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: Callback failed - {ex.Message}");
                return BadRequest($"ログイン処理に失敗しました: {ex.Message}");
            }
        }

        private async Task<UserInfo> GetUserInfoAsync(string accessToken) {
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

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            var cognitoDomain = _configuration["BookNoteKeys:AWS:Domain"];
            var clientId = _configuration["BookNoteKeys:AWS:ClientId"];
            var logoutUrl = _configuration["BookNoteKeys:AWS:LogoutUrl"];

            var cognitoLogoutUrl = $"https://{cognitoDomain}/logout?" +
                                  $"client_id={clientId}&" +
                                  $"logout_uri={Uri.EscapeDataString(logoutUrl)}";
            if (AccountDataGetter.IsAuthenticated())
                ActivityTracer.LogActivity(ActivityTrace.ActivityType.LOGOUT, AccountDataGetter.GetUserId());
            return Redirect(cognitoLogoutUrl);
        }

        // ========== 内部クラス ==========

        private class TokenResponse {
            public string access_token { get; set; }
            public string id_token { get; set; }
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }

        private class UserInfo {
            [JsonPropertyName("sub")]
            public string sub { get; set; }

            [JsonPropertyName("email")]
            public string email { get; set; }

            [JsonPropertyName("username")]
            public string username { get; set; }

            [JsonPropertyName("name")]
            public string name { get; set; }
        }
    }
}