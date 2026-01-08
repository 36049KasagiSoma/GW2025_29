using BookNote.Scripts.Models;
using BookNote.Scripts.SelectBook;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace BookNote.Scripts.SelectBookReview {
    public class RecommendBook : ISelectBookReview {

        OracleConnection _connection;

        public RecommendBook( OracleConnection connection) {
            _connection = connection;
        }
        public List<BookReview> GetReview() {
            var list = new List<BookReview>();

            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            const string sql = @"
            SELECT REVIEW_ID, USER_ID, ISBN, RATING, ISSPOILERS, POSTINGTIME, TITLE, REVIEW
            FROM BOOKREVIEW
            ORDER BY POSTINGTIME DESC";

            using var cmd = new OracleCommand(sql, _connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                list.Add(new BookReview {
                    ReviewId = reader.GetInt32(reader.GetOrdinal("REVIEW_ID")),
                    UserId = reader.GetString(reader.GetOrdinal("USER_ID")).Trim(),
                    Isbn = reader.GetString(reader.GetOrdinal("ISBN")).Trim(),
                    Rating = reader.IsDBNull(reader.GetOrdinal("RATING")) ? null : reader.GetInt32(reader.GetOrdinal("RATING")),
                    IsSpoilers = reader.IsDBNull(reader.GetOrdinal("ISSPOILERS")) ? null : reader.GetInt32(reader.GetOrdinal("ISSPOILERS")),
                    PostingTime = reader.GetDateTime(reader.GetOrdinal("POSTINGTIME")),
                    Title = reader.IsDBNull(reader.GetOrdinal("TITLE")) ? null : reader.GetString(reader.GetOrdinal("TITLE")),
                    Review = reader.IsDBNull(reader.GetOrdinal("REVIEW")) ? null : reader.GetString(reader.GetOrdinal("REVIEW"))
                });

            }

            return list;
        }
    }
}
