using BookNote.Scripts.Models;

namespace BookNote.Scripts.UserControl {
    public class UserActivityTracer {
        public UserActivityTracer() {
            var config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", false)
           .Build();
        }

        public void PutActivity(string userId,ActivityType activity) {

        }
    }
}
