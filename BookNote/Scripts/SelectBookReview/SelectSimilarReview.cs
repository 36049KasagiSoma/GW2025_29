using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Scripts.SelectBookReview {
    public class SelectSimilarReview : SelectBookReviewBace {
        public SelectSimilarReview(OracleConnection conn, string? myId) : base(conn, myId) {
        }
        public override async Task<List<BookReview>> GetReview() {
            return await GetReview(10,1); //指定されなかったら仮で1
        }
        public async Task<List<BookReview>> GetReview(int limit, int reviewId) {
            // 基準レビューのEmbeddingを取得
            const string embeddingSql = @"
        SELECT EMBEDDING FROM BOOKREVIEW WHERE REVIEW_ID = :reviewId";

            string? embeddingJson = null;
            using (var embCmd = new OracleCommand(embeddingSql, _conn)) {
                embCmd.BindByName = true;
                embCmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
                var result = await embCmd.ExecuteScalarAsync();
                embeddingJson = result == DBNull.Value ? null : result?.ToString();
            }

            if (string.IsNullOrEmpty(embeddingJson))
                return new List<BookReview>();

            var baseVector = System.Text.Json.JsonSerializer.Deserialize<float[]>(embeddingJson)!;

            // Embeddingが存在する全レビューを取得（基準レビュー除外）
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW, R.EMBEDDING
                FROM BOOKREVIEW R, USERS U, BOOKS B
                WHERE R.USER_ID = U.USER_ID 
                  AND R.ISBN = B.ISBN 
                  AND R.POSTINGTIME IS NOT NULL
                  AND R.EMBEDDING IS NOT NULL
                  AND R.REVIEW_ID != :reviewId
                  AND (:loginUserId IS NULL OR NOT EXISTS (
                      SELECT 1 
                      FROM USERBLOCK BL 
                      WHERE BL.TO_USER_ID = :loginUserId 
                        AND BL.FOR_USER_ID = R.USER_ID
                  ))";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value =
                string.IsNullOrEmpty(_myId) ? (object)DBNull.Value : _myId;

            var allReviews = await GetListFromSql(cmd);
            // コサイン類似度で上位limit件を返す
            return allReviews
                .Select(r => {
                    float[]? vec = null;
                    try {
                        // GetListFromSqlがEMBEDDINGをReviewプロパティに入れている場合を考慮
                        // EMBEDDINGは別途取得が必要な場合はここで処理
                        vec = r.Embedding is not null
                            ? System.Text.Json.JsonSerializer.Deserialize<float[]>(r.Embedding)
                            : null;
                    } catch { }
                    return (review: r, vector: vec);
                })
                .Where(x => x.vector != null)
                .OrderByDescending(x => CosineSimilarity(baseVector, x.vector!))
                .Take(limit)
                .Select(x => x.review)
                .ToList();
        }

        public static double CosineSimilarity(float[] v1, float[] v2) {
            if (v1.Length != v2.Length)
                throw new ArgumentException("Vector size mismatch");

            double dot = 0.0;
            double norm1 = 0.0;
            double norm2 = 0.0;

            for (int i = 0; i < v1.Length; i++) {
                dot += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }

            return dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }
    }
}
