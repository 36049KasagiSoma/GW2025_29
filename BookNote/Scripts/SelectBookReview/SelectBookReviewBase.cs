using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    /// <summary>
    /// レビュー取得クラスの基底クラス。
    /// DB接続管理・マッピング処理を共通化し、重複コードを排除します。
    /// </summary>
    public abstract class SelectBookReviewBase : ISelectBookReview {
        protected readonly OracleConnection _conn;
        protected readonly string? _myId;

        protected SelectBookReviewBase(OracleConnection conn, string? myId) {
            _conn = conn;
            _myId = myId;
        }

        public abstract Task<List<BookReview>> GetReview();

        /// <summary>
        /// コマンドを実行し、BookReviewのリストを返します。
        /// SELECT句には必ず EMBEDDING カラムを含めてください。
        /// </summary>
        protected async Task<List<BookReview>> GetListFromSql(OracleCommand cmd) {
            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

            var list = new List<BookReview>();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync()) {
                list.Add(MapBookReview(reader));
            }

            return list;
        }

        /// <summary>
        /// DataReaderの1行をBookReviewモデルにマッピングします。
        /// </summary>
        private static BookReview MapBookReview(System.Data.Common.DbDataReader reader) {
            return new BookReview {
                // Convert.ToInt32 を使うことで、OracleのNUMBER型を安全にintへ変換
                ReviewId = Convert.ToInt32(reader["REVIEW_ID"]),
                UserId = reader["USER_ID"]?.ToString()?.Trim() ?? "",
                Isbn = reader["ISBN"]?.ToString()?.Trim() ?? "",

                // Null許容型の安全な変換
                Rating = reader.IsDBNull(reader.GetOrdinal("RATING")) ? null : Convert.ToInt32(reader["RATING"]),
                IsSpoilers = reader.IsDBNull(reader.GetOrdinal("ISSPOILERS")) ? null : Convert.ToInt32(reader["ISSPOILERS"]),
                PostingTime = reader.IsDBNull(reader.GetOrdinal("POSTINGTIME"))
                    ? DateTime.MinValue
                    : ((DateTimeOffset)reader["POSTINGTIME"]).DateTime,
                Title = reader.IsDBNull(reader.GetOrdinal("REVIEW_TITLE")) ? null : reader["REVIEW_TITLE"].ToString(),
                Review = reader.IsDBNull(reader.GetOrdinal("REVIEW")) ? null : reader["REVIEW"].ToString(),
                Embedding = reader.IsDBNull(reader.GetOrdinal("EMBEDDING")) ? null : reader["EMBEDDING"].ToString(),

                User = new User {
                    UserId = reader["USER_ID"]?.ToString()?.Trim() ?? "",
                    UserPublicId = reader[reader.GetOrdinal("USER_PUBLICID")]?.ToString()?.Trim() ?? "",
                    UserName = reader["USER_NAME"]?.ToString()?.Trim() ?? "",
                },
                Book = new Book {
                    Title = reader.IsDBNull(reader.GetOrdinal("TITLE")) ? "" : reader["TITLE"].ToString()?.Trim(),
                    Author = reader.IsDBNull(reader.GetOrdinal("AUTHOR")) ? "" : reader["AUTHOR"].ToString()?.Trim(),
                    Publisher = reader.IsDBNull(reader.GetOrdinal("PUBLISHER")) ? "" : reader["PUBLISHER"].ToString()?.Trim(),
                },
            };
        }

        /// <summary>
        /// ブロックユーザー・停止ユーザー・非公開レビューを除外するSQLフラグメント。
        /// :loginUserId パラメータと合わせて使用してください。
        /// </summary>
        protected const string BlockFilterSql = @"
            U.USER_STATUSID = 1
            AND R.STATUS_ID = 2
            AND (:loginUserId IS NULL OR NOT EXISTS (
                SELECT 1
                FROM USERBLOCK BL
                WHERE BL.TO_USER_ID = :loginUserId
                  AND BL.FOR_USER_ID = R.USER_ID
            ))AND (:loginUserId IS NULL OR NOT EXISTS (
                SELECT 1
                FROM BookMute BM
                WHERE BM.User_Id = :loginUserId
                  AND BM.ISBN = R.ISBN
            ))";

        /// <summary>
        /// :loginUserId パラメータを追加します。
        /// </summary>
        protected void AddLoginUserIdParam(OracleCommand cmd) {
            cmd.Parameters.Add(":loginUserId", OracleDbType.Char).Value =
                string.IsNullOrEmpty(_myId) ? (object)DBNull.Value : _myId;
        }

        public async Task<List<BookReview>> GetAllReviews(int? days = null) {
            string sql = $@"
                {CommonSelectSql}
                WHERE R.POSTINGTIME IS NOT NULL
                  AND {BlockFilterSql}
                  {(days.HasValue ? $"AND R.POSTINGTIME >= (SYSTIMESTAMP AT TIME ZONE 'Asia/Tokyo') - NUMTODSINTERVAL({days.Value}, 'DAY')" : "")}
                ";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            AddLoginUserIdParam(cmd);
            return await GetListFromSql(cmd);
        }

        public async Task<List<BookReview>> GetRecentShuffledReviews(int limit) {
            // DBMS_RANDOM.VALUE を使うことで、Oracle 側で高速にシャッフルできます
            string sql = $@"
                {CommonSelectSql}
                WHERE R.POSTINGTIME >= SYSTIMESTAMP - INTERVAL '30' DAY
                  AND {BlockFilterSql}
                ORDER BY DBMS_RANDOM.VALUE
                FETCH FIRST :limit ROWS ONLY";

            using var cmd = new OracleCommand(sql, _conn);
            cmd.BindByName = true;
            cmd.Parameters.Add(":limit", OracleDbType.Int32).Value = limit;
            AddLoginUserIdParam(cmd); // :loginUserId に null(DBNull) を渡す

            return await GetListFromSql(cmd);
        }

        /// <summary>
        /// 全クラスで共通のSELECT句。
        /// </summary>
        protected const string CommonSelectSql = @"
            SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID,
                   R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER,
                   R.RATING, R.ISSPOILERS, R.POSTINGTIME,
                   R.TITLE AS REVIEW_TITLE, R.REVIEW, R.EMBEDDING
            FROM BOOKREVIEW R
            INNER JOIN USERS U ON R.USER_ID = U.USER_ID
            INNER JOIN BOOKS B ON R.ISBN = B.ISBN";
    }
}