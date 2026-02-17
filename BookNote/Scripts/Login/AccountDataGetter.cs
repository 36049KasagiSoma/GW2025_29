using BookNote.Scripts.Models;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookNote.Scripts.Login {
    public class AccountDataGetter {
        private static IHttpContextAccessor? _staticHttpContextAccessor;
        private static IConfiguration? _configuration;

        private AccountDataGetter() {
        }

        public static void Initialize(IHttpContextAccessor httpContextAccessor, IConfiguration configration) {
            _staticHttpContextAccessor = httpContextAccessor;
            _configuration = configration;
        }

        // ========== ログイン情報取得用メソッド ==========

        /// <summary>
        /// ログインしているかどうかを確認
        /// </summary>
        public static bool IsAuthenticated() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            var isAuth = httpContext?.User?.Identity?.IsAuthenticated ?? false;
            return isAuth;
        }

        /// <summary>
        /// ユーザー名を取得
        /// </summary>
        public static string GetUserName() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) {
                return null;
            }
            var userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;
            return userName;
        }

        /// <summary>
        /// ユーザー名を取得(DB)
        /// </summary>
        public static async Task<string?> GetDbUserNameAsync() {
            try {
                var userId = GetUserId();

                if (string.IsNullOrEmpty(userId)) {
                    Console.WriteLine("ERROR: GetDbUserNameAsync() - UserId is null or empty");
                    return null;
                }

                var connString = Keywords.GetDbConnectionString(_configuration);

                using (var connection = new OracleConnection(connString)) {
                    await connection.OpenAsync();

                    var result = await GetDbUserNameAsync(connection);
                    return result;
                }
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: GetDbUserNameAsync() - {ex.Message}");
                Console.WriteLine($"ERROR: GetDbUserNameAsync() - StackTrace: {ex.StackTrace}");
            }
            return null;
        }

        /// <summary>
        /// ユーザー公開IDを取得
        /// </summary>
        public static async Task<string?> GetDbUserPublicIdAsync(OracleConnection conn) {

            // まずClaimから取得を試みる
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true) {
                var cachedValue = httpContext.User.FindFirst("user_public_id")?.Value;
                if (!string.IsNullOrEmpty(cachedValue)) {
                    return cachedValue;
                }
            }

            // ClaimになければDBから取得
            var id = GetUserId();

            try {
                if (conn.State != ConnectionState.Open) {
                    await conn.OpenAsync();
                }
                UserGetter userGetter = new UserGetter(conn);

                var user = await userGetter.GetUserToSub(id);

                if (user != null) {
                    return user.UserPublicId;
                } else {
                    Console.WriteLine("ERROR: GetDbUserPublicIdAsync(conn) - User not found in DB");
                    return null;
                }
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: GetDbUserPublicIdAsync(conn) - {ex.Message}");
                Console.WriteLine($"ERROR: GetDbUserPublicIdAsync(conn) - StackTrace: {ex.StackTrace}");
            }
            return null;
        }

        /// <summary>
        /// ユーザー名を取得
        /// </summary>
        public static async Task<string?> GetDbUserNameAsync(OracleConnection conn) {

            // まずClaimから取得を試みる
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true) {
                var cachedValue = httpContext.User.FindFirst("db_username")?.Value;
                if (!string.IsNullOrEmpty(cachedValue)) {
                    return cachedValue;
                }
            }

            // ClaimになければDBから取得
            var id = GetUserId();

            try {
                if (conn.State != ConnectionState.Open) {
                    await conn.OpenAsync();
                }
                UserGetter userGetter = new UserGetter(conn);

                var user = await userGetter.GetUserToSub(id);

                if (user != null) {
                    return user.UserName;
                } else {
                    Console.WriteLine("ERROR: GetDbUserNameAsync(conn) - User not found in DB");
                    return null;
                }
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: GetDbUserNameAsync(conn) - {ex.Message}");
                Console.WriteLine($"ERROR: GetDbUserNameAsync(conn) - StackTrace: {ex.StackTrace}");
            }
            return null;
        }

        public static async Task<string?> GetDbUserPublicIdAsync() {
            try {
                var userId = GetUserId();

                if (string.IsNullOrEmpty(userId)) {
                    Console.WriteLine("ERROR: GetDbUserPublicIdAsync() - UserId is null or empty");
                    return null;
                }

                var connString = Keywords.GetDbConnectionString(_configuration);

                using (var connection = new OracleConnection(connString)) {
                    await connection.OpenAsync();

                    var result = await GetDbUserPublicIdAsync(connection);
                    return result;
                }
            } catch (Exception ex) {
                Console.WriteLine($"ERROR: GetDbUserPublicIdAsync() - {ex.Message}");
                Console.WriteLine($"ERROR: GetDbUserPublicIdAsync() - StackTrace: {ex.StackTrace}");
            }
            return null;
        }

        /// <summary>
        /// メールアドレスを取得
        /// </summary>
        public static string GetEmail() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) {
                return null;
            }
            var email = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            return email;
        }

        /// <summary>
        /// ユーザーID（sub）を取得
        /// </summary>
        public static string GetUserId() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId;
        }

        /// <summary>
        /// アクセストークンを取得
        /// </summary>
        public static string GetAccessToken() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) {
                return null;
            }
            var token = httpContext.User.FindFirst("access_token")?.Value;
            return token;
        }

        /// <summary>
        /// IDトークンを取得
        /// </summary>
        public static string GetIdToken() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) {
                return null;
            }
            var token = httpContext.User.FindFirst("id_token")?.Value;
            return token;
        }

        /// <summary>
        /// リフレッシュトークンを取得
        /// </summary>
        public static string GetRefreshToken() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) {
                return null;
            }
            var token = httpContext.User.FindFirst("refresh_token")?.Value;
            return token;
        }

        /// <summary>
        /// すべてのユーザー情報を取得
        /// </summary>
        public static UserInfoData GetUserInfo() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true) {
                return null;
            }

            var info = new UserInfoData {
                UserId = GetUserId(),
                Name = GetUserName(),
                Email = GetEmail(),
                AccessToken = GetAccessToken(),
                IdToken = GetIdToken(),
                RefreshToken = GetRefreshToken()
            };
            return info;
        }

        // ========== 内部クラス ==========

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