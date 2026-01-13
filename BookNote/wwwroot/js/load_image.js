// ページ読み込み後に画像を取得
document.addEventListener('DOMContentLoaded', async () => {

    // small / large をまとめて取得
    const reviewCards = document.querySelectorAll(
        '.review-card-small, .review-card-large'
    );

    // ISBN -> bookCover要素配列
    const isbnMap = new Map();

    reviewCards.forEach(card => {
        const isbn = card.dataset.isbn;

        const bookCover =
            card.querySelector('.book-cover-small') ||
            card.querySelector('.book-cover-large');

        // 画像が既にある場合はスキップ
        if (!bookCover || bookCover.querySelector('img')) return;

        if (!isbnMap.has(isbn)) {
            isbnMap.set(isbn, []);
        }
        isbnMap.get(isbn).push(bookCover);
    });

    // ISBNごとに1回だけ画像取得
    const fetchPromises = Array.from(isbnMap.entries()).map(async ([isbn, bookCovers]) => {
        try {
            const response = await fetch(`/?handler=Image&isbn=${isbn}`);
            if (!response.ok) return;
            const blob = await response.blob();
            const imageUrl = URL.createObjectURL(blob);
            // small / large 両方に反映
            bookCovers.forEach(bookCover => {
                bookCover.innerHTML = `<img src="${imageUrl}" alt="書影" draggable="false"/>`;
            });
        } catch (error) {
            console.error(`ISBN ${isbn} の画像取得に失敗しました:`, error);
        }
    });

    await Promise.all(fetchPromises);
});


document.addEventListener('DOMContentLoaded', () => {
    const reviewCards = document.querySelectorAll(
        '.review-card-small, .review-card-large'
    );
    reviewCards.forEach(card => {
        card.addEventListener('click', () => {
            const reviewId = card.dataset.reviewId;
            if (!reviewId) return;

            window.location.href = `/ReviewDetails/${reviewId}`;
        });
    });
});