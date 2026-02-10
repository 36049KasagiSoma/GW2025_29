using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace BookNote.Scripts.SelectBookReview {
    public class PopularityBook : SelectBookReviewBace {


        public PopularityBook(OracleConnection connection):base(connection) {
        }
        public async Task<List<BookReview>> GetReview(int cnt) {
            var list = new List<BookReview>();
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME,　U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR,B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R, USERS U, BOOKS B
                WHERE R.USER_ID = U.USER_ID AND R.ISBN = B.ISBN AND R.POSTINGTIME IS NOT NULL
                ORDER BY POSTINGTIME DESC";
            using var cmd = new OracleCommand(sql, _conn);
            
            return await GetListFromSql(cmd);
        }

        public override async Task<List<BookReview>> GetReview() {
            return await GetReview(20);
        }
    }
}
