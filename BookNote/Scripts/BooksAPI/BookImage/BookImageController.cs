using System;

namespace BookNote.Scripts.BooksAPI.BookImage {
    public class BookImageController {
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

        public async Task<byte[]?> GetBookImageData(string isbn) {
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
    }
}
