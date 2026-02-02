using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BookNote.Scripts.BooksAPI.BookImage.Fetcher;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System;
using System.Configuration;
using System.Net.Http;

namespace BookNote.Scripts.BooksAPI.BookImage {
    /// <summary>
    /// 書影取得をサポートするクラスです。
    /// </summary>
    public class BookImageController {
        private string _bucketName = "";             // S3のバケット名
        private AmazonS3Client _s3Client;            // S3接続用クライアント
        private CloudFrontFetcher _clientFetcher;    // CloudFront書影取得サポートクラス

        public BookImageController() {
            var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();

            _clientFetcher = new CloudFrontFetcher();

            var s3c = config.GetSection("S3Config");

            var s3config = new AmazonS3Config {
                RegionEndpoint = Amazon.RegionEndpoint.USEast1 // バージニア北部
            };

            var credentials = new BasicAWSCredentials(
                s3c["Ak"],
                s3c["Sk"]
            );

            _s3Client = new AmazonS3Client(credentials, s3config);
            _bucketName = s3c["BucketName"] ?? "";
        }

        /// <summary>
        /// ISBNから、画像取得用のURLを返します。
        /// ただし、キャッシュURLは返されません。
        /// </summary>
        /// <param name="isbn">対象のISBN</param>
        /// <returns>画像URL 存在しない場合は NULL</returns>
        public async Task<string?> GetBookImageUrl(string isbn) {
            var fecList = BookCoverFetcherFactory.CreateList();
            foreach (var fec in fecList) {
                try {
                    var imageUrl = await fec.GetCoverUrlAsync(isbn);
                    if (imageUrl != null && imageUrl.Trim().Length > 0) {
                        return imageUrl;
                    }
                } catch { }
            }
            return null;
        }

        /// <summary>
        /// 画像を取得します。
        /// キャッシュからの取得を優先します。
        /// </summary>
        /// <param name="isbn">対象のISBN</param>
        /// <returns>画像Byte配列 存在しない場合は NULL</returns>
        public async Task<byte[]?> GetBookImageData(string isbn) {
            bool isExist = await ExistsInS3Async(isbn);

            if (!isExist) {
                var image = await GetBookImage(isbn);
                if (image != null)
                    await PutObjectAsync(isbn, image);
                return image;
            }

            var getImage = await _clientFetcher.GetCoverImageAsync(isbn);
            if (getImage != null) {
                return getImage;
            }

            return null;
        }

        private async Task<byte[]?> GetBookImage(string isbn) {
            var fecList = BookCoverFetcherFactory.CreateList();
            foreach (var fec in fecList) {
                try {
                    var image = await fec.GetCoverImageAsync(isbn);
                    if (image != null && image.Length > 0) {
                        return image;
                    }
                } catch { }
            }
            return null;
        }

        private async Task<bool> ExistsInS3Async(string isbn) {
            try {
                await _s3Client.GetObjectMetadataAsync(_bucketName, $"covers/{isbn}.jpg");
                return true;
            } catch (AmazonS3Exception ex)
                  when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) {
                return false;
            } catch (AmazonS3Exception ex)
                  when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                return false;
            }
        }

        private async Task PutObjectAsync(string isbn, byte[] data) {
            using var stream = new MemoryStream(data);

            var request = new PutObjectRequest {
                BucketName = _bucketName,
                Key = $"covers/{isbn}.jpg",
                InputStream = stream,
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.Private
            };

            await _s3Client.PutObjectAsync(request);
        }
    }
}
