using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;
using System.Text.Json;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// Embeddingのコサイン類似度を用いて、類似レビューを取得します。
    /// </summary>
    public class SelectSimilarReview : SelectBookReviewBase {

        public SelectSimilarReview(OracleConnection conn, string? myId) : base(conn, myId) { }

        public override async Task<List<BookReview>> GetReview() => await GetReview(10, 1);

        // ---- 単一レビューId ------------------------------------------------

        public async Task<List<BookReview>> GetReview(int limit, int reviewId) {
            var baseVector = await FetchEmbeddingVector(reviewId);
            if (baseVector is null)
                return new List<BookReview>();

            var candidates = await FetchCandidates(new[] { reviewId });
            return RankBySimilarity(candidates, baseVector, limit);
        }

        // ---- 複数レビューId（指数加重平均ベクトル）------------------------

        /// <summary>
        /// 複数のレビューIdからベースベクトルを生成し、類似レビューを返します。
        /// <paramref name="reviewIds"/> はアクティビティ時間の昇順（古い順）で渡してください。
        /// 末尾（最新）ほど指数的に大きい重みが付きます。
        /// </summary>
        /// <param name="limit">取得件数</param>
        /// <param name="reviewIds">アクティビティ昇順のレビューIdリスト</param>
        /// <param name="decayBase">
        /// 指数減衰の底。デフォルト 2.0（最新が1つ前の2倍の重み）。
        /// 1.0に近いほどフラットな重みになります。
        /// </param>
        public async Task<List<BookReview>> GetReview(int limit, IReadOnlyList<int> reviewIds, double decayBase = 2.0) {
            if (reviewIds is null || reviewIds.Count == 0)
                return new List<BookReview>();

            if (reviewIds.Count == 1)
                return await GetReview(limit, reviewIds[0]);

            // 各レビューのEmbeddingを並列取得
            var vectors = new List<float[]>(); // Embeddingの型に合わせて調整してください
            foreach (var id in reviewIds) {
                var vector = await FetchEmbeddingVector(id);
                if (vector != null)
                    vectors.Add(vector);
            }

            var validPairs = reviewIds
                .Zip(vectors, (id, vec) => (id, vec))
                .Select((pair, idx) => (pair.id, pair.vec, idx))
                .Where(x => x.vec is not null)
                .ToList();

            if (validPairs.Count == 0)
                return new List<BookReview>();

            var baseVector = ComputeWeightedAverageVector(
                validPairs.Select(x => (x.vec!, x.idx)).ToList(),
                decayBase
            );

            var candidates = await FetchCandidates(reviewIds);
            return RankBySimilarity(candidates, baseVector, limit);
        }

        // ---- リストから類似順に並べ替え（DBアクセスなし）-------------------

        /// <summary>
        /// 単一レビューIdを基準に、渡されたリストを類似度順に並べ替えて返します。
        /// 基準レビュー自身はリストから除外されます。
        /// </summary>
        public List<BookReview> SortBySimilarity(IReadOnlyList<BookReview> source, int reviewId, int limit = int.MaxValue) {
            var baseReview = source.FirstOrDefault(r => r.ReviewId == reviewId);
            var baseVector = baseReview is not null ? TryParseEmbedding(baseReview.Embedding) : null;
            if (baseVector is null)
                return new List<BookReview>();

            return RankBySimilarity(
                source.Where(r => r.ReviewId != reviewId).ToList(),
                baseVector,
                limit
            );
        }

        /// <summary>
        /// 複数のレビューIdを基準に、渡されたリストを類似度順に並べ替えて返します。
        /// <paramref name="reviewIds"/> はアクティビティ時間の昇順（古い順）で渡してください。
        /// 末尾（最新）ほど指数的に大きい重みが付きます。
        /// 基準レビュー群はリストから除外されます。
        /// </summary>
        public List<BookReview> SortBySimilarity(IReadOnlyList<BookReview> source, IReadOnlyList<int> reviewIds, int limit = int.MaxValue, double decayBase = 2.0) {
            if (reviewIds is null || reviewIds.Count == 0)
                return new List<BookReview>();

            var idSet = reviewIds.ToHashSet();
            var validPairs = reviewIds
                .Select((id, idx) => {
                    var vec = TryParseEmbedding(source.FirstOrDefault(r => r.ReviewId == id)?.Embedding);
                    return (vec, idx);
                })
                .Where(x => x.vec is not null)
                .ToList();

            if (validPairs.Count == 0)
                return new List<BookReview>();

            var baseVector = ComputeWeightedAverageVector(
                validPairs.Select(x => (x.vec!, x.idx)).ToList(),
                decayBase
            );

            return RankBySimilarity(
                source.Where(r => !idSet.Contains(r.ReviewId)).ToList(),
                baseVector,
                limit
            );
        }

        // ---- private helpers -----------------------------------------------

        private static float[]? TryParseEmbedding(string? json) {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonSerializer.Deserialize<float[]>(json); } catch { return null; }
        }

        private async Task<float[]?> FetchEmbeddingVector(int reviewId) {
            const string sql = "SELECT EMBEDDING FROM BOOKREVIEW WHERE REVIEW_ID = :reviewId";

            if (_conn.State != System.Data.ConnectionState.Open)
                await _conn.OpenAsync();

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":reviewId", OracleDbType.Int32).Value = reviewId;

            var result = await cmd.ExecuteScalarAsync();
            var json = result == DBNull.Value ? null : result?.ToString();

            if (string.IsNullOrEmpty(json))
                return null;

            try {
                return JsonSerializer.Deserialize<float[]>(json);
            } catch {
                return null;
            }
        }

        private async Task<List<BookReview>> FetchCandidates(IEnumerable<int> excludeIds) {
            // 除外IDをIN句で使うためバインド変数を動的生成
            var idList = excludeIds.ToList();
            var inParams = string.Join(",", idList.Select((_, i) => $":exId{i}"));

            string sql = $@"
                {CommonSelectSql}
                WHERE R.POSTINGTIME IS NOT NULL
                  AND R.EMBEDDING IS NOT NULL
                  AND R.REVIEW_ID NOT IN ({inParams})
                  AND {BlockFilterSql}";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            for (int i = 0; i < idList.Count; i++)
                cmd.Parameters.Add($":exId{i}", OracleDbType.Int32).Value = idList[i];
            AddLoginUserIdParam(cmd);

            return await GetListFromSql(cmd);
        }

        /// <summary>
        /// 指数重みつき加重平均ベクトルを計算します。
        /// indexが大きいほど重みが大きくなります（最新優先）。
        /// weight = decayBase ^ index
        /// </summary>
        private static float[] ComputeWeightedAverageVector(
            IReadOnlyList<(float[] vec, int index)> items,
            double decayBase) {

            int dim = items[0].vec.Length;
            var result = new double[dim];
            double totalWeight = 0;

            foreach (var (vec, index) in items) {
                double weight = Math.Pow(decayBase, index);
                totalWeight += weight;
                for (int d = 0; d < dim; d++)
                    result[d] += vec[d] * weight;
            }

            return result.Select(v => (float)(v / totalWeight)).ToArray();
        }

        private static List<BookReview> RankBySimilarity(
            List<BookReview> candidates,
            float[] baseVector,
            int limit) {

            return candidates
                .Select(r => {
                    float[]? vec = null;
                    try {
                        vec = r.Embedding is not null
                            ? JsonSerializer.Deserialize<float[]>(r.Embedding)
                            : null;
                    } catch { /* 不正なEmbeddingはスキップ */ }
                    return (review: r, vector: vec);
                })
                .Where(x => x.vector is not null)
                .OrderByDescending(x => CosineSimilarity(baseVector, x.vector!))
                .Take(limit)
                .Select(x => x.review)
                .ToList();
        }

        // ---- static utility ------------------------------------------------

        public static double CosineSimilarity(float[] v1, float[] v2) {
            if (v1.Length != v2.Length)
                throw new ArgumentException("Vector size mismatch");

            double dot = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < v1.Length; i++) {
                dot += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }
            return dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }
    }
}