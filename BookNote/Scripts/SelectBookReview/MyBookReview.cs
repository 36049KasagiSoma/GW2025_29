using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// 指定ユーザー自身のレビューを取得します。
    /// </summary>
    public class MyBookReview : SelectBookReviewBase {

        public MyBookReview(OracleConnection conn, string? myId) : base(conn, myId) { }

        public override async Task<List<BookReview>> GetReview() => await GetReview(10);


        public async Task<List<BookReview>> GetReview(int limit) {
            // 自分自身のレビューのためブロックフィルタは不要
            // ただし下書き（Status_Id != 1）および停止ユーザーは除外
            string sql = $@"
                {CommonSelectSql}
                WHERE R.USER_ID = :userId
                  AND R.POSTINGTIME IS NOT NULL
                  AND U.USER_STATUSID = 1
                  AND R.STATUS_ID = 2
                ORDER BY R.POSTINGTIME DESC
                FETCH FIRST :limit ROWS ONLY";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":userId", OracleDbType.Varchar2).Value = _myId;
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            return await GetListFromSql(cmd);
        }


    }
}