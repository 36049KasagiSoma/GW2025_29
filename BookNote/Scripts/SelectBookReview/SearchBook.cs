using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace BookNote.Scripts.SelectBookReview {
    public class SearchBooks : ISelectBookReview {

        OracleConnection _connection;

        public SearchBooks(OracleConnection connection) {
            _connection = connection;
        }
        public async Task<List<BookReview>> GetReview(string keyword, int count, string sortOrder = "match") {
            var list = new List<BookReview>();
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            Console.WriteLine($"GetReview呼び出し: keyword={keyword}, count={count}, sortOrder={sortOrder}"); // デバッグ用

            string orderByClause = sortOrder switch {
                "date" => "ORDER BY POSTINGTIME DESC",
                "rating" => "ORDER BY GOOD_COUNT DESC, POSTINGTIME DESC",
                _ => "ORDER BY MATCH_SCORE DESC, POSTINGTIME DESC"
            };
            string sql = $@"
                SELECT * FROM (
                    SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, U.USER_PUBLICID, R.ISBN, B.TITLE, B.AUTHOR, B.PUBLISHER, 
                           R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW,
                           (CASE WHEN B.TITLE LIKE :keyword THEN 4 ELSE 0 END +
                            CASE WHEN B.AUTHOR LIKE :keyword THEN 3 ELSE 0 END +
                            CASE WHEN R.TITLE LIKE :keyword THEN 2 ELSE 0 END +
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
                        AND (B.TITLE LIKE :keyword OR B.AUTHOR LIKE :keyword OR R.TITLE LIKE :keyword OR R.REVIEW LIKE :keyword)
                    {orderByClause}
                )";

            using var cmd = new OracleCommand(sql, _connection);
            cmd.Parameters.Add(":keyword", OracleDbType.Varchar2).Value = $"%{keyword}%";
            cmd.Parameters.Add(":count", OracleDbType.Int32).Value = count;

            using var reader = await cmd.ExecuteReaderAsync();
            int rowCount = 0;
            while (await reader.ReadAsync()) {
                rowCount++;
                list.Add(new BookReview {
                    ReviewId = reader.GetInt32(reader.GetOrdinal("REVIEW_ID")),
                    UserId = reader.GetString(reader.GetOrdinal("USER_ID")).Trim(),
                    Isbn = reader.GetString(reader.GetOrdinal("ISBN")).Trim(),
                    Rating = reader.IsDBNull(reader.GetOrdinal("RATING")) ? null : reader.GetInt32(reader.GetOrdinal("RATING")),
                    IsSpoilers = reader.IsDBNull(reader.GetOrdinal("ISSPOILERS")) ? null : reader.GetInt32(reader.GetOrdinal("ISSPOILERS")),
                    PostingTime = reader.GetDateTime(reader.GetOrdinal("POSTINGTIME")),
                    Title = reader.IsDBNull(reader.GetOrdinal("REVIEW_TITLE")) ? null : reader.GetString(reader.GetOrdinal("REVIEW_TITLE")),
                    Review = reader.IsDBNull(reader.GetOrdinal("REVIEW")) ? null : reader.GetString(reader.GetOrdinal("REVIEW")),
                    User = new User() {
                        UserId = reader.GetString(reader.GetOrdinal("USER_ID")).Trim(),
                        UserPublicId = reader.GetString(reader.GetOrdinal("USER_PUBLICID")).Trim(),
                        UserName = reader.GetString(reader.GetOrdinal("USER_NAME")).Trim(),
                    },
                    Book = new Book() {
                        Title = reader.GetString(reader.GetOrdinal("TITLE")).Trim(),
                        Author = reader.GetString(reader.GetOrdinal("AUTHOR")).Trim(),
                        Publisher = reader.GetString(reader.GetOrdinal("PUBLISHER")).Trim(),
                    },
                });

                if (rowCount >= count) break;
            }

            return list;
        }

        public async Task<List<BookReview>> GetReview(string keyword) {
            return await GetReview(keyword,20);
        }

        public async Task<List<BookReview>> GetReview() {
            return await GetReview("");
        }
    }
}
