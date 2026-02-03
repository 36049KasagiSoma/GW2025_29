using Amazon.Runtime;
using Amazon.S3;
using BookNote.Scripts.BooksAPI.BookImage.Fetcher;

namespace BookNote.Scripts.UserControl {
    public class UserIconGetter {
        private string _bucketName = "";             // S3のバケット名
        private AmazonS3Client _s3Client;            // S3接続用クライアント
        private HttpClient _httpClient;

        public UserIconGetter() {
            var config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", false)
           .Build();

            var s3c = config.GetSection("S3Config");

            var s3config = new AmazonS3Config {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1 // バージニア北部
            };

            var credentials = new BasicAWSCredentials(
                s3c["Ak"],
                s3c["Sk"]
            );

            _s3Client = new AmazonS3Client(credentials, s3config);
            _httpClient = new HttpClient();
            _bucketName = s3c["BucketName"] ?? "";
        }

        public async Task<byte[]?> GetIconImageData(string id, IconSize size) {
            var data = await GetIconImageDataTask(id, size);

            if (data == null || data.Length == 0) {
                data = await GetIconImageDataTask("n", size); // デフォルトアイコンの時
            }
            return data;
        }

        private async Task<byte[]?> GetIconImageDataTask(string id, IconSize size) {
            bool isExist = await ExistsInS3Async(id, size);
            if (!isExist) return null;
            try {
                var url = $"{Keywords.GetCloudFrontBaceUrl()}/icons/{id}/{(size == IconSize.SMALL ? "64" : "256")}.jpg";
                // リクエストメッセージを作成してヘッダーを追加
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                request.Headers.Add("Referer", Keywords.GetCloudFrontBaceUrl() + "/"); // または自分のサイトのURL

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode) {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            } catch (HttpRequestException ex) {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                return null;
            } catch (Exception ex) {
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> ExistsInS3Async(string id, IconSize size) {
            try {
                await _s3Client.GetObjectMetadataAsync(_bucketName, $"icons/{id}/{(size == IconSize.SMALL ? "64" : "256")}.jpg");
                return true;
            } catch (AmazonS3Exception ex)
                  when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                Console.WriteLine(ex.Message);
                return false;
            } catch (AmazonS3Exception ex)
                  when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public enum IconSize {
            SMALL, LARGE,
        }
    }
}
