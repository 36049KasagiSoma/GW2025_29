using Amazon.Runtime.Internal.Util;
using BookNote.Scripts;
using BookNote.Scripts.Login;
using BookNote.Scripts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace BookNote.Pages
{
    public class SelectTypeModel : PageModel {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SelectTypeModel> _logger;
        public SelectTypeModel(ILogger<SelectTypeModel> logger, IConfiguration configuration) {
            _logger = logger;
            _configuration = configuration;
        }

        public List<BookReview> Drafts { get; set; } = new List<BookReview>();
        public int DraftCount { get; set; }

        public async Task<IActionResult> OnGetAsync() {
            // TODO ログインチェック
            var userId = AccountController.GetUserId();    
            //if (string.IsNullOrEmpty(userId)) {
            //    return RedirectToPage("/Login");
            //}

            Drafts = await GetDraftsAsync(userId);
            _logger.LogInformation(Drafts.Count.ToString());
            DraftCount = Drafts.Count;
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteDraftAsync(int draftId) {
            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                    await connection.OpenAsync();

                    var userId = AccountController.GetUserId();

                    // ユーザー確認と削除
                    var sql = @"DELETE FROM BookReview 
                       WHERE Review_Id = :ReviewId 
                       AND User_Id = :UserId 
                       AND PostingTime IS NULL";

                    using (var command = new OracleCommand(sql, connection)) {
                        command.Parameters.Add(":ReviewId", OracleDbType.Int32).Value = draftId;
                        command.Parameters.Add(":UserId", OracleDbType.Char).Value = userId;

                        var result = await command.ExecuteNonQueryAsync();

                        if (result > 0) {
                            _logger.LogInformation("下書き削除成功: {DraftId}", draftId);
                            return new JsonResult(new { success = true });
                        } else {
                            return new JsonResult(new { success = false, message = "削除権限がありません" });
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "下書き削除エラー");
                return new JsonResult(new { success = false, message = "削除に失敗しました" });
            }
        }

        private async Task<List<BookReview>> GetDraftsAsync(string userId) {
            var list = new List<BookReview>();

            try {
                using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {

                    await connection.OpenAsync();
                    const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, R.ISBN, B.TITLE AS BOOK_TITLE, B.AUTHOR, B.PUBLISHER,
                       R.RATING, R.ISSPOILERS, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R
                LEFT JOIN BOOKS B ON R.ISBN = B.ISBN
                WHERE R.USER_ID = :userId AND R.POSTINGTIME IS NULL
                ORDER BY R.REVIEW_ID DESC";

                    using var cmd = new OracleCommand(sql, connection);
                    cmd.Parameters.Add(new OracleParameter("userId", userId));

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) {
                        list.Add(new BookReview {
                            ReviewId = reader.GetInt32(reader.GetOrdinal("REVIEW_ID")),
                            Title = reader.IsDBNull(reader.GetOrdinal("REVIEW_TITLE"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("REVIEW_TITLE")).Trim(),
                            Review = reader.IsDBNull(reader.GetOrdinal("REVIEW"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("REVIEW")).Trim(),
                            Isbn = reader.IsDBNull(reader.GetOrdinal("ISBN"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("ISBN")).Trim()
                        }
                        );
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex,"下書き取得エラー");
            }

            return list;
        }
    }
}
