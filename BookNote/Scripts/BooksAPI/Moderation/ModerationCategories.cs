using System.Text.Json.Serialization;

namespace BookNote.Scripts.BooksAPI.Moderation {
    /// <summary>
    /// 各カテゴリの違反フラグ
    /// </summary>
    public class ModerationCategories {
        public bool Hate { get; init; }

        [JsonPropertyName("hate/threatening")]
        public bool HateThreatening { get; init; }

        public bool Harassment { get; init; }

        [JsonPropertyName("harassment/threatening")]
        public bool HarassmentThreatening { get; init; }

        [JsonPropertyName("self-harm")]
        public bool SelfHarm { get; init; }

        [JsonPropertyName("self-harm/intent")]
        public bool SelfHarmIntent { get; init; }

        [JsonPropertyName("self-harm/instructions")]
        public bool SelfHarmInstructions { get; init; }

        public bool Sexual { get; init; }

        [JsonPropertyName("sexual/minors")]
        public bool SexualMinors { get; init; }

        public bool Violence { get; init; }

        [JsonPropertyName("violence/graphic")]
        public bool ViolenceGraphic { get; init; }
    }
}
