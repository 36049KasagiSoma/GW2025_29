using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    public class RecommentedBook : SelectBookReviewBace {
        public RecommentedBook(OracleConnection conn) : base(conn) {
        }

        public override async Task<List<BookReview>> GetReview() {
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR,B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R, USERS U, BOOKS B
                WHERE R.USER_ID = U.USER_ID AND R.ISBN = B.ISBN AND R.POSTINGTIME IS NOT NULL
                ORDER BY POSTINGTIME DESC";
            using var cmd = new OracleCommand(sql, _conn);
           
            return await GetListFromSql(cmd);
        }
    }
}
