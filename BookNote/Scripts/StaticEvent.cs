using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace BookNote.Scripts {
    public class StaticEvent {
        private StaticEvent() { }
        public static string ToPlainText(string markdown) {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var text = markdown;

            // 改行コードを統一
            text = text.Replace("\r\n", "").Replace("\r", "");
            // コードブロック ``` ```
            text = Regex.Replace(text, @"```[\s\S]*?```", string.Empty);
            // インラインコード `code`
            text = Regex.Replace(text, @"`([^`]*)`", "$1");
            // 画像 ![alt](url) → alt
            text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]+\)", "$1");
            // リンク [text](url) → text
            text = Regex.Replace(text, @"\[(.*?)\]\([^)]+\)", "$1");
            // 見出し # ## ###
            text = Regex.Replace(text, @"^\s{0,3}#{1,6}\s*", string.Empty, RegexOptions.Multiline);
            // 強調 **bold** / __bold__
            text = Regex.Replace(text, @"(\*\*|__)(.*?)\1", "$2");

            // 斜体 *italic* / _italic_
            text = Regex.Replace(text, @"(\*|_)(.*?)\1", "$2");
            // 打ち消し線 ~~text~~
            text = Regex.Replace(text, @"~~(.*?)~~", "$1");
            // 引用 >
            text = Regex.Replace(text, @"^\s*>\s?", string.Empty, RegexOptions.Multiline);
            // 箇条書き -, *, +
            text = Regex.Replace(text, @"^\s*[-*+]\s+", string.Empty, RegexOptions.Multiline);
            // 番号付きリスト 1. 2.
            text = Regex.Replace(text, @"^\s*\d+\.\s+", string.Empty, RegexOptions.Multiline);
            // 水平線 --- *** ___
            text = Regex.Replace(text, @"^\s*([-*_]){3,}\s*$", string.Empty, RegexOptions.Multiline);
            // 余分な空行を整理
            text = Regex.Replace(text, @"\n{2,}", "\n");

            return text.Trim();
        }

        public static string TrimReview(string? text, int cnt) {
            if (text == null) return string.Empty;
            var pt = ToPlainText(text);
            pt = Regex.Replace(pt, @"[\p{Z}\p{C}]", "");
            if (pt.Length <= cnt) return pt;

            return pt.Substring(0, cnt - 1) + "…";
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
