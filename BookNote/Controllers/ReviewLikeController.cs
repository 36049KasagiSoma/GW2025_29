using BookNote.Scripts;
using BookNote.Scripts.Login;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using static BookNote.Pages.ReviewDetailsModel;

namespace BookNote.Controllers {
    [ApiController]
    [Route("api/reviews")]
    public class ReviewLikeController : ControllerBase {
        IConfiguration _configuration;
        public ReviewLikeController(IConfiguration configuration) {
            _configuration = configuration;
        }

        // GetLikeStatus メソッド
        [HttpGet("{reviewId}/like")]
        public async Task<IActionResult> GetLikeStatus(int reviewId) {
            var isLiked = false;
            var likeCount = 0;

            UserDataManager userDataManager = new UserDataManager();
            var userId = userDataManager.GetUserId();

            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                await connection.OpenAsync();

                // いいね総数を取得
                using (var countCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId",
                    connection)) {
                    countCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    var result = await countCmd.ExecuteScalarAsync();
                    likeCount = Convert.ToInt32(result);
                }

                using (var likeCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                    connection)) {
                    likeCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    likeCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                    var result = await likeCmd.ExecuteScalarAsync();
                    isLiked = Convert.ToInt32(result) > 0;
                }
            }

            return Ok(new { isLiked, likeCount });
        }

        [HttpPost("{reviewId}/like")]
        public async Task<IActionResult> ToggleLike(int reviewId) {
            UserDataManager userDataManager = new UserDataManager();
            var userId = userDataManager.GetUserId();

            var isLiked = false;
            var likeCount = 0;

            using (var connection = new OracleConnection(Keywords.GetDbConnectionString(_configuration))) {
                await connection.OpenAsync();

                // 既存のいいねを確認
                using (var checkCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM GoodReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                    connection)) {
                    checkCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                    checkCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists) {
                        // いいねを削除
                        using (var deleteCmd = new OracleCommand(
                            "DELETE FROM GoodReview WHERE Review_Id = :reviewId AND User_Id = :userId",
                            connection)) {
                            deleteCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                            deleteCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        isLiked = false;
                    } else {
                        // いいねを追加
                        using (var insertCmd = new OracleCommand(
                            "INSERT INTO GoodReview (User_Id, Review_Id) VALUES (:userId, :reviewId)",
                            connection)) {
                            insertCmd.Parameters.Add(":userId", OracleDbType.Char).Value = userId;
                            insertCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                        isLiked = true;
                    }
                }

                // 更新後の総数を取得
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