using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BookNote.Scripts.BooksAPI.BookImage.Fetcher;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;
using System.Text;

namespace BookNote.Scripts.UserControl {
    public class UserIconGetter {
        private string _bucketName = "";             // S3のバケット名
        private AmazonS3Client _s3Client;            // S3接続用クライアント
        private HttpClient _httpClient;
        private AmazonCloudFrontClient _cloudFrontClient;

        public UserIconGetter() {
            var config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", false)
           .Build();

            var s3c = config.GetSection("S3Config");

            var s3config = new AmazonS3Config {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1 // バージニア北部
            };

            // ============================================
            //var credentials = new BasicAWSCredentials(
            //    s3c["Ak"],
            //    s3c["Sk"]
            //);
            //_s3Client = new AmazonS3Client(credentials, s3config);
            //_cloudFrontClient = new AmazonCloudFrontClient(credentials, Amazon.RegionEndpoint.USEast1);
            // ============================================
            _s3Client = new AmazonS3Client(s3config);
            _cloudFrontClient = new AmazonCloudFrontClient(Amazon.RegionEndpoint.USEast1);
            //=============================================
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
            string? fileName = await FindIconInS3Async(id, size);
            if (fileName == null) return null;
            try {
                var url = $"{Keywords.GetCloudFrontBaceUrl()}/icons/{id}/{fileName}";
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

        private async Task<string?> FindIconInS3Async(string id, IconSize size) {
            try {
                var prefix = $"icons/{id}/{IconSizeToString(size)}_";
                var request = new ListObjectsV2Request {
                    BucketName = _bucketName,
                    Prefix = prefix,
                    MaxKeys = 1
                };

                var response = await _s3Client.ListObjectsV2Async(request);

                if (response.S3Objects != null && response.S3Objects.Count > 0) {
                    // フルパスからファイル名のみを抽出
                    var fullKey = response.S3Objects[0].Key;
                    return fullKey.Split('/').Last();
                }

                return null;
            } catch (AmazonS3Exception ex) {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// S3ファイルを削除
        /// </summary>
        private async Task<bool> DeleteS3FileAsync(string? userId, string? filename) {
            try {
                if (userId == null || filename == null) return false;
                string path = $"icons/{userId}/{filename}";
                var request = new DeleteObjectRequest {
                    BucketName = _bucketName,
                    Key = path
                };
                await _s3Client.DeleteObjectAsync(request);

                return true;
            } catch (Exception ex) {
                Console.WriteLine($"S3ファイル削除エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定サイズのアイコンをS3にアップロード
        /// </summary>
        private async Task<bool> UploadIconToS3Async(string userId, byte[] imageData, IconSize size) {
            try {
                var key = $"icons/{userId}/{IconSizeToString(size)}_{GenerateId(10)}.jpg";
                byte[] jpegData = ConvertToJpeg(imageData);

                using (var stream = new MemoryStream(jpegData)) {
                    var putRequest = new PutObjectRequest {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = "image/jpeg",
                    };
                    var response = await _s3Client.PutObjectAsync(putRequest);

                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK) {
                        Console.WriteLine($"アイコンアップロード成功: {key}");
                        return true;
                    } else {
                        Console.WriteLine($"アイコンアップロード失敗: {key}, StatusCode: {response.HttpStatusCode}");
                        return false;
                    }
                }
            } catch (AmazonS3Exception ex) {
                Console.WriteLine($"S3エラー: {ex.Message}");
                return false;
            } catch (Exception ex) {
                Console.WriteLine($"アップロードエラー: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// アイコン画像をS3にアップロード
        /// </summary>
        public async Task<bool> UploadIconAsync(string userId, byte[] icon256, byte[] icon64, bool invalidateCache = true) {
            try {
                string? delete64 = await FindIconInS3Async(userId, IconSize.SMALL);
                string? delete256 = await FindIconInS3Async(userId, IconSize.LARGE);

                // 256×256のアイコンをアップロード
                var upload256Success = await UploadIconToS3Async(userId, icon256, IconSize.LARGE);

                // 64×64のアイコンをアップロード
                var upload64Success = await UploadIconToS3Async(userId, icon64, IconSize.SMALL);

                if (upload256Success && upload64Success) {
                    if (invalidateCache) {
                        // バックグラウンドで実行（レスポンスを待たない）
                        _ = Task.Run(() => DeleteS3FileAsync(userId, delete64));
                        _ = Task.Run(() => DeleteS3FileAsync(userId, delete256));
                    }
                    return true;
                }

                return false;
            } catch (Exception ex) {
                Console.WriteLine($"アイコンアップロードエラー: {ex.Message}");
                return false;
            }
        }

        private byte[] ConvertToJpeg(byte[] imageData) {
            using (var outputStream = new MemoryStream()) {
                using (var image = Image.Load(imageData)) {
                    // 透過部分を白背景に変換してからJPEG保存
                    image.Mutate(x => x.BackgroundColor(Color.White));

                    // JPEG形式で保存(品質90)
                    image.SaveAsJpeg(outputStream, new JpegEncoder {
                        Quality = 90
                    });
                }

                return outputStream.ToArray();
            }
        }

        public enum IconSize {
            SMALL, LARGE,
        }

        public string IconSizeToString(IconSize size) {
            switch (size) {
                case IconSize.SMALL:
                    return "64";
                case IconSize.LARGE:
                    return "256";
                default:
                    Console.WriteLine($"IconSizeToString:未対応の種類 {size.ToString()}");
                    return "";
            }
        }


        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// 日本時間ベースのID生成
        /// 形式: yyyyMMddssmmdd + ランダム英数字
        /// </summary>
        /// <param name="randomLength">ランダム部分の桁数（推奨: 8以上）</param>
        private static string GenerateId(int randomLength = 8) {
            // 日本時間取得
            TimeZoneInfo jst = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Tokyo Standard Time" : "Asia/Tokyo"
            );
            DateTime jstNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jst);

            // 日付部分
            string datePart = jstNow.ToString("yyyyMMddssmmdd");

            // ランダム部分
            string randomPart = GenerateRandomString(randomLength);

            return datePart + randomPart;
        }
        private static string GenerateRandomString(int length) {
            var result = new StringBuilder(length);
            byte[] buffer = new byte[length];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(buffer);
            }

            for (int i = 0; i < length; i++) {
                result.Append(Chars[buffer[i] % Chars.Length]);
            }

            return result.ToString();
        }
    }
}
