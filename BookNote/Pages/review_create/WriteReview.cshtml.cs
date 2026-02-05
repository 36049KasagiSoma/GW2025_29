using BookNote.Scripts;
using BookNote.Scripts.Login;
using Markdig;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using ReverseMarkdown;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookNote.Pages.review_create {
    public class WriteReviewModel : PageModel {
        private readonly ILogger<WriteReviewModel> _logger;
        private readonly IConfiguration _configuration;


        public WriteReviewModel(ILogger<WriteReviewModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public int? ReviewId { get; set; }
        [BindProperty]
        public ReviewInputModel? ReviewInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync() {
            if (ReviewId.HasValue) {
                await LoadReviewAsync(ReviewId.Value);
                if (ReviewInput == null) {
                    Response.Redirect("/review_create/WriteReview");
                }
            }
            return Page();
        }

        private async Task LoadReviewAsync(int reviewId) {
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    var userId = AccountController.GetUserId();

                    var sql = @"SELECT R.REVIEW_ID, R.USER_ID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER,
                               R.RATING, R.ISSPOILERS, R.TITLE AS REVIEW_TITLE, R.REVIEW
                        FROM BOOKREVIEW R
                        LEFT JOIN BOOKS B ON R.ISBN = B.ISBN
                        WHERE R.REVIEW_ID = :ReviewId";

                    using (var command = new OracleCommand(sql, connection)) {
                        command.BindByName = true;

                        command.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = reviewId;
                        using (var reader = await command.ExecuteReaderAsync()) {
                            if (await reader.ReadAsync()) {
                                if (reader.GetString(reader.GetOrdinal("USER_ID")) != userId) {
                                    ReviewInput = null;
                                    return;
                                }
                                ReviewInput.BookIsbn = reader.GetString(reader.GetOrdinal("ISBN")).Trim();
                                ReviewInput.BookTitle = reader.IsDBNull(reader.GetOrdinal("TITLE"))
                                    ? ""
                                    : reader.GetString(reader.GetOrdinal("TITLE")).Trim();
                                ReviewInput.BookAuthor = reader.IsDBNull(reader.GetOrdinal("AUTHOR"))
                                    ? ""
                                    : reader.GetString(reader.GetOrdinal("AUTHOR")).Trim();
                                ReviewInput.BookPublisher = reader.IsDBNull(reader.GetOrdinal("PUBLISHER"))
                                    ? ""
                                    : reader.GetString(reader.GetOrdinal("PUBLISHER")).Trim();
                                ReviewInput.Title = reader.IsDBNull(reader.GetOrdinal("REVIEW_TITLE"))
                                    ? ""
                                    : reader.GetString(reader.GetOrdinal("REVIEW_TITLE")).Trim();
                                ReviewInput.Rating = reader.IsDBNull(reader.GetOrdinal("RATING"))
                                    ? 0
                                    : reader.GetInt32(reader.GetOrdinal("RATING"));
                                ReviewInput.ContainsSpoiler = reader.IsDBNull(reader.GetOrdinal("ISSPOILERS"))
                                    ? false
                                    : reader.GetInt32(reader.GetOrdinal("ISSPOILERS")) == 1;
                                ReviewInput.ContentMarkdown = reader.IsDBNull(reader.GetOrdinal("REVIEW"))
                                     ? ""
                                     : reader.GetString(reader.GetOrdinal("REVIEW")).Trim();

                                // Markdown → HTML変換（Quill編集用）
                                ReviewInput.ContentHtml = ConvertMarkdownToQuillHtml(ReviewInput.ContentMarkdown);

                                ReviewInput.ContentHtml = Markdown.ToHtml(ReviewInput.ContentMarkdown);
                                _logger.LogInformation(ReviewInput.ContentHtml);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "レビュー読み込みエラー");
            }
        }

        private string ConvertMarkdownToQuillHtml(string markdown) {
            if (string.IsNullOrWhiteSpace(markdown)) {
                return "";
            }

            // Markdigのパイプライン設定
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var html = Markdown.ToHtml(markdown, pipeline);
            return html;
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

        private async Task<bool> SaveReviewAsync(bool isDraft) {
            if (!ModelState.IsValid) {
                return false;
            }

            var converter = new Converter(new Config {
                UnknownTags = Config.UnknownTagsOption.PassThrough,
                GithubFlavored = true,
                SmartHrefHandling = true
            });
            ReviewInput.ContentMarkdown = converter.Convert(ReviewInput.ContentHtml);

            if (isDraft) {
                _logger.LogInformation("=== 下書き保存 ===");
                _logger.LogInformation("ContentHtml: {ContentHtml}", ReviewInput.ContentHtml);
                _logger.LogInformation("ContentMarkdown: {ContentMarkdown}", ReviewInput.ContentMarkdown);
                _logger.LogInformation("==================");
            }

            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();
                    await EnsureBookExistsAsync(connection, ReviewInput.BookIsbn);

                    string sql;
                    if (ReviewId.HasValue) {
                        // 既存レビューの更新
                        if (isDraft) {
                            sql = @"UPDATE BookReview 
                               SET Rating = :Rating, ISSPOILERS = :IsSpoilers, Title = :Title, Review = :Review, PostingTime = NULL
                               WHERE Review_Id = :ReviewId";
                        } else {
                            sql = @"UPDATE BookReview 
                               SET Rating = :Rating, ISSPOILERS = :IsSpoilers, Title = :Title, Review = :Review, PostingTime = SYSTIMESTAMP
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
                               (:UserId, :ISBN, :Rating, :IsSpoilers, :Title, :Review, SYSTIMESTAMP)";
                        }
                    }


                    using (var command = new OracleCommand(sql, connection)) {
                        command.BindByName = true;
                        // 先にUPDATE/INSERT共通のパラメータを設定
                        command.Parameters.Add(":Rating", OracleDbType.Int32).Value = ReviewInput.Rating;
                        command.Parameters.Add(":IsSpoilers", OracleDbType.Int32).Value = ReviewInput.ContainsSpoiler ? 1 : 0;
                        command.Parameters.Add(":Title", OracleDbType.Varchar2).Value =
                            string.IsNullOrEmpty(ReviewInput.Title) ? DBNull.Value : ReviewInput.Title;
                        command.Parameters.Add(":Review", OracleDbType.Clob).Value =
                            string.IsNullOrEmpty(ReviewInput.ContentMarkdown) ? DBNull.Value : ReviewInput.ContentMarkdown;
                        if (ReviewId.HasValue) {
                            command.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = ReviewId.Value;
                        } else {
                            command.Parameters.Add(":UserId", OracleDbType.Char).Value = AccountController.GetUserId();
                            command.Parameters.Add(":ISBN", OracleDbType.Char).Value = ReviewInput.BookIsbn;
                        }

                        await command.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation("レビュー登録成功");
                }
                return true;
            } catch (Exception ex) {
                _logger.LogError(ex, "レビュー登録エラー");
                ModelState.AddModelError(string.Empty, "レビューの投稿に失敗しました");
                return false;
            }
        }

        public async Task<IActionResult> OnPostSaveDraftAsync() {
            _logger.LogInformation("下書き保存処理開始 - ReviewId: {ReviewId}", ReviewId);

            if (await SaveReviewAsync(isDraft: true)) {
                return RedirectToPage("/review_create/SelectType");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostPublish() {
            _logger.LogInformation("投稿処理開始 - ReviewId: {ReviewId}", ReviewId);

            if (await SaveReviewAsync(isDraft: false)) {
                return RedirectToPage("/Reviews");
            }
            return Page();
        }
    }


    public class ReviewInputModel {
        private string _bookTitle = string.Empty;
        private string _bookAuthor = string.Empty;
        private string _bookPublisher = string.Empty;
        private string _title = string.Empty;

        [Required(ErrorMessage = "書籍を選択してください")]
        public string BookIsbn { get; set; } = string.Empty;

        [Required(ErrorMessage = "書籍を選択してください")]
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

        [Required(ErrorMessage = "レビュータイトルを入力してください")]
        [StringLength(100, ErrorMessage = "レビュータイトルは100文字以内で入力してください")]
        public string Title {
            get => _title;
            set => _title = TruncateByByteLength(value, 100);
        }

        [Required(ErrorMessage = "評価を選択してください")]
        [Range(1, 5, ErrorMessage = "評価は1〜5の間で選択してください")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "レビュー本文を入力してください")]
        public string ContentHtml { get; set; } = string.Empty;

        public string ContentMarkdown { get; set; } = string.Empty;

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

            // デコードエラーで末尾に (U+FFFD) が含まれる場合は1文字削る
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
          ContentMarkdown = ""{(ContentMarkdown.Length > 50 ? ContentMarkdown.Substring(0, 50) + "..." : ContentMarkdown)}"",
          ContainsSpoiler = {ContainsSpoiler}
        }}";
        }
    }
}