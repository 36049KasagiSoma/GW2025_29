using BookNote.Scripts.Login;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BookNote.Scripts.ActivityTrace {
    public class ActivityTracer {
        private ActivityTracer() { }

        private static string _connectionString = "";

        public static void Initialize(string connectionString) {
            _connectionString = connectionString;
        }

        public static void LogActivity(
             ActivityType activityType,
             string userId,
             string? targetId = null,
             string? value = null,
              IActivityTraceTaskResult? result = null) {
            _ = Task.Run(async () => {
                try {
                    await InsertLog(activityType, userId, targetId, value, result);
                } catch {
                }
            });
        }


        private static async Task InsertLog(ActivityType activityType, string userId, string? targetId, string? value, IActivityTraceTaskResult? result) {
            try {
                await using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                        INSERT INTO UserActivity (User_Id, Activity_Id, Target_Id, Value, Timestamp)
                        VALUES (:User_Id, :Activity_Id, :Target_Id, :Value, SYSTIMESTAMP AT TIME ZONE 'Asia/Tokyo')";

                await using var command = new OracleCommand(query, connection);
                command.Parameters.Add("User_Id", OracleDbType.Varchar2).Value = userId;
                command.Parameters.Add("Activity_Id", OracleDbType.Int32).Value = (int)activityType;
                command.Parameters.Add("Target_Id", OracleDbType.Varchar2).Value = targetId != null ? targetId : DBNull.Value;
                command.Parameters.Add("Value", OracleDbType.Varchar2).Value = value != null ? value : DBNull.Value;

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                result?.OnTaskCompleted(Status.SUCCESS);
            } catch (Exception ex) {
                result?.OnTaskCompleted(Status.FAILURE, ex);
            }
        }

        public enum Status {
            SUCCESS,
            FAILURE,
        }

        public interface IActivityTraceTaskResult {
            Status TaskStatus { get; }
            public void OnTaskCompleted(Status status, Exception? ex = null);
        }
    }
}
