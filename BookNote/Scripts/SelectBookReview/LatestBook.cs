using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    public class LatestBook {
        OracleConnection _connection;

        public LatestBook(OracleConnection connection) {
            _connection = connection;
        }
        public async Task<List<BookReview>> GetReview(int cnt) {
            var list = new List<BookReview>();
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, R.ISBN, B.TITLE, B.AUTHOR,B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R, USERS U, BOOKS B
                WHERE R.USER_ID = U.USER_ID AND R.ISBN = B.ISBN
                ORDER BY POSTINGTIME DESC";
            using var cmd = new OracleCommand(sql, _connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
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
                        UserName = reader.GetString(reader.GetOrdinal("USER_NAME")).Trim(),
                    },
                    Book = new Book() {
                        Title = reader.GetString(reader.GetOrdinal("TITLE")).Trim(),
                        Author = reader.GetString(reader.GetOrdinal("AUTHOR")).Trim(),
                        Publisher = reader.GetString(reader.GetOrdinal("PUBLISHER")).Trim(),
                    },
                });
            }
            return list;
        }

        public async Task<List<BookReview>> GetReview() {
            return await GetReview(20);
        }
    }
}
