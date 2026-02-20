using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    public class LatestBook : SelectBookReviewBace {

        public LatestBook(OracleConnection conn, string? myId) : base(conn, myId) {
        }

        public async Task<List<BookReview>> GetReview(int limit) {
            const string sql = @"
                 SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW, R.EMBEDDING
                 FROM BOOKREVIEW R
                 INNER JOIN USERS U ON R.USER_ID = U.USER_ID
                 INNER JOIN BOOKS B ON R.ISBN = B.ISBN
                 WHERE R.POSTINGTIME IS NOT NULL
                   AND (:loginUserId IS NULL OR NOT EXISTS (
                       SELECT 1 
                       FROM USERBLOCK BL 
                       WHERE BL.TO_USER_ID = :loginUserId 
                         AND BL.FOR_USER_ID = R.USER_ID
                   ))
                 ORDER BY R.POSTINGTIME DESC
                 FETCH FIRST :limit ROWS ONLY";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value =
                string.IsNullOrEmpty(_myId) ? (object)DBNull.Value : _myId;
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            return await GetListFromSql(cmd);
        }

        public override async Task<List<BookReview>> GetReview() {
            return await GetReview(20);
        }
    }
}
