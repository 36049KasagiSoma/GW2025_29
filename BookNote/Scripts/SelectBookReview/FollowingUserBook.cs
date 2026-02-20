using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// フォロー中ユーザーのレビューを取得します。
    /// </summary>
    public class FollowingUserBook : SelectBookReviewBase {

        public FollowingUserBook(OracleConnection conn, string? myId) : base(conn, myId) { }

        public override async Task<List<BookReview>> GetReview() => await GetReview(_myId ?? "", 10);

        public async Task<List<BookReview>> GetReview(string userId) => await GetReview(userId, 10);

        public async Task<List<BookReview>> GetReview(string userId, int limit) {
            string sql = $@"
                {CommonSelectSql}
                WHERE R.USER_ID IN (
                    SELECT F.For_User_Id
                    FROM UserFollow F
                    WHERE F.To_User_Id = :userId
                )
                AND R.POSTINGTIME IS NOT NULL
                AND {BlockFilterSql}
                ORDER BY R.POSTINGTIME DESC
                FETCH FIRST :limit ROWS ONLY";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":userId", OracleDbType.Varchar2).Value = userId;
            AddLoginUserIdParam(cmd);
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            return await GetListFromSql(cmd);
        }
    }
}