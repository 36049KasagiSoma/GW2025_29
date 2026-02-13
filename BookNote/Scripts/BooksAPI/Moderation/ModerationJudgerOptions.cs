namespace BookNote.Scripts.BooksAPI.Moderation
{
    public class ModerationJudgerOptions
    {
        // ============================================================
        // ブロックフラグ（true = OpenAI のカテゴリフラグが true の場合にブロック）
        // ============================================================

        /// <summary>
        /// ヘイトスピーチ（差別的・憎悪表現）のブロック
        /// </summary>
        public bool IsBlockHate { get; set; } = true;

        /// <summary>
        /// 差別的脅迫のブロック
        /// </summary>
        public bool IsBlockHateThreatening { get; set; } = true;

        /// <summary>
        /// ハラスメント（侮辱・人格攻撃）のブロック
        /// </summary>
        public bool IsBlockHarassment { get; set; } = false;

        /// <summary>
        /// 脅迫を含むハラスメントのブロック
        /// </summary>
        public bool IsBlockHarassmentThreatening { get; set; } = true;

        /// <summary>
        /// 自傷・自殺に関する内容のブロック
        /// </summary>
        public bool IsBlockSelfHarm { get; set; } = false;

        /// <summary>
        /// 自傷・自殺の意図が明確な内容のブロック
        /// </summary>
        public bool IsBlockSelfHarmIntent { get; set; } = true;

        /// <summary>
        /// 自傷・自殺の具体的な手順を含む内容のブロック
        /// </summary>
        public bool IsBlockSelfHarmInstructions { get; set; } = true;

        /// <summary>
        /// 性的コンテンツのブロック
        /// </summary>
        public bool IsBlockSexual { get; set; } = false;

        /// <summary>
        /// 未成年への性的コンテンツのブロック
        /// </summary>
        public bool IsBlockSexualMinors { get; set; } = false;

        /// <summary>
        /// 暴力的表現のブロック
        /// </summary>
        public bool IsBlockViolence { get; set; } = false;

        /// <summary>
        /// 過激な暴力描写のブロック
        /// </summary>
        public bool IsBlockViolenceGraphic { get; set; } = true;

        // ============================================================
        // スコアしきい値（OpenAI フラグが false でもスコアがこの値以上でブロック）
        // 0.0 〜 1.0。値が低いほど厳しく、高いほど緩くなる。
        // null を指定するとスコアによるブロックを無効化。
        // ============================================================

        /// <summary>
        /// ヘイトスピーチのブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? HateScoreThreshold { get; set; } = 1.0;

        /// <summary>
        /// 差別的脅迫のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? HateThreateningScoreThreshold { get; set; } = 1.0;

        /// <summary>
        /// 強い侮辱・人格攻撃（執拗な悪口など）のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? HarassmentScoreThreshold { get; set; } = 0.85;

        /// <summary>
        /// 脅迫を含むハラスメントのブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? HarassmentThreateningScoreThreshold { get; set; } = 1.0;

        /// <summary>
        /// 自傷・自殺に関する内容のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? SelfHarmScoreThreshold { get; set; } = 0.90;

        /// <summary>
        /// 自傷・自殺の意図が明確な内容のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? SelfHarmIntentScoreThreshold { get; set; } = 1.0;

        /// <summary>
        /// 自傷・自殺の具体的な手順を含む内容のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? SelfHarmInstructionsScoreThreshold { get; set; } = 1.0;

        /// <summary>
        /// 露骨な性的コンテンツのブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? SexualScoreThreshold { get; set; } = 0.85;

        /// <summary>
        /// 未成年への性的コンテンツのブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? SexualMinorsScoreThreshold { get; set; } = 1.0;

        /// <summary>
        /// 暴力的表現（危害を示唆する内容）のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? ViolenceScoreThreshold { get; set; } = 0.90;

        /// <summary>
        /// 過激な暴力描写のブロック閾値（0.0 〜 1.0）
        /// </summary>
        public double? ViolenceGraphicScoreThreshold { get; set; } = 1.0;
    }
}