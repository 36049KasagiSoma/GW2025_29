using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// キーワード検索でレビューを取得します。
    /// </summary>
    public class SearchBooks : SelectBookReviewBase {

        public SearchBooks(OracleConnection conn, string? myId) : base(conn, myId) { }

        public override async Task<List<BookReview>> GetReview() => await GetReview("", 20);

        public async Task<List<BookReview>> GetReview(string keyword) => await GetReview(keyword, 20);

        public async Task<List<BookReview>> GetReview(string keyword, int limit, string sortOrder = "match") {
            string orderByClause = sortOrder switch {
                "date" => "ORDER BY POSTINGTIME DESC",
                "rating" => "ORDER BY GOOD_COUNT DESC, POSTINGTIME DESC",
                _ => "ORDER BY MATCH_SCORE DESC, POSTINGTIME DESC"
            };

            // OracleはFETCH FIRST をサブクエリ内で使えないため ROWNUM でlimitを適用
            string sql = $@"
                SELECT * FROM (
                    SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID,
                           R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER,
                           R.RATING, R.ISSPOILERS, R.POSTINGTIME,
                           R.TITLE AS REVIEW_TITLE, R.REVIEW, R.EMBEDDING,
                           (CASE WHEN B.TITLE  LIKE :keyword THEN 4 ELSE 0 END +
                            CASE WHEN B.AUTHOR LIKE :keyword THEN 3 ELSE 0 END +
                            CASE WHEN R.TITLE  LIKE :keyword THEN 2 ELSE 0 END +
                            CASE WHEN R.REVIEW LIKE :keyword THEN 1 ELSE 0 END) AS MATCH_SCORE,
                           NVL(G.GOOD_COUNT, 0) AS GOOD_COUNT
                    FROM BOOKREVIEW R
                    INNER JOIN USERS U ON R.USER_ID = U.USER_ID
                    INNER JOIN BOOKS B ON R.ISBN = B.ISBN
                    LEFT JOIN (
                        SELECT REVIEW_ID, COUNT(*) AS GOOD_COUNT
                        FROM GOODREVIEW
                        GROUP BY REVIEW_ID
                    ) G ON R.REVIEW_ID = G.REVIEW_ID
                    WHERE R.POSTINGTIME IS NOT NULL
                      AND (B.TITLE  LIKE :keyword
                        OR B.AUTHOR LIKE :keyword
                        OR R.TITLE  LIKE :keyword
                        OR R.REVIEW LIKE :keyword)
                      AND {BlockFilterSql}
                    {orderByClause}
                )
                WHERE ROWNUM <= :limit";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":keyword", OracleDbType.Varchar2).Value = $"%{keyword}%";
            AddLoginUserIdParam(cmd);
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            return await GetListFromSql(cmd);
        }
    }
}