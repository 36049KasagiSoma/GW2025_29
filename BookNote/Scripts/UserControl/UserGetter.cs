using BookNote.Scripts.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.UserControl {
    public class UserGetter {
        OracleConnection _connection;

        public UserGetter(OracleConnection connection) {
            _connection = connection;
        }
        public async Task<User?> GetUser(string UserPublicId) {

            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
            const string sql = @"
                SELECT *
                FROM USERS
                WHERE USER_PublicId = :UserPublicId";
            using var cmd = new OracleCommand(sql, _connection);
            cmd.BindByName = true;
            cmd.Parameters.Add(":UserPublicId", OracleDbType.Char).Value = UserPublicId;
            using var reader = await cmd.ExecuteReaderAsync();
            User? user = null;
            while (await reader.ReadAsync()) {
                try {
                    string R_Id = reader.GetString(reader.GetOrdinal("User_Id"));
                    string R_PId = reader.GetString(reader.GetOrdinal("User_PublicId"));
                    string R_Name = reader.GetString(reader.GetOrdinal("User_Name"));
                    string R_Profile = reader.GetString(reader.GetOrdinal("User_Profile"));
                    user = new User {
                        UserId = R_Id,
                        UserPublicId = R_PId,
                        UserName = R_Name,
                        UserProfile = R_Profile,
                    };
                    user.BookReviews = await GetUserReviews(UserPublicId);  // ← ここを削除
                } catch (Exception ex) {
                    throw;
                }

                if (user != null) {
                    break;
                }
            }

            if (user != null) {
                user.BookReviews = await GetUserReviews(UserPublicId);
            }
            return user;
        }


        public async Task<List<BookReview>> GetUserReviews(string UserPublicId) {
            var list = new List<BookReview>();
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, R.ISBN, B.TITLE, B.AUTHOR,B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R, USERS U, BOOKS B
                WHERE R.USER_ID = U.USER_ID AND R.ISBN = B.ISBN AND R.POSTINGTIME IS NOT NULL AND U.USER_PUBLICID = :UserPublicId
                ORDER BY POSTINGTIME DESC";
            try {
                using var cmd = new OracleCommand(sql, _connection);
                cmd.Parameters.Add(":UserPublicId", OracleDbType.Char).Value = UserPublicId;
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
            }catch(Exception ex) {
                throw;            }
            return list;
        }
        public async Task<List<BookReview>> GetUserGoodReviews(string UserPublicId) {
            var list = new List<BookReview>();
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
            const string sql = @"
                SELECT R.REVIEW_ID, R.USER_ID, U.USER_NAME, R.ISBN, B.TITLE, B.AUTHOR,B.PUBLISHER, R.RATING, R.ISSPOILERS, R.POSTINGTIME, R.TITLE AS REVIEW_TITLE, R.REVIEW
                FROM BOOKREVIEW R, USERS U, BOOKS B, GoodReview G
                WHERE R.USER_ID = U.USER_ID AND R.ISBN = B.ISBN AND G.USER_ID = R.USER_ID AND R.REVIEW_ID = G.REVIEW_ID AND R.POSTINGTIME IS NOT NULL AND U.USER_PUBLICID = :UserPublicId
                ORDER BY POSTINGTIME DESC";
            try {
                using var cmd = new OracleCommand(sql, _connection);
                cmd.Parameters.Add(":UserPublicId", OracleDbType.Char).Value = UserPublicId;
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
            } catch (Exception ex) {
                throw;
            }
            return list;
        }
    }
}
