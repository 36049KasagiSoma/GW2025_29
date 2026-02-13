namespace BookNote.Scripts.BooksAPI.Moderation {
    /// <summary>
    /// Moderation API のレスポンス全体
    /// </summary>
    public class ModerationResponse {
        /// <summary>リクエスト ID</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>使用モデル名</summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>各テキストの判定結果リスト</summary>
        public ModerationResult[] Results { get; init; } = Array.Empty<ModerationResult>();
    }
}
