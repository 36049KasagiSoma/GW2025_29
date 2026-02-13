using System.Text.Json.Serialization;

namespace BookNote.Scripts.BooksAPI.Moderation {
    /// <summary>
    /// 1 件分の判定結果
    /// </summary>
    public class ModerationResult {
        /// <summary>有害コンテンツが検出されたか</summary>
        public bool Flagged { get; init; }

        /// <summary>カテゴリごとのフラグ（true = 違反）</summary>
        public ModerationCategories Categories { get; init; } = new();

        /// <summary>カテゴリごとのスコア（0.0 - 1.0）</summary>
        [JsonPropertyName("category_scores")]
        public ModerationCategoryScores CategoryScores { get; init; } = new();
    }
}
