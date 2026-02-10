using BookNote.Scripts;
using BookNote.Scripts.UserControl;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Pages {
    public class ReviewCommentsModel : PageModel {
        private readonly OracleConnection _conn;
        private readonly UserIconGetter _userIconGetter;

        public int ReviewId { get; set; }
        public string ReviewTitle { get; set; } = string.Empty;
        public List<CommentData> Comments { get; set; } = new();
        public int TotalComments { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;

        public ReviewCommentsModel(OracleConnection conn) {
            _conn = conn;
            _userIconGetter = new UserIconGetter();
        }

        public async Task<IActionResult> OnGetAsync(int reviewId, int page = 1) {
            ReviewId = reviewId;
            CurrentPage = page;

            if (_conn.State != ConnectionState.Open) {
                await _conn.OpenAsync();
            }

            // レビュータイトル取得
            var titleQuery = "SELECT Title FROM BookReview WHERE Review_Id = :ReviewId";
            using (var command = new OracleCommand(titleQuery, _conn)) {
                command.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                var result = await command.ExecuteScalarAsync();
                ReviewTitle = result?.ToString() ?? "レビュー";
            }

            // 総コメント数取得
            var countQuery = "SELECT COUNT(*) FROM ReviewComment WHERE Review_Id = :ReviewId";
            using (var command = new OracleCommand(countQuery, _conn)) {
                command.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                TotalComments = Convert.ToInt32(await command.ExecuteScalarAsync());
            }

            TotalPages = (int)Math.Ceiling((double)TotalComments / PageSize);

            // ページング用のコメント取得
            var offset = (CurrentPage - 1) * PageSize;
            var query = @"
                SELECT * FROM (
                    SELECT 
                        rc.Comment_Id,
                        rc.Comment_Text,
                        rc.PostingTime,
                        u.User_Name,
                        u.User_PublicId,
                        ROW_NUMBER() OVER (ORDER BY rc.PostingTime ASC) AS rn
                    FROM ReviewComment rc
                    INNER JOIN Users u ON rc.User_Id = u.User_Id
                    WHERE rc.Review_Id = :ReviewId
                )
                WHERE rn > :Offset AND rn <= :EndRow";

            using (var command = new OracleCommand(query, _conn)) {
                command.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                command.Parameters.Add(new OracleParameter("Offset", offset));
                command.Parameters.Add(new OracleParameter("EndRow", offset + PageSize));

                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        Comments.Add(new CommentData {
                            CommentId = Convert.ToInt32(reader["Comment_Id"]),
                            CommentText = reader.GetOracleClob(reader.GetOrdinal("Comment_Text")).Value,
                            PostingTime = reader.GetDateTime(reader.GetOrdinal("PostingTime")),
                            PostingTimeDisplay = StaticEvent.FormatPostingTime(reader.GetDateTime(reader.GetOrdinal("PostingTime"))),
                            UserName = reader["User_Name"].ToString() ?? "",
                            UserPublicId = reader["User_PublicId"].ToString() ?? ""
                        });
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnGetUserIconAsync(string publicId) {
            byte[]? imageData = await _userIconGetter.GetIconImageData(publicId, UserIconGetter.IconSize.SMALL);
            if (imageData != null && imageData.Length > 0) {
                return File(imageData, "image/jpeg");
            }
            return NotFound();
        }

        public class CommentData {
            public int CommentId { get; set; }
            public string CommentText { get; set; } = string.Empty;
            public DateTime PostingTime { get; set; }
            public string PostingTimeDisplay { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string UserPublicId { get; set; } = string.Empty;
        }
    }
}