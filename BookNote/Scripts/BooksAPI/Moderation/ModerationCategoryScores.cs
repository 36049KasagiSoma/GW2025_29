using System.Text.Json.Serialization;

namespace BookNote.Scripts.BooksAPI.Moderation {
    /// <summary>
    /// 各カテゴリのスコア
    /// </summary>
    public class ModerationCategoryScores {
        public double Hate { get; init; }

        [JsonPropertyName("hate/threatening")]
        public double HateThreatening { get; init; }

        public double Harassment { get; init; }

        [JsonPropertyName("harassment/threatening")]
        public double HarassmentThreatening { get; init; }

        [JsonPropertyName("self-harm")]
        public double SelfHarm { get; init; }

        [JsonPropertyName("self-harm/intent")]
        public double SelfHarmIntent { get; init; }

        [JsonPropertyName("self-harm/instructions")]
        public double SelfHarmInstructions { get; init; }

        public double Sexual { get; init; }

        [JsonPropertyName("sexual/minors")]
        public double SexualMinors { get; init; }

        public double Violence { get; init; }

        [JsonPropertyName("violence/graphic")]
        public double ViolenceGraphic { get; init; }
    }
}
