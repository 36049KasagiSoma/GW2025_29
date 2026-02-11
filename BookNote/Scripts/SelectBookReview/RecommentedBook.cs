using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    public class RecommentedBook : SelectBookReviewBace {
        public RecommentedBook(OracleConnection conn,string? myId) : base(conn, myId) {
        }

        public override async Task<List<BookReview>> GetReview() {
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R, USERS U, BOOKS B
                WHERE R.USER_ID = U.USER_ID 
                  AND R.ISBN = B.ISBN 
                  AND R.POSTINGTIME IS NOT NULL
                  AND (:loginUserId IS NULL OR NOT EXISTS (
                      SELECT 1 
                      FROM USERBLOCK BL 
                      WHERE BL.TO_USER_ID = :loginUserId 
                        AND BL.FOR_USER_ID = R.USER_ID
                  ))
                ORDER BY POSTINGTIME DESC";
            using var cmd = new OracleCommand(sql, _conn);
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value =
                string.IsNullOrEmpty(_myId) ? (object)DBNull.Value : _myId;
            return await GetListFromSql(cmd);
        }
    }
}
