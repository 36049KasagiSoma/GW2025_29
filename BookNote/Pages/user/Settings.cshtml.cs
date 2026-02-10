using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace BookNote.Pages.user {
    public class SettingsModel : PageModel {
        private readonly OracleConnection _conn;

        public SettingsModel(OracleConnection conn) {
            _conn = conn;
        }

        public string UserPublicId { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "ユーザー名は必須です")]
        [StringLength(50, ErrorMessage = "ユーザー名は50文字以内で入力してください")]
        public string UserName { get; set; } = string.Empty;

        [BindProperty]
        [StringLength(500, ErrorMessage = "プロフィールは500文字以内で入力してください")]
        public string? Profile { get; set; }

        [BindProperty]
        public string? IconBase64 { get; set; }

        public byte[]? IconImageData { get; set; }

        public async Task<IActionResult> OnGetAsync() {
            if (!AccountDataGetter.IsAuthenticated()) {
                return RedirectToPage("/Login");
            }

            var userId = AccountDataGetter.GetUserId();

            if (_conn.State != ConnectionState.Open) {
                await _conn.OpenAsync();
            }
            var query = @"
                    SELECT 
                        User_PublicId,
                        User_Name,
                        User_Profile
                    FROM Users
                    WHERE User_Id = :UserId
                ";

            using (var command = new OracleCommand(query, _conn)) {
                command.Parameters.Add(new OracleParameter("UserId", userId));

                using (var reader = await command.ExecuteReaderAsync()) {
                    if (await reader.ReadAsync()) {
                        UserPublicId = reader["User_PublicId"] as string ?? "";
                        UserName = reader["User_Name"] as string ?? "";
                        Profile = reader["User_Profile"] as string ?? "";
                    }
                }
            }

            // アイコン画像データを取得
            IconImageData = await new UserIconGetter()
                .GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);


            return Page();
        }

        public async Task<IActionResult> OnPostAsync() {
            if (!AccountDataGetter.IsAuthenticated()) {
                return RedirectToPage("/Login");
            }

            var userId = AccountDataGetter.GetUserId();

            // UserPublicIdを最初に取得（この位置に移動）
            if (_conn.State != ConnectionState.Open) {
                await _conn.OpenAsync();
            }
            var query = "SELECT User_PublicId FROM Users WHERE User_Id = :UserId";
            using (var command = new OracleCommand(query, _conn)) {
                command.Parameters.Add(new OracleParameter("UserId", userId));
                var result = await command.ExecuteScalarAsync();
                UserPublicId = result as string ?? "";
            }


            if (!ModelState.IsValid) {
                // バリデーションエラー時の処理
                IconImageData = await new UserIconGetter()
                    .GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);
                return Page();
            }

            // Base64形式のアイコン画像処理
            if (!string.IsNullOrEmpty(IconBase64)) {
                try {
                    var base64Data = IconBase64;
                    if (base64Data.Contains(",")) {
                        base64Data = base64Data.Split(',')[1];
                    }
                    var originalImageData = Convert.FromBase64String(base64Data);

                    var icon256 = StaticEvent.ResizeIcon(originalImageData, 256);
                    var icon64 = StaticEvent.ResizeIcon(originalImageData, 64);

                    Console.WriteLine($"元画像サイズ: {originalImageData.Length} bytes");
                    Console.WriteLine($"256×256サイズ: {icon256.Length} bytes");
                    Console.WriteLine($"64×64サイズ: {icon64.Length} bytes");

                    // S3にアップロード
                    var iconGetter = new UserIconGetter();
                    var uploadSuccess = await iconGetter.UploadIconAsync(UserPublicId, icon256, icon64);

                    if (!uploadSuccess) {
                        ModelState.AddModelError("IconBase64", "アイコンのアップロードに失敗しました");
                        IconImageData = await iconGetter
                            .GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);
                        return Page();
                    }

                    Console.WriteLine("アイコンアップロード完了");

                } catch (Exception ex) {
                    Console.WriteLine($"アイコン処理エラー: {ex.Message}");
                    ModelState.AddModelError("IconBase64", "画像の処理に失敗しました");

                    IconImageData = await new UserIconGetter()
                        .GetIconImageData(UserPublicId, UserIconGetter.IconSize.LARGE);
                    return Page();
                }
            }

            var query2 = @"
                    UPDATE Users
                    SET User_Name = :UserName,
                        User_Profile = :Profile
                    WHERE User_Id = :UserId
                ";

            using (var command = new OracleCommand(query2, _conn)) {
                command.Parameters.Add(new OracleParameter("UserName", UserName));
                command.Parameters.Add(new OracleParameter("Profile", (object?)Profile ?? DBNull.Value));
                command.Parameters.Add(new OracleParameter("UserId", userId));

                await command.ExecuteNonQueryAsync();
            }


            return RedirectToPage("/user/MyPage");
        }

    }
}