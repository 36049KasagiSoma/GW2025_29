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
using System.Threading.Tasks;

namespace BookNote.Scripts.Login {
    [Route("[controller]")]
    public class AccountController : Controller {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private static IHttpContextAccessor _staticHttpContextAccessor;

        public AccountController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor) {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            if (_staticHttpContextAccessor == null) {
                _staticHttpContextAccessor = httpContextAccessor;
            }
        }

        public static void Initialize(IHttpContextAccessor httpContextAccessor) {
            _staticHttpContextAccessor = httpContextAccessor;
        }
        // ========== Static メソッド（ログイン情報取得用） ==========

        /// <summary>
        /// ログインしているかどうかを確認
        /// </summary>
        public static bool IsAuthenticated() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            return httpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// ユーザー名を取得
        /// </summary>
        public static string GetUserName() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;
            return httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
        }
        /// <summary>
        /// ユーザー名を取得(DB)
        /// </summary>
        public static async Task<string?> GetDbUserNameAsync() {
            var id = GetUserId();
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(config))) {
                    await connection.OpenAsync();
                    UserGetter userGetter = new UserGetter(connection);
                    var user = await userGetter.GetUserToSub(id);
                    return user != null ? user.UserName : null;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// メールアドレスを取得
        /// </summary>
        public static string GetEmail() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;
            return httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// ユーザーID（sub）を取得
        /// </summary>
        /// <summary>
        /// ユーザーID（sub）を取得
        /// </summary>
        public static string GetUserId() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;

            // デバッグ情報を出力
            if (httpContext == null) {
                Console.WriteLine("DEBUG: HttpContext is null");
                return null;
            }

            if (httpContext.User == null) {
                Console.WriteLine("DEBUG: User is null");
                return null;
            }

            if (httpContext.User.Identity?.IsAuthenticated != true) {
                Console.WriteLine("DEBUG: User is not authenticated");
                return null;
            }


            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return userId;
        }

        /// <summary>
        /// アクセストークンを取得
        /// </summary>
        public static string GetAccessToken() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;
            return httpContext.User.FindFirst("access_token")?.Value;
        }

        /// <summary>
        /// IDトークンを取得
        /// </summary>
        public static string GetIdToken() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;
            return httpContext.User.FindFirst("id_token")?.Value;
        }

        /// <summary>
        /// リフレッシュトークンを取得
        /// </summary>
        public static string GetRefreshToken() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;
            return httpContext.User.FindFirst("refresh_token")?.Value;
        }

        /// <summary>
        /// すべてのユーザー情報を取得
        /// </summary>
        public static UserInfoData GetUserInfo() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) return null;

            return new UserInfoData {
                UserId = GetUserId(),
                Name = GetUserName(),
                Email = GetEmail(),
                AccessToken = GetAccessToken(),
                IdToken = GetIdToken(),
                RefreshToken = GetRefreshToken()
            };
        }

        // ========== ログイン/ログアウト処理 ==========

        [HttpGet("Login")]
        public IActionResult Login(string returnUrl = null) {
            var cognitoDomain = _configuration["AWS:Domain"];
            var clientId = _configuration["AWS:ClientId"];
            var callbackUrl = _configuration["AWS:CallbackUrl"];
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
                    Console.WriteLine($"ERROR: Token request failed - {responseContent}");
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
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userInfo?.sub ?? "unknown"),
                        new Claim(ClaimTypes.Email, userInfo?.email ?? ""),
                        new Claim(ClaimTypes.Name, userInfo?.name ?? userInfo?.username ?? ""),
                        new Claim("access_token", tokenResponse.access_token),
                        new Claim("id_token", tokenResponse.id_token),
                        new Claim("refresh_token", tokenResponse.refresh_token)
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
                    Console.WriteLine($"ERROR: StackTrace - {ex.StackTrace}");
                    // セッション認証は成功しているので続行
                }

                // リダイレクト
                var returnUrl = !string.IsNullOrEmpty(state) ? state : "/";

                if (Url.IsLocalUrl(returnUrl)) {
                    return Redirect(returnUrl);
                } else {
                    return RedirectToAction("Index", "Home");
                }
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: Callback failed - {ex.Message}");
                Console.WriteLine($"ERROR: StackTrace - {ex.StackTrace}");
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

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            var cognitoDomain = _configuration["AWS:Domain"];
            var clientId = _configuration["AWS:ClientId"];
            var logoutUrl = _configuration["AWS:LogoutUrl"];

            var cognitoLogoutUrl = $"https://{cognitoDomain}/logout?" +
                                  $"client_id={clientId}&" +
                                  $"logout_uri={Uri.EscapeDataString(logoutUrl)}";

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
            public string sub { get; set; }
            public string email { get; set; }
            public string username { get; set; }
            public string name { get; set; }
        }

        public class UserInfoData {
            public string UserId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string AccessToken { get; set; }
            public string IdToken { get; set; }
            public string RefreshToken { get; set; }
        }
    }
}