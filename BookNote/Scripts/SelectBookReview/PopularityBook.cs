using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace BookNote.Scripts.SelectBookReview {
    public class PopularityBook : SelectBookReviewBace {


        public PopularityBook(OracleConnection connection, string? myId) : base(connection, myId) {
        }
        public async Task<List<BookReview>> GetReview(int limit) {
            var list = new List<BookReview>();
            const string sql = @"
                 SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                 FROM BOOKREVIEW R
                 INNER JOIN USERS U ON R.USER_ID = U.USER_ID
                 INNER JOIN BOOKS B ON R.ISBN = B.ISBN
                 LEFT JOIN (
                     SELECT REVIEW_ID, COUNT(*) AS GOOD_COUNT
                     FROM GOODREVIEW
                     GROUP BY REVIEW_ID
                 ) G ON R.REVIEW_ID = G.REVIEW_ID
                  WHERE R.POSTINGTIME IS NOT NULL
                   AND R.POSTINGTIME >= SYSTIMESTAMP - INTERVAL '30' DAY
                   AND (:loginUserId IS NULL OR NOT EXISTS (
                       SELECT 1 
                       FROM USERBLOCK BL 
                       WHERE BL.TO_USER_ID = :loginUserId 
                         AND BL.FOR_USER_ID = R.USER_ID
                   ))
                 ORDER BY 
                     (NVL(G.GOOD_COUNT, 0) * 10) - 
                     (EXTRACT(DAY FROM (CAST(SYSTIMESTAMP AS TIMESTAMP) - CAST(R.POSTINGTIME AS TIMESTAMP))) * 24 + 
                      EXTRACT(HOUR FROM (CAST(SYSTIMESTAMP AS TIMESTAMP) - CAST(R.POSTINGTIME AS TIMESTAMP)))) DESC
                 FETCH FIRST :limit ROWS ONLY";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value =
                string.IsNullOrEmpty(_myId) ? (object)DBNull.Value : _myId;
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;

            return await GetListFromSql(cmd);
        }

        public override async Task<List<BookReview>> GetReview() {
            return await GetReview(10);
        }
    }
}
