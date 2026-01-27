// 画像読み込み関数
async function loadBookImages(containerSelector = '') {
    const selector = containerSelector
        ? `${containerSelector} .review-card-small, ${containerSelector} .review-card-large`
        : '.review-card-small, .review-card-large';

    const reviewCards = document.querySelectorAll(selector);
    const isbnMap = new Map();

    reviewCards.forEach(card => {
        const isbn = card.dataset.isbn;
        const bookCover =
            card.querySelector('.book-cover-small') ||
            card.querySelector('.book-cover-large');
        if (!bookCover || bookCover.querySelector('img')) return;
        if (!isbnMap.has(isbn)) {
            isbnMap.set(isbn, []);
        }
        isbnMap.get(isbn).push(bookCover);
    });

    const fetchPromises = Array.from(isbnMap.entries()).map(async ([isbn, bookCovers]) => {
        try {
            const response = await fetch(`/?handler=Image&isbn=${isbn}`);
            if (!response.ok) return;
            const blob = await response.blob();
            const imageUrl = URL.createObjectURL(blob);
            bookCovers.forEach(bookCover => {
                bookCover.innerHTML = `<img src="${imageUrl}" alt="書影" draggable="false"/>`;
            });
        } catch (error) {
            console.error(`ISBN ${isbn} の画像取得に失敗しました:`, error);
        }
    });

    await Promise.all(fetchPromises);
}

// カードクリックイベントを設定
function setupCardClickEvents(containerSelector = '') {
    const selector = containerSelector
        ? `${containerSelector} .review-card-small, ${containerSelector} .review-card-large`
        : '.review-card-small, .review-card-large';

    const reviewCards = document.querySelectorAll(selector);
    reviewCards.forEach(card => {
        card.addEventListener('click', () => {
            const reviewId = card.dataset.reviewId;
            if (!reviewId) return;
            window.location.href = `/ReviewDetails/${reviewId}`;
        });
    });
}

document.addEventListener('DOMContentLoaded', async () => {
    await loadBookImages();
});

document.addEventListener('DOMContentLoaded', () => {
    setupCardClickEvents();
});