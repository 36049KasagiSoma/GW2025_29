using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Controllers {
    [Route("api/reviews/{reviewId}/comments")]
    [ApiController]
    public class ReviewCommentController : ControllerBase {
        private readonly OracleConnection _conn;

        public ReviewCommentController(OracleConnection conn) {
            _conn = conn;
        }

        // コメント一覧取得（レビューページ用 - 最新5件）
        [HttpGet]
        public async Task<IActionResult> GetComments(int reviewId, [FromQuery] int limit = 5) {
            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                var query = @"
                    SELECT * FROM (
                        SELECT 
                            rc.Comment_Id,
                            rc.Comment_Text,
                            rc.PostingTime,
                            u.User_Name,
                            u.User_PublicId
                        FROM ReviewComment rc
                        INNER JOIN Users u ON rc.User_Id = u.User_Id
                        WHERE rc.Review_Id = :ReviewId
                        ORDER BY rc.PostingTime DESC
                    )
                    WHERE ROWNUM <= :Limit";

                var comments = new List<object>();

                using (var command = new OracleCommand(query, _conn)) {
                    command.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                    command.Parameters.Add(new OracleParameter("Limit", limit));

                    using (var reader = await command.ExecuteReaderAsync()) {
                        while (await reader.ReadAsync()) {
                            comments.Add(new {
                                commentId = Convert.ToInt32(reader["Comment_Id"]),
                                commentText = reader.GetOracleClob(reader.GetOrdinal("Comment_Text")).Value,
                                postingTime = reader.GetDateTime(reader.GetOrdinal("PostingTime")),
                                userName = reader["User_Name"].ToString(),
                                userPublicId = reader["User_PublicId"].ToString()
                            });
                        }
                    }
                }

                // 最新順にソート（ROWNUM後に逆順にする）
                comments.Reverse();

                return Ok(comments);
            } catch (Exception ex) {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // コメント総数取得
        [HttpGet("count")]
        public async Task<IActionResult> GetCommentCount(int reviewId) {
            try {
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                var query = "SELECT COUNT(*) FROM ReviewComment WHERE Review_Id = :ReviewId";

                using (var command = new OracleCommand(query, _conn)) {
                    command.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return Ok(new { count });
                }
            } catch (Exception ex) {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // コメント投稿
        [HttpPost]
        public async Task<IActionResult> PostComment(int reviewId, [FromBody] CommentRequest request) {
            // 認証チェック
            if (!AccountDataGetter.IsAuthenticated()) {
                return Unauthorized(new { error = "ログインが必要です" });
            }

            if (string.IsNullOrWhiteSpace(request.CommentText)) {
                return BadRequest(new { error = "コメントを入力してください" });
            }

            if (request.CommentText.Length > 1000) {
                return BadRequest(new { error = "コメントは1000文字以内で入力してください" });
            }

            try {
                var userId = AccountDataGetter.GetUserId();
                if (string.IsNullOrEmpty(userId)) {
                    return Unauthorized(new { error = "ユーザー情報が取得できません" });
                }

                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                var query = @"
                    INSERT INTO ReviewComment (Review_Id, User_Id, Comment_Text, PostingTime)
                    VALUES (:ReviewId, :UserId, :CommentText, SYSTIMESTAMP AT TIME ZONE 'Asia/Tokyo')
                    RETURNING Comment_Id INTO :CommentId";

                int commentId;

                using (var command = new OracleCommand(query, _conn)) {
                    command.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                    command.Parameters.Add(new OracleParameter("UserId", userId));
                    command.Parameters.Add(new OracleParameter("CommentText", request.CommentText));

                    var commentIdParam = new OracleParameter("CommentId", OracleDbType.Int32) {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(commentIdParam);

                    await command.ExecuteNonQueryAsync();
                    commentId = Convert.ToInt32(commentIdParam.Value.ToString());
                }

                var userName = await AccountDataGetter.GetDbUserNameAsync();
                var userPublicId = AccountDataGetter.GetDbUserPublicIdAsync();
                if (AccountDataGetter.IsAuthenticated())
                    ActivityTracer.LogActivity(ActivityType.WRITE_COMMENT, AccountDataGetter.GetUserId(), reviewId.ToString());

                return Ok(new {
                    commentId,
                    commentText = request.CommentText,
                    postingTime = DateTime.Now,
                    userName,
                    userPublicId
                });
            } catch (Exception ex) {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // コメント削除（自分のコメントのみ）
        [HttpDelete("{commentId}")]
        public async Task<IActionResult> DeleteComment(int reviewId, int commentId) {
            if (!AccountDataGetter.IsAuthenticated()) {
                return Unauthorized(new { error = "ログインが必要です" });
            }

            try {
                var userId = AccountDataGetter.GetUserId();
                if (_conn.State != ConnectionState.Open) {
                    await _conn.OpenAsync();
                }

                // 自分のコメントかチェック
                var checkQuery = @"
                    SELECT COUNT(*) 
                    FROM ReviewComment 
                    WHERE Comment_Id = :CommentId 
                    AND Review_Id = :ReviewId 
                    AND User_Id = :UserId";

                using (var checkCommand = new OracleCommand(checkQuery, _conn)) {
                    checkCommand.Parameters.Add(new OracleParameter("CommentId", commentId));
                    checkCommand.Parameters.Add(new OracleParameter("ReviewId", reviewId));
                    checkCommand.Parameters.Add(new OracleParameter("UserId", userId));

                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (count == 0) {
                        return Forbid();
                    }
                }

                // 削除実行
                var deleteQuery = "DELETE FROM ReviewComment WHERE Comment_Id = :CommentId";
                using (var deleteCommand = new OracleCommand(deleteQuery, _conn)) {
                    deleteCommand.Parameters.Add(new OracleParameter("CommentId", commentId));
                    await deleteCommand.ExecuteNonQueryAsync();
                }

                return Ok(new { message = "削除しました" });
            } catch (Exception ex) {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class CommentRequest {
            public string CommentText { get; set; } = string.Empty;
        }
    }
}