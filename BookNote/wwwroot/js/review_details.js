// いいね機能の初期化
document.addEventListener('DOMContentLoaded', function () {
    const likeButton = document.querySelector('.like-button');
    if (likeButton) {
        const reviewId = likeButton.getAttribute('data-review-id');
        console.log('Review ID:', reviewId);
        loadLikeStatus(reviewId, likeButton);
        likeButton.addEventListener('click', function (e) {
            e.preventDefault();
            if (!window.isUserAuthenticated) {
                alert('いいね機能を使用するにはログインが必要です');
                return;
            }
            if (likeButton.classList.contains('disabled')) {
                return;
            }
            console.log('Like button clicked');
            toggleLike(reviewId, likeButton);
        });
    }

    // コメント機能の初期化
    initComments();
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
            updateLikeButton(button, data.isLiked, data.likeCount, false);
        }
    } catch (error) {
        console.error('いいね状態の読み込みエラー:', error);
    }
}

// いいねの切り替え
async function toggleLike(reviewId, button) {
    if (!window.isUserAuthenticated) {
        alert('いいね機能を使用するにはログインが必要です');
        return;
    }
    try {
        console.log('Toggling like for review:', reviewId);
        const response = await fetch(`/api/reviews/${reviewId}/like`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        console.log('Toggle response status:', response.status);
        if (response.status === 401) {
            alert('いいね機能を使用するにはログインが必要です');
            return;
        }
        if (response.ok) {
            const data = await response.json();
            console.log('Toggle result:', data);
            updateLikeButton(button, data.isLiked, data.likeCount, true);
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
    if (animate && isLiked) {
        heartIcon.classList.add('animate');
        setTimeout(() => {
            heartIcon.classList.remove('animate');
        }, 300);
    }
    likeCount.textContent = count;
}

// ========== コメント機能 ==========

function initComments() {
    const reviewCard = document.querySelector('.review-card-large');
    if (!reviewCard) return;

    const reviewId = document.querySelector('.like-button')?.getAttribute('data-review-id');
    if (!reviewId) return;

    loadComments(reviewId);

    // コメント投稿フォームのイベント
    const submitBtn = document.getElementById('submit-comment');
    if (submitBtn) {
        submitBtn.addEventListener('click', () => submitComment(reviewId));
    }

    const commentInput = document.getElementById('comment-input');
    if (commentInput) {
        commentInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                submitComment(reviewId);
            }
        });
    }
}

// コメント読み込み
async function loadComments(reviewId) {
    try {
        const [commentsResponse, countResponse] = await Promise.all([
            fetch(`/api/reviews/${reviewId}/comments?limit=5`),
            fetch(`/api/reviews/${reviewId}/comments/count`)
        ]);

        if (commentsResponse.ok && countResponse.ok) {
            const comments = await commentsResponse.json();
            const countData = await countResponse.json();
            displayComments(comments, countData.count, reviewId);
        }
    } catch (error) {
        console.error('コメント読み込みエラー:', error);
    }
}

// コメント表示
function displayComments(comments, totalCount, reviewId) {
    const container = document.getElementById('comments-container');
    if (!container) return;

    const header = document.querySelector('.comments-header span');
    if (header) {
        header.textContent = `コメント (${totalCount})`;
    }

    const list = document.getElementById('comments-list');
    if (!list) return;

    list.innerHTML = '';

    comments.forEach(comment => {
        const commentEl = createCommentElement(comment, reviewId);
        list.appendChild(commentEl);
    });

    if (typeof loadUserIcons === 'function') {
        loadUserIcons('#comments-list');
    }

    // 「すべて見る」リンクの表示
    const viewAllLink = document.getElementById('view-all-comments');
    if (viewAllLink) {
        if (totalCount > 5) {
            viewAllLink.style.display = 'block';
            viewAllLink.href = `/review/${reviewId}/comments`;
        } else {
            viewAllLink.style.display = 'none';
        }
    }
}

// コメント要素作成
function createCommentElement(comment, reviewId) {
    const div = document.createElement('div');
    div.className = 'comment-item';
    div.setAttribute('data-comment-id', comment.commentId);

    const formattedTime = formatPostingTime(new Date(comment.postingTime));
    const isOwnComment = window.currentUserPublicId === comment.userPublicId;

    div.innerHTML = `
        <div class="comment-header">
            <div class="user-info">
                <div class="reviewer-icon" data-public-id="${comment.userPublicId}"></div>
                <a href="/user/UserProfile/${comment.userPublicId}" class="user-name">${comment.userName}</a>
                <span class="post-time">${formattedTime}</span>
            </div>
            ${isOwnComment ? `<button class="delete-comment-btn" data-comment-id="${comment.commentId}">削除</button>` : ''}
        </div>
        <div class="comment-text">${escapeHtml(comment.commentText)}</div>
    `;

    // 削除ボタンのイベント
    const deleteBtn = div.querySelector('.delete-comment-btn');
    if (deleteBtn) {
        deleteBtn.addEventListener('click', () => deleteComment(reviewId, comment.commentId));
    }

    // アイコン読み込みは後でまとめて行う
    return div;
}

// コメント投稿
async function submitComment(reviewId) {
    if (!window.isUserAuthenticated) {
        alert('コメントを投稿するにはログインが必要です');
        return;
    }

    const input = document.getElementById('comment-input');
    if (!input) return;

    const commentText = input.value.trim();
    if (!commentText) {
        alert('コメントを入力してください');
        return;
    }

    if (commentText.length > 1000) {
        alert('コメントは1000文字以内で入力してください');
        return;
    }

    try {
        const response = await fetch(`/api/reviews/${reviewId}/comments`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ commentText })
        });

        if (response.status === 401) {
            alert('コメントを投稿するにはログインが必要です');
            return;
        }

        if (response.ok) {
            input.value = '';
            const charCount = document.querySelector('.char-count');
            if (charCount) {
                charCount.textContent = '0 / 1000';
            }
            await loadComments(reviewId);
        } else {
            const error = await response.json();
            alert(error.error || 'コメントの投稿に失敗しました');
        }
    } catch (error) {
        console.error('コメント投稿エラー:', error);
        alert('通信エラーが発生しました');
    }
}

// コメント削除
async function deleteComment(reviewId, commentId) {
    if (!confirm('このコメントを削除しますか?')) {
        return;
    }

    try {
        const response = await fetch(`/api/reviews/${reviewId}/comments/${commentId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            await loadComments(reviewId);
        } else {
            alert('削除に失敗しました');
        }
    } catch (error) {
        console.error('コメント削除エラー:', error);
        alert('通信エラーが発生しました');
    }
}

// ユーザーアイコン読み込み
function loadUserIcon(iconElement, publicId) {
    if (!iconElement || !publicId) return;

    // load_image.jsの関数を活用する場合
    iconElement.setAttribute('data-public-id', publicId);

    // または直接実装
    //fetch(`/ReviewDetails/OnGetUserIcon?publicId=${publicId}`)
    //    .then(response => {
    //        if (!response.ok) throw new Error('Failed to load icon');
    //        return response.blob();
    //    })
    //    .then(blob => {
    //        const imageUrl = URL.createObjectURL(blob);
    //        const img = document.createElement('img');
    //        img.src = imageUrl;
    //        img.alt = 'User Icon';
    //        img.setAttribute('draggable', 'false');
    //        iconElement.innerHTML = '';
    //        iconElement.appendChild(img);
    //    })
    //    .catch(error => {
    //        console.error('Icon load error:', error);
    //        iconElement.textContent = publicId.substring(0, 2).toUpperCase();
    //    });
}

// 日時フォーマット（既存のStaticEvent.FormatPostingTimeと同等の処理）
function formatPostingTime(date) {
    const now = new Date();
    const diff = now - date;
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (seconds < 60) return 'たった今';
    if (minutes < 60) return `${minutes}分前`;
    if (hours < 24) return `${hours}時間前`;
    if (days < 7) return `${days}日前`;

    return date.toLocaleDateString('ja-JP');
}

// HTMLエスケープ
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}