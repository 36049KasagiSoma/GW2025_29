using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    public class RecommendedBook : SelectBookReviewBase {
        public RecommendedBook(OracleConnection conn, string? myId) : base(conn, myId) {
        }
        public override async Task<List<BookReview>> GetReview() {
            return await GetReview(10);
        }
        public async Task<List<BookReview>> GetReview(int limit) {
            if (string.IsNullOrEmpty(_myId)) {
                return await GetRecentShuffledReviews(limit);
            }
            var ssr = new SelectSimilarReview(_conn, _myId);

            // 1. データの並列・一括取得
            var reviews = await GetAllReviews(30 * 5); //過去5ヶ月分のレビューを取得
            var viewedIdsList = await GetViewedReviewIds();
            var goodedIdsList = await GetGoodedReviewIds();
            var followedIdsList = await GetFollowedUsersReviewIds();


            // 2. 検索効率のためにHashSet化
            var viewedIds = viewedIdsList.ToHashSet();
            var goodedIds = goodedIdsList.ToHashSet();
            var followedIds = followedIdsList.ToHashSet();

            // 3. カテゴリごとに抽出 (元のロジックを維持：抽出後に元のリストから除外)
            var viewSsr = ssr.SortBySimilarity(reviews, viewedIds.ToList(), limit);
            reviews = reviews.Where(r => !viewedIds.Contains(r.ReviewId)).ToList();

            var goodSsr = ssr.SortBySimilarity(reviews, goodedIds.ToList(), limit);
            reviews = reviews.Where(r => !goodedIds.Contains(r.ReviewId)).ToList();

            var followedSsr = ssr.SortBySimilarity(reviews, followedIds.ToList(), limit);

            var rtn = new List<BookReview>();

            rtn.AddRange(InterleaveReviews(limit, viewSsr, goodSsr, followedSsr));
            if(rtn.Count < limit) {
                rtn.AddRange((await GetRecentShuffledReviews(limit)).ToList());
            }
            // 4. ラウンドロビン方式で結合
            return rtn;
        }

        ///　<summary> 複数のレビューリストをラウンドロビン方式で結合します。</summary>
        private List<BookReview> InterleaveReviews(int limit, params List<BookReview>[] sources) {
            var result = new List<BookReview>();
            int maxCount = sources.Max(s => s.Count);

            for (int i = 0; i < maxCount; i++) {
                foreach (var source in sources) {
                    if (i < source.Count) {
                        result.Add(source[i]);
                        if (result.Count >= limit) return result;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// ユーザーが閲覧（View）したレビューのIdリストを返します。
        /// </summary>
        public async Task<List<int>> GetViewedReviewIds() {
            const string sql = @"
                SELECT TO_NUMBER(A.TARGET_ID) AS REVIEW_ID
                FROM USERACTIVITY A
                WHERE A.USER_ID = :userId
                  AND A.ACTIVITY_ID = 3
                  AND A.TARGET_ID IS NOT NULL
                ORDER BY A.TIMESTAMP ASC";

            return await FetchReviewIds(sql);
        }

        /// <summary>
        /// ユーザーがいいね（GoodTheReview）したレビューのIdリストを返します。
        /// </summary>
        public async Task<List<int>> GetGoodedReviewIds() {
            const string sql = @"
                SELECT TO_NUMBER(A.TARGET_ID) AS REVIEW_ID
                FROM USERACTIVITY A
                WHERE A.USER_ID = :userId
                  AND A.TARGET_ID IS NOT NULL
                  AND A.ACTIVITY_ID IN (4, 11)
                  AND A.TIMESTAMP = (
                      SELECT MAX(A2.TIMESTAMP)
                      FROM USERACTIVITY A2
                      WHERE A2.USER_ID = :userId
                        AND A2.TARGET_ID = A.TARGET_ID
                        AND A2.ACTIVITY_ID IN (4, 11)
                  )
                  AND A.ACTIVITY_ID = 4
                ORDER BY A.TIMESTAMP ASC";
            return await FetchReviewIds(sql);
        }

        /// <summary>
        /// ユーザーがフォロー（FollowUser）したユーザーのレビューIdリストを返します。
        /// フォロー操作の時系列順（古い順）で、各フォローユーザーの公開レビューIdを返します。
        /// </summary>
        public async Task<List<int>> GetFollowedUsersReviewIds() {
            const string sql = @"
                SELECT R.REVIEW_ID
                FROM BOOKREVIEW R
                INNER JOIN USERS U ON U.USER_ID = R.USER_ID
                WHERE R.POSTINGTIME IS NOT NULL
                  AND R.STATUS_ID = 2
                  AND U.USER_STATUSID = 1
                  AND R.USER_ID IN (
                      SELECT A.TARGET_ID
                      FROM USERACTIVITY A
                      WHERE A.USER_ID = :userId
                        AND A.TARGET_ID IS NOT NULL
                        AND A.ACTIVITY_ID IN (5, 12)
                        AND A.TIMESTAMP = (
                            SELECT MAX(A2.TIMESTAMP)
                            FROM USERACTIVITY A2
                            WHERE A2.USER_ID = :userId
                              AND A2.TARGET_ID = A.TARGET_ID
                              AND A2.ACTIVITY_ID IN (5, 12)
                        )
                        AND A.ACTIVITY_ID = 5
                  )
                ORDER BY R.POSTINGTIME ASC";

            return await FetchReviewIds(sql);
        }

        private async Task<List<int>> FetchReviewIds(string sql) {
            var list = new List<int>();

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":userId", OracleDbType.Char).Value = _myId;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                if (!reader.IsDBNull(0))
                    list.Add(reader.GetInt32(0));
            }

            return list;
        }
    }
}
