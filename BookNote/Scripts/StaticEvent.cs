using System.Text.RegularExpressions;

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
            var now = DateTime.Now;
            var diff = now - postingTime;

            if (diff.TotalMinutes < 60) {
                return $"{(int)diff.TotalMinutes}分前";
            } else if (diff.TotalHours < 24) {
                return $"{(int)diff.TotalHours}時間前";
            } else if (diff.TotalDays < 30) {
                return $"{(int)diff.TotalDays}日前";
            } else {
                return postingTime.ToString("yyyy/MM/dd");
            }
        }
    }
}
