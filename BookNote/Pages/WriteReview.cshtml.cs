using BookNote.Scripts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using ReverseMarkdown;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookNote.Pages
{
    public class WriteReviewModel : PageModel {
        private readonly ILogger<WriteReviewModel> _logger;
        private readonly IConfiguration _configuration;

        public WriteReviewModel(ILogger<WriteReviewModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public ReviewInputModel ReviewInput { get; set; } = new();

        public void OnGet() {
            // ページ初期表示
        }

        public IActionResult OnPostSaveDraft() {
            if (!ModelState.IsValid) {
                return Page();
            }

            // HTML → Markdown変換
            var converter = new Converter();
            ReviewInput.ContentMarkdown = converter.Convert(ReviewInput.ContentHtml);

            // Markdownをログ出力
            _logger.LogInformation("=== 下書き保存 ===");
            _logger.LogInformation("ContentHtml: {ContentHtml}", ReviewInput.ContentHtml);
            _logger.LogInformation("ContentMarkdown: {ContentMarkdown}", ReviewInput.ContentMarkdown);
            _logger.LogInformation("==================");

            // 下書き保存処理
            // TODO: データベースに下書きとして保存
            // 例: _reviewService.SaveDraft(ReviewInput);

            TempData["Message"] = "下書きを保存しました";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPublish() {
            if (!ModelState.IsValid) {
                return Page();
            }

            // HTML → Markdown変換
            var converter = new Converter();
            ReviewInput.ContentMarkdown = converter.Convert(ReviewInput.ContentHtml);

            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();

                    // 仮ユーザーID（実際はセッションから取得）
                    var userId = "550e8400-e29b-41d4-a716-446655440000";

                    // 仮ISBN（実際は書籍選択から取得）
                    var isbn = "9784000000000";

                    var sql = @"INSERT INTO BookReview 
                       (User_Id, ISBN, Rating, IsSpoilers, Title, Review) 
                       VALUES 
                       (:UserId, :ISBN, :Rating, :IsSpoiler, :Title, :Review)";

                    using (var command = new OracleCommand(sql, connection)) {
                        command.Parameters.Add(":UserId", OracleDbType.Char).Value = userId;
                        command.Parameters.Add(":ISBN", OracleDbType.Char).Value = isbn;
                        command.Parameters.Add(":Rating", OracleDbType.Int32).Value = ReviewInput.Rating;
                        command.Parameters.Add(":IsSpoilers", OracleDbType.Int32).Value = ReviewInput.ContainsSpoiler ? 1 : 0;
                        command.Parameters.Add(":Title", OracleDbType.Varchar2).Value = ReviewInput.Title;
                        command.Parameters.Add(":Review", OracleDbType.NClob).Value = ReviewInput.ContentMarkdown;

                        await command.ExecuteNonQueryAsync();
                    }

                    _logger.LogInformation("レビュー登録成功");
                }

                return RedirectToPage("/Reviews");
            } catch (Exception ex) {
                _logger.LogError(ex, "レビュー登録エラー");
                ModelState.AddModelError(string.Empty, "レビューの投稿に失敗しました");
                return Page();
            }
        }
    }

    public class ReviewInputModel {
        [Required(ErrorMessage = "書籍を選択してください")]
        public string BookTitle { get; set; } = string.Empty;

        public string BookAuthor { get; set; } = string.Empty;

        [Required(ErrorMessage = "レビュータイトルを入力してください")]
        [StringLength(20, ErrorMessage = "レビュータイトルは20文字以内で入力してください")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "評価を選択してください")]
        [Range(1, 5, ErrorMessage = "評価は1〜5の間で選択してください")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "レビュー本文を入力してください")]
        public string ContentHtml { get; set; } = string.Empty;

        // Markdownはサーバー側で生成するのでRequiredを外す
        public string ContentMarkdown { get; set; } = string.Empty;

        public bool ContainsSpoiler { get; set; }

    }
}
