using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// 人気レビューをスコア順に取得します（直近30日以内）。
    /// スコア = いいね数×10 - 経過時間（時間単位）
    /// </summary>
    public class PopularityBook : SelectBookReviewBase {

        public PopularityBook(OracleConnection conn, string? myId) : base(conn, myId) { }

        public override async Task<List<BookReview>> GetReview() => await GetReview(10);

        public async Task<List<BookReview>> GetReview(int limit) {
            string sql = $@"
                {CommonSelectSql}
                LEFT JOIN (
                    SELECT REVIEW_ID, COUNT(*) AS GOOD_COUNT
                    FROM GOODREVIEW
                    GROUP BY REVIEW_ID
                ) G ON R.REVIEW_ID = G.REVIEW_ID
                WHERE R.POSTINGTIME IS NOT NULL
                  AND R.POSTINGTIME >= SYSTIMESTAMP - INTERVAL '30' DAY
                  AND {BlockFilterSql}
                ORDER BY
                    (NVL(G.GOOD_COUNT, 0) * 10) -
                    (EXTRACT(DAY  FROM (CAST(SYSTIMESTAMP AS TIMESTAMP) - CAST(R.POSTINGTIME AS TIMESTAMP))) * 24 +
                     EXTRACT(HOUR FROM (CAST(SYSTIMESTAMP AS TIMESTAMP) - CAST(R.POSTINGTIME AS TIMESTAMP)))) DESC
                FETCH FIRST :limit ROWS ONLY";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            AddLoginUserIdParam(cmd);
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            return await GetListFromSql(cmd);
        }
    }
}