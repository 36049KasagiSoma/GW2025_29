using System.Text;

namespace BookNote.Scripts {
    public class Keywords {
        private Keywords() { }
        public static string GetDbConnectionString(IConfiguration configuration) {
            StringBuilder sb = new StringBuilder();

            var rds = configuration.GetSection("RdsConfig");
            sb.Append($"User Id={rds["UserId"]};");
            sb.Append($"Password={rds["Password"]};");
            sb.Append("Data Source=");
            sb.Append("(DESCRIPTION=");
            sb.Append("(ADDRESS=(PROTOCOL=TCP)");
            sb.Append($"(HOST={rds["Host"]})");
            sb.Append($"(PORT={rds["Port"]}))");
            sb.Append($"(CONNECT_DATA=(SERVICE_NAME={rds["Service"]}))");
            sb.Append(")");
            return sb.ToString();
        }

        public static string GetCloudFrontBaceUrl() => "https://d2dayc6ex7a6gk.cloudfront.net";

    }
}
