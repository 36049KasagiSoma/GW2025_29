namespace BookNote.Scripts.BooksAPI.Moderation {
    public class ModerationJudger {
        private ModerationJudgerOptions _options;

        public ModerationJudger() : this(new ModerationJudgerOptions()) {
        }
        public ModerationJudger(ModerationJudgerOptions options) {
            _options = options;
        }

        public void SetOptions(ModerationJudgerOptions options) {
            _options = options;
        }

        public ModerationJudgerOptions GetOptions() {
            return _options;
        }

        public bool IsContentSafe(ModerationResponse resultResponse) {
            var result = resultResponse.Results[0];
            var report = GetReport(result);
            bool shouldBlock = report.Values.Any(v => v);
            return !shouldBlock;
        }

        public bool IsContentSafe(ModerationResult result) {
            var report = GetReport(result);
            bool shouldBlock = report.Values.Any(v => v);
            return !shouldBlock;
        }

        public Dictionary<string, bool> GetReport(ModerationResult result) {
            var report = new Dictionary<string, bool>();

            // ── カテゴリフラグによる判定 ──────────────────────────
            report["Hate"] = _options.IsBlockHate && result.Categories.Hate;
            report["HateThreatening"] = _options.IsBlockHateThreatening && result.Categories.HateThreatening;
            report["Harassment"] = _options.IsBlockHarassment && result.Categories.Harassment;
            report["HarassmentThreatening"] = _options.IsBlockHarassmentThreatening && result.Categories.HarassmentThreatening;
            report["SelfHarm"] = _options.IsBlockSelfHarm && result.Categories.SelfHarm;
            report["SelfHarmIntent"] = _options.IsBlockSelfHarmIntent && result.Categories.SelfHarmIntent;
            report["SelfHarmInstructions"] = _options.IsBlockSelfHarmInstructions && result.Categories.SelfHarmInstructions;
            report["Sexual"] = _options.IsBlockSexual && result.Categories.Sexual;
            report["SexualMinors"] = _options.IsBlockSexualMinors && result.Categories.SexualMinors;
            report["Violence"] = _options.IsBlockViolence && result.Categories.Violence;
            report["ViolenceGraphic"] = _options.IsBlockViolenceGraphic && result.Categories.ViolenceGraphic;

            // ── スコアによる判定（閾値が null の場合はスキップ）────
            report["HighScore_Hate"] = _options.HateScoreThreshold is double ht && result.CategoryScores.Hate >= ht;
            report["HighScore_HateThreatening"] = _options.HateThreateningScoreThreshold is double htt && result.CategoryScores.HateThreatening >= htt;
            report["HighScore_Harassment"] = _options.HarassmentScoreThreshold is double har && result.CategoryScores.Harassment >= har;
            report["HighScore_HarassmentThreatening"] = _options.HarassmentThreateningScoreThreshold is double hat && result.CategoryScores.HarassmentThreatening >= hat;
            report["HighScore_SelfHarm"] = _options.SelfHarmScoreThreshold is double sh && result.CategoryScores.SelfHarm >= sh;
            report["HighScore_SelfHarmIntent"] = _options.SelfHarmIntentScoreThreshold is double shi && result.CategoryScores.SelfHarmIntent >= shi;
            report["HighScore_SelfHarmInstructions"] = _options.SelfHarmInstructionsScoreThreshold is double shs && result.CategoryScores.SelfHarmInstructions >= shs;
            report["HighScore_Sexual"] = _options.SexualScoreThreshold is double sx && result.CategoryScores.Sexual >= sx;
            report["HighScore_SexualMinors"] = _options.SexualMinorsScoreThreshold is double sxm && result.CategoryScores.SexualMinors >= sxm;
            report["HighScore_Violence"] = _options.ViolenceScoreThreshold is double vl && result.CategoryScores.Violence >= vl;
            report["HighScore_ViolenceGraphic"] = _options.ViolenceGraphicScoreThreshold is double vlg && result.CategoryScores.ViolenceGraphic >= vlg;

            return report;
        }

        public void PrintReport(ModerationResult result) {
            Console.WriteLine("=== モデレーション判定結果 ===");
            Console.WriteLine($"  有害コンテンツ検出（OpenAI 総合判定）     : {Flag(result.Flagged)}");
            Console.WriteLine();
            Console.WriteLine($"  有害コンテンツ検出（option からの判定）   : {Flag(!IsContentSafe(result))}");
            Console.WriteLine("==============================");

            // --- カテゴリ違反フラグ ---
            Console.WriteLine("--- カテゴリ違反フラグ ---");
            Console.WriteLine($"  ヘイトスピーチ              : {Flag(result.Categories.Hate)}");
            Console.WriteLine($"  ヘイトスピーチ（脅迫含む）  : {Flag(result.Categories.HateThreatening)}");
            Console.WriteLine($"  ハラスメント                : {Flag(result.Categories.Harassment)}");
            Console.WriteLine($"  ハラスメント（脅迫含む）    : {Flag(result.Categories.HarassmentThreatening)}");
            Console.WriteLine($"  自傷・自殺                  : {Flag(result.Categories.SelfHarm)}");
            Console.WriteLine($"  自傷・自殺（意図）          : {Flag(result.Categories.SelfHarmIntent)}");
            Console.WriteLine($"  自傷・自殺（手順）          : {Flag(result.Categories.SelfHarmInstructions)}");
            Console.WriteLine($"  性的コンテンツ              : {Flag(result.Categories.Sexual)}");
            Console.WriteLine($"  未成年への性的コンテンツ    : {Flag(result.Categories.SexualMinors)}");
            Console.WriteLine($"  暴力                        : {Flag(result.Categories.Violence)}");
            Console.WriteLine($"  暴力（過激な描写含む）      : {Flag(result.Categories.ViolenceGraphic)}");
            Console.WriteLine();

            // --- カテゴリスコア（0.00 〜 1.00）---
            Console.WriteLine("--- カテゴリスコア（0.00 - 1.00）---");
            Console.WriteLine($"  ヘイトスピーチ              : {Score(result.CategoryScores.Hate)}");
            Console.WriteLine($"  ヘイトスピーチ（脅迫含む）  : {Score(result.CategoryScores.HateThreatening)}");
            Console.WriteLine($"  ハラスメント                : {Score(result.CategoryScores.Harassment)}");
            Console.WriteLine($"  ハラスメント（脅迫含む）    : {Score(result.CategoryScores.HarassmentThreatening)}");
            Console.WriteLine($"  自傷・自殺                  : {Score(result.CategoryScores.SelfHarm)}");
            Console.WriteLine($"  自傷・自殺（意図）          : {Score(result.CategoryScores.SelfHarmIntent)}");
            Console.WriteLine($"  自傷・自殺（手順）          : {Score(result.CategoryScores.SelfHarmInstructions)}");
            Console.WriteLine($"  性的コンテンツ              : {Score(result.CategoryScores.Sexual)}");
            Console.WriteLine($"  未成年への性的コンテンツ    : {Score(result.CategoryScores.SexualMinors)}");
            Console.WriteLine($"  暴力                        : {Score(result.CategoryScores.Violence)}");
            Console.WriteLine($"  暴力（過激な描写含む）      : {Score(result.CategoryScores.ViolenceGraphic)}");
            Console.WriteLine();

            // --- ブロック対象カテゴリ一覧 ---
            var report = GetReport(result);
            var blocked = report.Where(kv => kv.Value).Select(kv => kv.Key).ToList();

            if (blocked.Count > 0) {
                Console.WriteLine("--- [NG] ブロック対象カテゴリ ---");
                foreach (var key in blocked)
                    Console.WriteLine($"  [NG] {ToJapanese(key)}");
            } else {
                Console.WriteLine("--- [OK] ブロック対象カテゴリなし ---");
            }
        }

        // --- キー → 日本語ラベル変換 ---
        private static readonly Dictionary<string, string> KeyLabels = new() {
            ["Hate"] = "ヘイトスピーチ",
            ["HateThreatening"] = "ヘイトスピーチ（脅迫含む）",
            ["Harassment"] = "ハラスメント",
            ["HarassmentThreatening"] = "ハラスメント（脅迫含む）",
            ["SelfHarm"] = "自傷・自殺",
            ["SelfHarmIntent"] = "自傷・自殺（意図）",
            ["SelfHarmInstructions"] = "自傷・自殺（手順）",
            ["Sexual"] = "性的コンテンツ",
            ["SexualMinors"] = "未成年への性的コンテンツ",
            ["Violence"] = "暴力",
            ["ViolenceGraphic"] = "暴力（過激な描写含む）",
            ["HighScore_Hate"] = "高スコア：ヘイトスピーチ",
            ["HighScore_HateThreatening"] = "高スコア：ヘイトスピーチ（脅迫含む）",
            ["HighScore_Harassment"] = "高スコア：ハラスメント",
            ["HighScore_HarassmentThreatening"] = "高スコア：ハラスメント（脅迫含む）",
            ["HighScore_SelfHarm"] = "高スコア：自傷・自殺",
            ["HighScore_SelfHarmIntent"] = "高スコア：自傷・自殺（意図）",
            ["HighScore_SelfHarmInstructions"] = "高スコア：自傷・自殺（手順）",
            ["HighScore_Sexual"] = "高スコア：性的コンテンツ",
            ["HighScore_SexualMinors"] = "高スコア：未成年への性的コンテンツ",
            ["HighScore_Violence"] = "高スコア：暴力",
            ["HighScore_ViolenceGraphic"] = "高スコア：暴力（過激な描写含む）",
        };

        private static string ToJapanese(string key)
            => KeyLabels.TryGetValue(key, out var label) ? label : key;

        // --- ヘルパー ---
        private static string Flag(bool value) => value ? "[YES]" : "[ no]";
        private static string Score(double value) {
            int filled = (int)(value * 10);
            var bar = new string('#', filled) + new string('-', 10 - filled);
            return $"{value:F4}  [{bar}]";
        }
    }
}
