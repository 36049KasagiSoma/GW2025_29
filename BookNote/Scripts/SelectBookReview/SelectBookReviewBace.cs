using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace BookNote.Scripts.SelectBookReview {
    public abstract class SelectBookReviewBace : ISelectBookReview {
        protected readonly OracleConnection _conn;
        protected readonly string? _myId;
        public SelectBookReviewBace(OracleConnection conn, string? myId) {
            _conn = conn;
            _myId = myId;
        }

        public abstract Task<List<BookReview>> GetReview();

        protected async Task<List<BookReview>> GetListFromSql(OracleCommand cmd) {
            var list = new List<BookReview>();

            if (_conn.State != ConnectionState.Open)
                await _conn.OpenAsync();

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
                    Embedding = reader.IsDBNull("EMBEDDING") ? null : reader.GetString("EMBEDDING"),
                    User = new User {
                        UserId = reader.GetString("USER_ID").Trim(),
                        UserPublicId = reader.GetString(reader.GetOrdinal("USER_PUBLICID")).Trim(),
                        UserName = reader.GetString("USER_NAME").Trim(),
                    },
                    Book = new Book() {
                        Title = reader.IsDBNull(reader.GetOrdinal("TITLE")) ? "" : reader.GetString(reader.GetOrdinal("TITLE")).Trim(),
                        Author = reader.IsDBNull(reader.GetOrdinal("AUTHOR")) ? "" : reader.GetString(reader.GetOrdinal("AUTHOR")).Trim(),
                        Publisher = reader.IsDBNull(reader.GetOrdinal("PUBLISHER")) ? "" : reader.GetString(reader.GetOrdinal("PUBLISHER")).Trim(),
                    },
                });
            }

            return list;
        }
    }
}
