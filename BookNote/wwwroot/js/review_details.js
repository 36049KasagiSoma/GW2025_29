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

// いいね機能の初期化
document.addEventListener('DOMContentLoaded', function () {
    const likeButton = document.querySelector('.like-button');

    if (likeButton) {
        const reviewId = likeButton.getAttribute('data-review-id');
        console.log('Review ID:', reviewId); // デバッグ用

        // 初期状態の読み込み
        loadLikeStatus(reviewId, likeButton);

        // クリックイベント
        likeButton.addEventListener('click', function (e) {
            e.preventDefault();
            console.log('Like button clicked'); // デバッグ用
            toggleLike(reviewId, likeButton);
        });
    } else {
        console.log('Like button not found'); // デバッグ用
    }
});

// いいね状態の読み込み
async function loadLikeStatus(reviewId, button) {
    try {
        console.log('Loading like status for review:', reviewId);
        const response = await fetch(`/api/reviews/${reviewId}/like`);
        console.log('Response status:', response.status);

        if (response.ok) {
            const data = await response.json();
            console.log('Like data:', data);
            updateLikeButton(button, data.isLiked, data.likeCount, false);  // animate = false
        }
    } catch (error) {
        console.error('いいね状態の読み込みエラー:', error);
    }
}

// いいねの切り替え
async function toggleLike(reviewId, button) {
    try {
        console.log('Toggling like for review:', reviewId);
        const response = await fetch(`/api/reviews/${reviewId}/like`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        console.log('Toggle response status:', response.status);

        if (response.ok) {
            const data = await response.json();
            console.log('Toggle result:', data);
            updateLikeButton(button, data.isLiked, data.likeCount, true);  // animate = true
        } else {
            const errorText = await response.text();
            console.error('Toggle error:', errorText);
            alert('いいねの処理に失敗しました');
        }
    } catch (error) {
        console.error('いいね切り替えエラー:', error);
        alert('通信エラーが発生しました');
    }
}

// ボタンの表示更新
function updateLikeButton(button, isLiked, count, animate = false) {
    const heartIcon = button.querySelector('.heart-icon');
    const likeCount = button.querySelector('.like-count');

    console.log('Updating button - isLiked:', isLiked, 'count:', count);

    if (isLiked) {
        button.classList.add('liked');
    } else {
        button.classList.remove('liked');
    }

    // アニメーションはクリック時のみ
    if (animate && isLiked) {
        heartIcon.classList.add('animate');
        setTimeout(() => {
            heartIcon.classList.remove('animate');
        }, 300);
    }

    likeCount.textContent = count;
}