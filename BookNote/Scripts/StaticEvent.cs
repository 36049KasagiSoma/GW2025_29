using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace BookNote.Scripts {
    public class StaticEvent {
        private StaticEvent() { }
        public static string ToPlainText(string? html, bool deleteLineBreak = true) {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;
            var text = html;
            // scriptタグとその内容を削除
            text = Regex.Replace(text, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
            // styleタグとその内容を削除
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);
            if (deleteLineBreak) {
                // 改行タグを削除
                text = Regex.Replace(text, @"<br\s*/?>", "", RegexOptions.IgnoreCase);
                // ブロック要素の終了タグを削除
                text = Regex.Replace(text, @"</(p|div|h[1-6]|li|ul|ol)>", "", RegexOptions.IgnoreCase);
                // 改行コードを統一
                text = text.Replace("\n", "").Replace("\r", "");
            } else {
                // 改行タグを改行文字に変換
                text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
                // ブロック要素の終了タグを改行に変換
                text = Regex.Replace(text, @"</(p|div|h[1-6]|li|ul|ol)>", "\n", RegexOptions.IgnoreCase);
                // 改行コードを統一
                text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            }
                      // すべてのHTMLタグを削除
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // HTMLエンティティをデコード
            text = System.Net.WebUtility.HtmlDecode(text);

            // 余分な空行を整理
            text = Regex.Replace(text, @"\n{2,}", "\n");

            // 行頭・行末の空白を削除
            text = Regex.Replace(text, @"^[ \t]+|[ \t]+$", string.Empty, RegexOptions.Multiline);

            return text.Trim();
        }

        public static string TrimReview(string? text, int cnt) {
            if (text == null) return string.Empty;
            var pt = ToPlainText(text);
            pt = Regex.Replace(pt, @"[\p{Z}\p{C}]", "");
            return TrimText(pt, cnt);
        }

        public static string TrimText(string? text, int cnt) {
            if (text == null) return string.Empty;
            if (text.Length <= cnt) return text;

            return text.Substring(0, cnt - 1) + "…";
        }

        public static string FormatPostingTime(DateTime postingTime) {
            // 日本時間(JST)で現在時刻を取得
            TimeZoneInfo jstZone;
            try {
                jstZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            } catch {
                // Windowsの場合
                jstZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            }

            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jstZone);

            // postingTimeがUTCの場合はJSTに変換
            var postingTimeJst = postingTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(postingTime, jstZone)
                : postingTime;

            var diff = now - postingTimeJst;

            if (diff.TotalMinutes < 60) {
                return $"{(int)diff.TotalMinutes}分前";
            } else if (diff.TotalHours < 24) {
                return $"{(int)diff.TotalHours}時間前";
            } else if (diff.TotalDays < 30) {
                return $"{(int)diff.TotalDays}日前";
            } else {
                return postingTimeJst.ToString("yyyy/MM/dd");
            }
        }

        public static string toHash(string text) {
            using var sha = SHA512.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash = sha.ComputeHash(bytes);

            return Convert.ToHexString(hash);
        }



        /// <summary>
        /// アイコン画像を指定サイズにリサイズ
        /// </summary>
        /// <param name="sourceImageData">元画像のバイトデータ</param>
        /// <param name="targetSize">目標サイズ</param>
        /// <returns>リサイズされた画像のバイトデータ</returns>
        public static byte[] ResizeIcon(byte[] sourceImageData, int targetSize) {
            if (sourceImageData == null || sourceImageData.Length == 0) {
                throw new ArgumentException("画像データが空です", nameof(sourceImageData));
            }

            if (targetSize <= 0) {
                throw new ArgumentException("サイズは正の数である必要があります", nameof(targetSize));
            }

            using (var image = SixLabors.ImageSharp.Image.Load(sourceImageData)) {
                // リサイズ
                image.Mutate(x => x.Resize(new ResizeOptions {
                    Size = new SixLabors.ImageSharp.Size(targetSize, targetSize),
                    Mode = ResizeMode.Crop,
                    Sampler = KnownResamplers.Lanczos3
                }));

                // PNG形式でバイト配列に変換
                using (var outputStream = new MemoryStream()) {
                    image.Save(outputStream, new PngEncoder());
                    return outputStream.ToArray();
                }
            }
        }

    }
}
