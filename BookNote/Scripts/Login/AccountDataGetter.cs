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
        private static IHttpContextAccessor _staticHttpContextAccessor;

        private AccountDataGetter() {
        }

        public static void Initialize(IHttpContextAccessor httpContextAccessor) {
            _staticHttpContextAccessor = httpContextAccessor;
        }

        // ========== ログイン情報取得用メソッド ==========

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
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(config))) {
                    return await GetDbUserNameAsync(connection);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
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
                return user != null ? user.UserPublicId : null;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
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
                return user != null ? user.UserName : null;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public static async Task<string?> GetDbUserPublicIdAsync() {
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(config))) {
                    return await GetDbUserPublicIdAsync(connection);
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
        public static string GetUserId() {
            var httpContext = _staticHttpContextAccessor?.HttpContext;
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