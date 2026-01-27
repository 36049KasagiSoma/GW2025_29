using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.SelectBookReview {
    public class FollowingUserBook {
        OracleConnection _connection;

        public FollowingUserBook(OracleConnection connection) {
            _connection = connection;
        }
        public async Task<List<BookReview>> GetReview(string userId) {
            var list = new List<BookReview>();

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, R.ISBN,
                       B.TITLE, B.AUTHOR, B.PUBLISHER,
                       R.RATING, R.ISSPOILERS, R.POSTINGTIME,
                       R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R
                JOIN USERS U ON R.USER_ID = U.USER_ID
                JOIN BOOKS B ON R.ISBN = B.ISBN
                WHERE R.USER_ID IN (
                    SELECT F.For_User_Id
                    FROM UserFollow F
                    WHERE F.To_User_Id = :userId
                )
                AND R.POSTINGTIME IS NOT NULL
                ORDER BY R.POSTINGTIME DESC";

            using var cmd = new OracleCommand(sql, _connection);
            cmd.Parameters.Add(":userId", OracleDbType.Varchar2).Value = userId;

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync()) {
                list.Add(new BookReview {
                    ReviewId = reader.GetInt32("REVIEW_ID"),
                    UserId = reader.GetString("USER_ID").Trim(),
                    Isbn = reader.GetString("ISBN").Trim(),
                    Rating = reader.IsDBNull("RATING") ? null : reader.GetInt32("RATING"),
                    IsSpoilers = reader.IsDBNull("ISSPOILERS") ? null : reader.GetInt32("ISSPOILERS"),
                    PostingTime = reader.GetDateTime("POSTINGTIME"),
                    Title = reader.IsDBNull("REVIEW_TITLE") ? null : reader.GetString("REVIEW_TITLE"),
                    Review = reader.IsDBNull("REVIEW") ? null : reader.GetString("REVIEW"),
                    User = new User {
                        UserId = reader.GetString("USER_ID").Trim(),
                        UserName = reader.GetString("USER_NAME").Trim(),
                    },
                    Book = new Book {
                        Title = reader.GetString("TITLE").Trim(),
                        Author = reader.GetString("AUTHOR").Trim(),
                        Publisher = reader.GetString("PUBLISHER").Trim(),
                    },
                });
            }

            return list;
        }

    }
}
