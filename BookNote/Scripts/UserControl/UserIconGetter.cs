using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BookNote.Scripts.BooksAPI.BookImage.Fetcher;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace BookNote.Scripts.UserControl {
    public class UserIconGetter {
        private string _bucketName = "";             // S3のバケット名
        private string _distributionId = "";
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
            _distributionId = s3c["CloudFrontDistributionId"] ?? "";
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
                var url = $"{Keywords.GetCloudFrontBaceUrl()}/icons/{id}/{IconSizeToString(size)}.jpg";
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
                await _s3Client.GetObjectMetadataAsync(_bucketName, $"icons/{id}/{IconSizeToString(size)}.jpg");
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

        /// <summary>
        /// アイコン画像をS3にアップロード（複数サイズ）
        /// </summary>
        /// <param name="userId">ユーザーのPublic ID</param>
        /// <param name="icon256">256×256の画像データ</param>
        /// <param name="icon64">64×64の画像データ</param>
        /// <returns>アップロード成功時true</returns>
        public async Task<bool> UploadIconAsync(string userId, byte[] icon256, byte[] icon64) {
            try {
                // 256×256のアイコンをアップロード
                var upload256Success = await UploadIconToS3Async(userId, icon256, IconSize.LARGE);

                // 64×64のアイコンをアップロード
                var upload64Success = await UploadIconToS3Async(userId, icon64, IconSize.SMALL);

                if (upload256Success && upload64Success) {
                    // CloudFrontキャッシュを削除
                    await InvalidateCloudFrontCacheAsync(userId);
                    return true;
                }

                return false;
            } catch (Exception ex) {
                Console.WriteLine($"アイコンアップロードエラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// CloudFrontのキャッシュを削除
        /// </summary>
        /// <param name="userId">ユーザーのPublic ID</param>
        private async Task<bool> InvalidateCloudFrontCacheAsync(string userId) {
            try {
                if (string.IsNullOrEmpty(_distributionId)) {
                    Console.WriteLine("CloudFront Distribution IDが設定されていません");
                    return false;
                }

                // 削除するパスのリスト
                var paths = new List<string> {
                    $"/icons/{userId}/256.jpg",
                    $"/icons/{userId}/64.jpg"
                };

                var invalidationBatch = new InvalidationBatch {
                    Paths = new Paths {
                        Quantity = paths.Count,
                        Items = paths
                    },
                    CallerReference = $"{userId}-{DateTime.UtcNow.Ticks}" // 一意の参照ID
                };

                var request = new CreateInvalidationRequest {
                    DistributionId = _distributionId,
                    InvalidationBatch = invalidationBatch
                };

                var response = await _cloudFrontClient.CreateInvalidationAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.Created) {
                    Console.WriteLine($"CloudFrontキャッシュ削除成功: Invalidation ID = {response.Invalidation.Id}");
                    Console.WriteLine($"削除対象: {string.Join(", ", paths)}");
                    return true;
                } else {
                    Console.WriteLine($"CloudFrontキャッシュ削除失敗: StatusCode = {response.HttpStatusCode}");
                    return false;
                }
            } catch (Exception ex) {
                Console.WriteLine($"CloudFrontキャッシュ削除エラー: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定サイズのアイコンをS3にアップロード（短いキャッシュ時間）
        /// </summary>
        private async Task<bool> UploadIconToS3Async(string userId, byte[] imageData, IconSize size) {
            try {
                var key = $"icons/{userId}/{IconSizeToString(size)}.jpg";
                byte[] jpegData = ConvertToJpeg(imageData);

                using (var stream = new MemoryStream(jpegData)) {
                    var putRequest = new PutObjectRequest {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = "image/jpeg",
                        // 短めのキャッシュ時間を設定（10分）
                        // Invalidationと組み合わせることで即座に反映しつつ、
                        // Invalidation失敗時も最大10分で更新される
                        Headers = {
                            CacheControl = "public, max-age=600, s-maxage=600"
                        }
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
                // 256×256のアイコンをアップロード
                var upload256Success = await UploadIconToS3Async(userId, icon256, IconSize.LARGE);

                // 64×64のアイコンをアップロード
                var upload64Success = await UploadIconToS3Async(userId, icon64, IconSize.SMALL);

                if (upload256Success && upload64Success) {
                    // オプションでキャッシュ削除（デフォルトはtrue）
                    if (invalidateCache) {
                        // バックグラウンドで実行（レスポンスを待たない）
                        _ = Task.Run(() => InvalidateCloudFrontCacheAsync(userId));
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
    }
}
