using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using AWSSecretsManager.Provider;
using BookNote.Scripts;
using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

namespace BookNote {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // ポート設定 - Elastic Beanstalkではポート5000を使用（nginxが80でプロキシ）
            builder.WebHost.ConfigureKestrel(options => {
                options.ListenAnyIP(5000);

                // ヘッダーとCookieのサイズ制限を増やす
                options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB (デフォルトは32KB)
                options.Limits.MaxRequestHeaderCount = 100; // デフォルトは100
                options.Limits.MaxRequestLineSize = 16384; // 16KB (デフォルトは8KB)
            });

            // HttpClientFactoryを追加
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            // CloudFront経由かどうかを判定
            var isBehindCloudFront = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("CLOUDFRONT_ENABLED"));

            // 認証サービスの追加
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.LoginPath = "/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    // CloudFront経由の場合はForwardedHeadersで判断
                    options.Cookie.SecurePolicy = isBehindCloudFront
                        ? CookieSecurePolicy.None
                        : CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.Name = "BookNote.Auth";
                });

            builder.Configuration.AddSecretsManager(
                region: RegionEndpoint.APNortheast1
            );

            builder.Services.AddAuthorization();

            // セッション設定
            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = isBehindCloudFront
                    ? CookieSecurePolicy.None
                    : CookieSecurePolicy.Always;
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
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            // Controller
            builder.Services.AddControllers();

            var app = builder.Build();

            // AccountControllerを初期化
            using (var scope = app.Services.CreateScope()) {
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                AccountDataGetter.Initialize(httpContextAccessor, builder.Configuration);
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseExceptionHandler("/error/Error500");

            // ForwardedHeaders設定（CloudFront対応）
            var forwardOptions = new ForwardedHeadersOptions {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto
            };
            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardOptions);

            // HTTPS通信 - CloudFront経由の場合は無効化
            if (!isBehindCloudFront && !app.Environment.IsDevelopment()) {
                app.UseHttpsRedirection();
            }

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