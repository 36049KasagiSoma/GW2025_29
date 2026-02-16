using BookNote.Scripts;
using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.BooksAPI.Moderation;
using BookNote.Scripts.Login;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

public class WriteReviewModel : PageModel {
    private readonly ILogger<WriteReviewModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly OracleConnection _conn;
    private readonly HtmlSanitizer _sanitizer;

    public WriteReviewModel(ILogger<WriteReviewModel> logger, IConfiguration configuration, OracleConnection conn) {
        _logger = logger;
        _configuration = configuration;
        _conn = conn;
        _sanitizer = new HtmlSanitizer();
        SetupSabutizer();
    }

    private void SetupSabutizer() {
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("u");
        _sanitizer.AllowedTags.Add("s");
        _sanitizer.AllowedTags.Add("h1");
        _sanitizer.AllowedTags.Add("h2");
        _sanitizer.AllowedTags.Add("h3");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
        _sanitizer.AllowedTags.Add("blockquote");
    }

    [BindProperty(SupportsGet = true)]
    public int? ReviewId { get; set; }

    // IsEditModeプロパティを削除し、IsPublishedプロパティを追加
    public bool IsPublished { get; set; } = false;

    [BindProperty]
    public ReviewInputModel? ReviewInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync() {
        if (!AccountDataGetter.IsAuthenticated()) {
            return RedirectToPage("/Login");
        }
        if (ReviewId.HasValue) {
            await LoadReviewAsync(ReviewId.Value);
            if (ReviewInput == null) {
                return RedirectToPage("/review_create/WriteReview");
            }
        }
        return Page();
    }

    private async Task LoadReviewAsync(int reviewId) {
        try {
            if (_conn.State != ConnectionState.Open) {
                await _conn.OpenAsync();
            }
            var userId = AccountDataGetter.GetUserId();

            var sql = @"SELECT R.REVIEW_ID, R.USER_ID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER,
                               R.RATING, R.ISSPOILERS, R.TITLE AS REVIEW_TITLE, R.REVIEW, R.POSTINGTIME
                        FROM BOOKREVIEW R
                        LEFT JOIN BOOKS B ON R.ISBN = B.ISBN
                        WHERE R.REVIEW_ID = :ReviewId";

            using (var command = new OracleCommand(sql, _conn)) {
                command.BindByName = true;
                command.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = reviewId;

                using (var reader = await command.ExecuteReaderAsync()) {
                    if (await reader.ReadAsync()) {
                        if (reader.GetString(reader.GetOrdinal("USER_ID")) != userId) {
                            ReviewInput = null;
                            return;
                        }

                        // PostingTimeがnullでない場合は公開済み
                        IsPublished = !reader.IsDBNull(reader.GetOrdinal("POSTINGTIME"));

                        var isbn = reader.GetString(reader.GetOrdinal("ISBN")).Trim();
                        // 仮のISBNの場合は書籍情報をすべて空にする
                        if (isbn == "0000000000000") {
                            ReviewInput.BookIsbn = "";
                            ReviewInput.BookTitle = "";
                            ReviewInput.BookAuthor = "";
                            ReviewInput.BookPublisher = "";
                        } else {
                            ReviewInput.BookIsbn = isbn;
                            ReviewInput.BookTitle = reader.IsDBNull(reader.GetOrdinal("TITLE"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("TITLE")).Trim();
                            ReviewInput.BookAuthor = reader.IsDBNull(reader.GetOrdinal("AUTHOR"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("AUTHOR")).Trim();
                            ReviewInput.BookPublisher = reader.IsDBNull(reader.GetOrdinal("PUBLISHER"))
                                ? ""
                                : reader.GetString(reader.GetOrdinal("PUBLISHER")).Trim();
                        }

                        ReviewInput.Title = reader.IsDBNull(reader.GetOrdinal("REVIEW_TITLE"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("REVIEW_TITLE")).Trim();
                        ReviewInput.Rating = reader.IsDBNull(reader.GetOrdinal("RATING"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("RATING"));
                        ReviewInput.ContainsSpoiler = reader.IsDBNull(reader.GetOrdinal("ISSPOILERS"))
                            ? false
                            : reader.GetInt32(reader.GetOrdinal("ISSPOILERS")) == 1;
                        ReviewInput.ContentHtml = reader.IsDBNull(reader.GetOrdinal("REVIEW"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("REVIEW")).Trim();
                    }
                }
            }

        } catch (Exception ex) {
            _logger.LogError(ex, "レビュー読み込みエラー");
        }
    }

    private async Task EnsureBookExistsAsync(OracleConnection connection, string isbn) {
        var checkSql = "SELECT COUNT(*) FROM Books WHERE Isbn = :ISBN";
        try {
            using (var checkCommand = new OracleCommand(checkSql, connection)) {
                checkCommand.Parameters.Add(":ISBN", OracleDbType.Char).Value = isbn;
                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (count == 0) {
                    var insertBookSql = @"INSERT INTO Books (Isbn, Title, Author, Publisher) 
                              VALUES (:ISBN, :Title, :Author, :Publisher)";
                    using (var insertBookCommand = new OracleCommand(insertBookSql, connection)) {
                        insertBookCommand.Parameters.Add(":ISBN", OracleDbType.Char).Value = isbn;
                        insertBookCommand.Parameters.Add(":Title", OracleDbType.Varchar2).Value = ReviewInput.BookTitle;
                        insertBookCommand.Parameters.Add(":Author", OracleDbType.Varchar2).Value = ReviewInput.BookAuthor ?? "";
                        insertBookCommand.Parameters.Add(":Publisher", OracleDbType.Varchar2).Value = ReviewInput.BookPublisher ?? "";
                        await insertBookCommand.ExecuteNonQueryAsync();
                    }
                    _logger.LogInformation("書籍を新規登録: {ISBN}", isbn);
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "書籍登録エラー");
        }
    }

    private async Task EnsureDummyBookExistsAsync(OracleConnection connection) {
        var checkSql = "SELECT COUNT(*) FROM Books WHERE Isbn = '0000000000000'";
        try {
            using (var checkCommand = new OracleCommand(checkSql, connection)) {
                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (count == 0) {
                    var insertBookSql = @"INSERT INTO Books (Isbn, Title, Author, Publisher) 
                      VALUES ('0000000000000', '(下書き用ダミー)', '', '')";
                    using (var insertBookCommand = new OracleCommand(insertBookSql, connection)) {
                        await insertBookCommand.ExecuteNonQueryAsync();
                    }
                    _logger.LogInformation("ダミー書籍を登録: 0000000000000");
                }
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "ダミー書籍登録エラー");
        }
    }

    private async Task<bool> SaveReviewAsync(bool isDraft) {
        if (!ModelState.IsValid || ReviewInput == null) {
            return false;
        }

        if (isDraft) {
            _logger.LogInformation("=== 下書き保存 ===");
            _logger.LogInformation("ContentHtml: {ContentHtml}", ReviewInput.ContentHtml);
            _logger.LogInformation("==================");
        } else {
            //公開時は適切な内容かをチェック
            var plainText = StaticEvent.ToPlainText(ReviewInput.ContentHtml) ?? "";
            var apiKey = _configuration["BookNoteKeys:OpenAI:ApiKey"];
            if (apiKey == null) {
                _logger.LogWarning("OpenAI APIキーが設定されていません。モデレーションチェックをスキップします。");
            } else {
                ModerationClient moderationClient = new(apiKey);
                ModerationResponse response = await moderationClient.CheckAsync(plainText);
                ModerationJudger judger = new();
                if (!judger.IsContentSafe(response)) {
                    _logger.LogWarning("レビュー内容がモデレーションにより拒否されました");
                    TempData["Error"] = "レビュー内容に不適切な表現が含まれているため、投稿できません。内容を修正してください。";
                    return false;
                }
            }
        }

        try {
            if (_conn.State != ConnectionState.Open) {
                await _conn.OpenAsync();
            }

            // 下書き保存時かつ仮のISBNの場合は書籍登録をスキップ
            var isbnValue = isDraft && string.IsNullOrWhiteSpace(ReviewInput.BookIsbn)
                ? "0000000000000"
                : ReviewInput.BookIsbn;

            if (isbnValue != "0000000000000") {
                await EnsureBookExistsAsync(_conn, isbnValue);
            } else {
                // 仮のISBNをBooksテーブルに登録（存在しない場合のみ）
                await EnsureDummyBookExistsAsync(_conn);
            }

            string sql;
            if (ReviewId.HasValue) {
                // 既存レビューの更新
                if (isDraft) {
                    sql = @"UPDATE BookReview 
                           SET ISBN = :ISBN, Rating = :Rating, ISSPOILERS = :IsSpoilers, Title = :Title, Review = :Review, PostingTime = NULL
                           WHERE Review_Id = :ReviewId";
                } else {
                    sql = @"UPDATE BookReview 
                           SET ISBN = :ISBN, Rating = :Rating, ISSPOILERS = :IsSpoilers, Title = :Title, Review = :Review, PostingTime = SYSTIMESTAMP AT TIME ZONE 'Asia/Tokyo'
                           WHERE Review_Id = :ReviewId";
                }
            } else {
                // 新規レビューの作成
                if (isDraft) {
                    sql = @"INSERT INTO BookReview 
                           (USER_ID, ISBN, RATING, ISSPOILERS, TITLE, REVIEW, POSTINGTIME) 
                           VALUES 
                           (:UserId, :ISBN, :Rating, :IsSpoilers, :Title, :Review, NULL)";
                } else {
                    sql = @"INSERT INTO BookReview 
                               (USER_ID, ISBN, RATING, ISSPOILERS, TITLE, REVIEW, POSTINGTIME) 
                               VALUES 
                               (:UserId, :ISBN, :Rating, :IsSpoilers, :Title, :Review, SYSTIMESTAMP AT TIME ZONE 'Asia/Tokyo')";
                }
            }

            using (var command = new OracleCommand(sql, _conn)) {
                command.BindByName = true;

                // ISBNの決定
                var isbnValue2 = isDraft && string.IsNullOrWhiteSpace(ReviewInput.BookIsbn)
                    ? "0000000000000"
                    : ReviewInput.BookIsbn;

                // 共通パラメータを設定
                command.Parameters.Add(":Rating", OracleDbType.Int32).Value = ReviewInput.Rating;
                command.Parameters.Add(":IsSpoilers", OracleDbType.Int32).Value = ReviewInput.ContainsSpoiler ? 1 : 0;
                command.Parameters.Add(":Title", OracleDbType.Varchar2).Value =
                    string.IsNullOrEmpty(ReviewInput.Title) ? DBNull.Value : ReviewInput.Title;

                // 保存前にサニタイズ
                ReviewInput.ContentHtml = _sanitizer.Sanitize(ReviewInput.ContentHtml);

                command.Parameters.Add(":Review", OracleDbType.Clob).Value =
                    string.IsNullOrEmpty(ReviewInput.ContentHtml) ? DBNull.Value : ReviewInput.ContentHtml;

                if (ReviewId.HasValue) {
                    // UPDATE時もISBNパラメータを追加
                    command.Parameters.Add(":ISBN", OracleDbType.Char).Value = isbnValue2;
                    command.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = ReviewId.Value;
                } else {
                    // INSERT時
                    command.Parameters.Add(":UserId", OracleDbType.Char).Value = AccountDataGetter.GetUserId();
                    command.Parameters.Add(":ISBN", OracleDbType.Char).Value = isbnValue2;
                }

                if (AccountDataGetter.IsAuthenticated())
                    ActivityTracer.LogActivity(
                        isDraft ? ActivityType.SAVE_DRAFT : ActivityType.WRITE_REVIEW,
                        AccountDataGetter.GetUserId(),
                        ReviewId.HasValue ? ReviewId.Value.ToString() : null);

                await command.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("レビュー登録成功");
            return true;
        } catch (Exception ex) {
            _logger.LogError(ex, "レビュー登録エラー");
            TempData["Error"] = "レビューの投稿に失敗しました";
            return false;
        }
    }

    public async Task<IActionResult> OnPostSaveDraftAsync() {
        _logger.LogInformation("下書き保存処理開始 - ReviewId: {ReviewId}", ReviewId);

        // 下書き保存時はモデル検証をスキップ
        ModelState.Clear();

        if (await SaveReviewAsync(isDraft: true)) {
            return RedirectToPage("/review_create/SelectType");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostPublish() {
        _logger.LogInformation("投稿処理開始 - ReviewId: {ReviewId}", ReviewId);

        // 編集時（ReviewIdがある場合）は、まずDBから公開状態を確認
        bool isPublishedReview = false;
        if (ReviewId.HasValue) {
            isPublishedReview = await CheckIfPublishedAsync(ReviewId.Value);
        }

        // 公開済みレビューの場合、本文とネタバレ以外の項目をDBから再取得して保持する
        if (isPublishedReview && ReviewId.HasValue) {
            await RestoreNonEditableFieldsAsync(ReviewId.Value);
        }

        // 投稿時のバリデーション（新規投稿または下書きからの投稿の場合のみ）
        if (!isPublishedReview) {
            if (string.IsNullOrWhiteSpace(ReviewInput.BookIsbn) || ReviewInput.BookIsbn == "0000000000000") {
                TempData["Error"] = "書籍を選択してください";
            }
            if (string.IsNullOrWhiteSpace(ReviewInput.Title)) {
                TempData["Error"] = "レビュータイトルを入力してください";
            }
            if (ReviewInput.Rating < 1 || ReviewInput.Rating > 5) {
                TempData["Error"] = "評価を選択してください";
            }
        }
        if (string.IsNullOrWhiteSpace(ReviewInput.ContentHtml)) {
            TempData["Error"] = "レビュー本文を入力してください";
        }

        if (!ModelState.IsValid) {
            return Page();
        }

        if (await SaveReviewAsync(isDraft: false)) {
            if (isPublishedReview && ReviewId.HasValue) {
                TempData["Message"] = "レビューを更新しました";
                return RedirectToPage("/ReviewDetails", new { reviewId = ReviewId.Value });
            }
            return RedirectToPage("/Reviews", new { tab = "myreviews" });
        }
        return Page();
    }

    /// <summary>
    /// 公開済みレビュー編集時に書籍・タイトル・評価をDBから取得して上書きする
    /// </summary>
    private async Task RestoreNonEditableFieldsAsync(int reviewId) {
        try {
            if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();
            var sql = @"SELECT R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER,
                               R.RATING, R.TITLE AS REVIEW_TITLE
                        FROM BOOKREVIEW R
                        LEFT JOIN BOOKS B ON R.ISBN = B.ISBN
                        WHERE R.REVIEW_ID = :ReviewId";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = reviewId;
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync()) {
                ReviewInput.BookIsbn = reader.GetString(reader.GetOrdinal("ISBN")).Trim();
                ReviewInput.BookTitle = reader.IsDBNull(reader.GetOrdinal("TITLE")) ? "" : reader.GetString(reader.GetOrdinal("TITLE")).Trim();
                ReviewInput.BookAuthor = reader.IsDBNull(reader.GetOrdinal("AUTHOR")) ? "" : reader.GetString(reader.GetOrdinal("AUTHOR")).Trim();
                ReviewInput.BookPublisher = reader.IsDBNull(reader.GetOrdinal("PUBLISHER")) ? "" : reader.GetString(reader.GetOrdinal("PUBLISHER")).Trim();
                ReviewInput.Title = reader.IsDBNull(reader.GetOrdinal("REVIEW_TITLE")) ? "" : reader.GetString(reader.GetOrdinal("REVIEW_TITLE")).Trim();
                ReviewInput.Rating = reader.IsDBNull(reader.GetOrdinal("RATING")) ? 0 : reader.GetInt32(reader.GetOrdinal("RATING"));
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "公開済みレビュー：非編集フィールド復元エラー");
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync() {
        if (!AccountDataGetter.IsAuthenticated()) {
            return RedirectToPage("/Login");
        }
        if (!ReviewId.HasValue) {
            return RedirectToPage("/Reviews");
        }

        try {
            if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();

            // 本人確認（他人のレビューを削除できないよう）
            var userId = AccountDataGetter.GetUserId();
            var checkSql = "SELECT COUNT(*) FROM BookReview WHERE Review_Id = :ReviewId AND User_Id = :UserId";
            using (var checkCmd = new OracleCommand(checkSql, _conn)) {
                checkCmd.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = ReviewId.Value;
                checkCmd.Parameters.Add(":UserId", OracleDbType.Char).Value = userId;
                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (count == 0) {
                    return Forbid();
                }
            }

            // コメントを先に削除（外部キー制約対策）
            var deleteCommentsSql = "DELETE FROM ReviewComment WHERE Review_Id = :ReviewId";
            using (var delCommentsCmd = new OracleCommand(deleteCommentsSql, _conn)) {
                delCommentsCmd.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = ReviewId.Value;
                await delCommentsCmd.ExecuteNonQueryAsync();
            }

            // いいねを削除
            var deleteLikesSql = "DELETE FROM GoodReview WHERE Review_Id = :ReviewId";
            using (var delLikesCmd = new OracleCommand(deleteLikesSql, _conn)) {
                delLikesCmd.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = ReviewId.Value;
                await delLikesCmd.ExecuteNonQueryAsync();
            }

            // レビュー本体を削除
            var deleteReviewSql = "DELETE FROM BookReview WHERE Review_Id = :ReviewId AND User_Id = :UserId";
            using (var delCmd = new OracleCommand(deleteReviewSql, _conn)) {
                delCmd.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = ReviewId.Value;
                delCmd.Parameters.Add(":UserId", OracleDbType.Char).Value = userId;
                await delCmd.ExecuteNonQueryAsync();
            }

            _logger.LogInformation("レビュー削除成功 - ReviewId: {ReviewId}", ReviewId.Value);
            return RedirectToPage("/Reviews", new { tab = "myreviews" });

        } catch (Exception ex) {
            _logger.LogError(ex, "レビュー削除エラー - ReviewId: {ReviewId}", ReviewId);
            return RedirectToPage("/ReviewDetails", new { reviewId = ReviewId.Value });
        }
    }
    /// <summary>
    /// レビューが公開済みかどうかをDBから確認
    /// </summary>
    private async Task<bool> CheckIfPublishedAsync(int reviewId) {
        try {
            if (_conn.State != ConnectionState.Open) await _conn.OpenAsync();
            var sql = "SELECT POSTINGTIME FROM BOOKREVIEW WHERE REVIEW_ID = :ReviewId";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = reviewId;
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value;
        } catch (Exception ex) {
            _logger.LogError(ex, "公開状態確認エラー");
            return false;
        }
    }
}

public class ReviewInputModel {
    private string _bookTitle = string.Empty;
    private string _bookAuthor = string.Empty;
    private string _bookPublisher = string.Empty;
    private string _title = string.Empty;

    public string BookIsbn { get; set; } = string.Empty;

    public string BookTitle {
        get => _bookTitle;
        set => _bookTitle = TruncateByByteLength(value, 100);
    }

    public string BookAuthor {
        get => _bookAuthor;
        set => _bookAuthor = TruncateByByteLength(value, 50);
    }

    public string BookPublisher {
        get => _bookPublisher;
        set => _bookPublisher = TruncateByByteLength(value, 30);
    }

    [StringLength(100, ErrorMessage = "レビュータイトルは100文字以内で入力してください")]
    public string Title {
        get => _title;
        set => _title = TruncateByByteLength(value, 100);
    }

    [Range(1, 5, ErrorMessage = "評価は1〜5の間で選択してください")]
    public int Rating { get; set; }

    public string ContentHtml { get; set; } = string.Empty;

    public bool ContainsSpoiler { get; set; }

    /// <summary>
    /// UTF-8バイト長で文字列を切り捨てる（Oracleの VARCHAR2(n BYTE) 対応）
    /// </summary>
    private static string TruncateByByteLength(string input, int maxByteLength) {
        if (string.IsNullOrEmpty(input)) {
            return input;
        }

        var encoding = Encoding.UTF8;
        var bytes = encoding.GetBytes(input);

        if (bytes.Length <= maxByteLength) {
            return input;
        }

        // maxByteLengthを超えないように文字を削る
        var truncatedBytes = new byte[maxByteLength];
        Array.Copy(bytes, truncatedBytes, maxByteLength);

        // 不完全なマルチバイト文字を避けるため、デコード可能な位置まで戻る
        var result = encoding.GetString(truncatedBytes);

        // デコードエラーで末尾に � (U+FFFD) が含まれる場合は1文字削る
        while (result.Contains('\uFFFD') && result.Length > 0) {
            result = result.Substring(0, result.Length - 1);
        }

        return result;
    }

    public override string ToString() {
        return $@"ReviewInputModel {{
          BookIsbn = ""{BookIsbn}"",
          BookTitle = ""{BookTitle}"",
          BookAuthor = ""{BookAuthor}"",
          BookPublisher = ""{BookPublisher}"",
          Title = ""{Title}"",
          Rating = {Rating},
          ContentHtml = ""{(ContentHtml.Length > 50 ? ContentHtml.Substring(0, 50) + "..." : ContentHtml)}"",
          ContainsSpoiler = {ContainsSpoiler}
        }}";
    }
}