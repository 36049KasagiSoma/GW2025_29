using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using BookNote.Scripts;
using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Diagnostics;

namespace BookNote {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // HttpClientFactoryを追加
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            // 認証サービスの追加
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.LoginPath = "/Login";  // ログインページのパス
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);  // クッキーの有効期限（7日間）
                    options.SlidingExpiration = true;  // アクティビティがあれば期限を延長
                    options.Cookie.HttpOnly = true;  // JavaScriptからのアクセスを防ぐ
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS必須
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.Name = "BookNote.Auth";  // クッキー名
                });

            builder.Services.AddAuthorization();

            // セッション設定
            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // データベース
            builder.Services.AddScoped<OracleConnection>(sp => {
                var conStr = Keywords.GetDbConnectionString(
                        sp.GetRequiredService<IConfiguration>());
                ActivityTracer.Initialize(conStr);
                return new OracleConnection(conStr);
            });


            // Razor Pages
            builder.Services.AddRazorPages().AddJsonOptions(options => {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            // Controller
            builder.Services.AddControllers();

            var app = builder.Build();

            // AccountControllerを初期化
            using (var scope = app.Services.CreateScope()) {
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                AccountDataGetter.Initialize(httpContextAccessor);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStatusCodePagesWithReExecute("/error/Error{0}");

            app.UseRouting();

            // 認証・認可ミドルウェア
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.MapRazorPages();
            app.MapControllers();

            app.Run();
        }
    }
}