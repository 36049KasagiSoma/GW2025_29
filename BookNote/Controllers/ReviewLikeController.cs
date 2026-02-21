using BookNote.Scripts;
using BookNote.Scripts.ActivityTrace;
using BookNote.Scripts.Login;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Controllers {
    [ApiController]
    [Route("api/reviews")]
    public class ReviewLikeController : ControllerBase {
        IConfiguration _configuration;
        public ReviewLikeController(IConfiguration configuration) {
            _configuration = configuration;
        }

        [HttpGet("{reviewId}/like")]
        public async Task<IActionResult> GetLikeStatus(int reviewId) {
            var isLiked = false;
            var likeCount = 0;
            var userId = AccountDataGetter.GetUserId(); // 未ログイン時はnull

            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                await connection.OpenAsync();

                // いいね総数を取得
                using (var countCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId",
                    connection)) {
                    countCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    likeCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                // ログイン済みの場合のみいいね済みチェック
                if (userId != null) {
                    using (var likeCmd = new OracleCommand(
                        "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                        connection)) {
                        likeCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                        likeCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                        isLiked = Convert.ToInt32(await likeCmd.ExecuteScalarAsync()) > 0;
                    }
                }
            }

            return Ok(new { isLiked, likeCount });
        }

        [HttpPost("{reviewId}/like")]
        public async Task<IActionResult> ToggleLike(int reviewId) {
            var userId = AccountDataGetter.GetUserId();

            // 未ログインチェック
            if (userId == null) {
                return Unauthorized();
            }

            var isLiked = false;
            var likeCount = 0;

            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                await connection.OpenAsync();

                // 自分のレビューへのいいねを拒否
                using (var ownerCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM BookReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                    connection)) {
                    ownerCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    ownerCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                    var isOwner = Convert.ToInt32(await ownerCmd.ExecuteScalarAsync()) > 0;
                    if (isOwner) {
                        return StatusCode(403, new { error = "自分のレビューにはいいねできません" });
                    }
                }

                // 既存のいいねを確認
                using (var checkCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                    connection)) {
                    checkCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    checkCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists) {
                        using (var deleteCmd = new OracleCommand(
                            "DELETE FROM GoodReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                            connection)) {
                            deleteCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                            deleteCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        ActivityTracer.LogActivity(ActivityType.UN_GOOD_THE_REVIEW, userId, reviewId.ToString());
                        isLiked = false;
                    } else {
                        using (var insertCmd = new OracleCommand(
                            "INSERT INTO GoodReview (User_Id, Review_Id) VALUES (:userId, :reviewId)",
                            connection)) {
                            insertCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                            insertCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                        ActivityTracer.LogActivity(ActivityType.GOOD_THE_REVIEW, userId, reviewId.ToString());
                        isLiked = true;
                    }
                }

                using (var countCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId",
                    connection)) {
                    countCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    likeCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }
            }

            return Ok(new { isLiked, likeCount });
        }
    }
}