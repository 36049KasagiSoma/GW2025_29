using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// 最新レビューを取得します。
    /// </summary>
    public class LatestBook : SelectBookReviewBase {

        public LatestBook(OracleConnection conn, string? myId) : base(conn, myId) { }

        public override async Task<List<BookReview>> GetReview() => await GetReview(20);

        public async Task<List<BookReview>> GetReview(int limit) {
            string sql = $@"
                {CommonSelectSql}
                WHERE R.POSTINGTIME IS NOT NULL
                  AND {BlockFilterSql}
                ORDER BY R.POSTINGTIME DESC
                FETCH FIRST :limit ROWS ONLY";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            AddLoginUserIdParam(cmd);
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            return await GetListFromSql(cmd);
        }
    }
}