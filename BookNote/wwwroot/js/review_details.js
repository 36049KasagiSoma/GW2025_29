document.addEventListener('DOMContentLoaded', async () => {
    const card = document.querySelector('.review-card-large');
    if (!card) return;

    const isbn = card.dataset.isbn;
    if (!isbn) return;

    const bookCover = card.querySelector('.book-cover-large');

    // 既に画像がある場合はスキップ
    if (bookCover.querySelector('img')) {
        return;
    }

    try {
        const response = await fetch(`?handler=Image&isbn=${encodeURIComponent(isbn)}`);

        if (!response.ok) return;

        const blob = await response.blob();
        const imageUrl = URL.createObjectURL(blob);

        bookCover.innerHTML = `<img src="${imageUrl}" alt="書影" />`;
    } catch (error) {
        console.error('画像の取得に失敗しました:', error);
    }
});
