// いいね機能の初期化
document.addEventListener('DOMContentLoaded', function () {
    const likeButton = document.querySelector('.like-button');
    if (likeButton) {
        const reviewId = likeButton.getAttribute('data-review-id');
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
            // 自分のレビューにはいいね不可
            if (window.currentUserPublicId && window.reviewOwnerPublicId
                && window.currentUserPublicId === window.reviewOwnerPublicId) {
                alert('自分のレビューにはいいねできません');
                return;
            }
            toggleLike(reviewId, likeButton);
        });
    }

    // コメント機能の初期化
    initComments();
});

// いいね状態の読み込み
async function loadLikeStatus(reviewId, button) {
    try {
        const response = await fetch(`/api/reviews/${reviewId}/like`);
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
        const response = await fetch(`/api/reviews/${reviewId}/like`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (response.status === 401) {
            alert('いいね機能を使用するにはログインが必要です');
            return;
        }
        if (response.ok) {
            const data = await response.json();
            updateLikeButton(button, data.isLiked, data.likeCount, true);
        } else {
            const errorText = await response.text();
            alert('いいねの処理に失敗しました');
        }
    } catch (error) {
        console.error('いいね切り替えエラー:', error);
        alert('通信エラーが発生しました');
    }
}

function updateLikeButton(button, isLiked, count, animate = false) {
    const heartIcon = button.querySelector('.heart-icon');
    const likeCount = button.querySelector('.like-count');
    if (isLiked) {
        button.classList.add('liked');
    } else {
        button.classList.remove('liked');
    }
    if (animate && isLiked) {
        heartIcon.classList.add('animate');
        setTimeout(() => heartIcon.classList.remove('animate'), 300);

        // いいね時に類似レビューを表示
        const reviewId = button.getAttribute('data-review-id');
        loadSimilarReviews(reviewId);
    }
    likeCount.textContent = count;
}

// 類似レビュー読み込み
async function loadSimilarReviews(reviewId) {
    try {
        const response = await fetch(`/ReviewDetails/${reviewId}?handler=SimilarReviews`);
        if (!response.ok) return;
        const reviews = await response.json();
        if (!reviews.length) return;

        const section = document.getElementById('similar-reviews-section');
        const list = document.getElementById('similar-reviews-list');
        if (!section || !list) return;

        list.innerHTML = '';
        reviews.forEach(r => {
            const card = document.createElement('div');
            card.className = 'review-card-small';
            card.setAttribute('data-review-id', r.reviewId);
            card.setAttribute('data-isbn', r.isbn);

            card.innerHTML = `
                <div class="book-cover-small"></div>
                <div class="review-content-small">
                    <div>
                        <div class="review-title">${escapeHtml((r.reviewTitle ?? '無題').substring(0, 15))}</div>
                        <div class="book-title">${escapeHtml(r.bookTitle ?? '')}</div>
                    </div>
                    <div>
                        <div class="reviewer-info">
                            <div class="reviewer-icon" data-public-id="${escapeHtml(r.userPublicId ?? '')}"></div>
                            <span class="reviewer-name">${escapeHtml(r.userName ?? '')}</span>
                        </div>
                    </div>
                </div>
            `;

            list.appendChild(card);
        });

        // 書影をReviewDetailsハンドラーから取得（load_image.jsのURLを上書き）
        const bookCoverEls = list.querySelectorAll('.book-cover-small');
        bookCoverEls.forEach(async (bookCover) => {
            const card = bookCover.closest('.review-card-small');
            const isbn = card.getAttribute('data-isbn');
            if (!isbn) return;
            insertLoadingGif(bookCover);
            try {
                const response = await fetch(`/Index?handler=Image&isbn=${encodeURIComponent(isbn)}`);
                if (!response.ok) { bookCover.innerHTML = ''; return; }
                const blob = await response.blob();
                bookCover.innerHTML = `<img src="${URL.createObjectURL(blob)}" alt="書影" draggable="false"/>`;
            } catch {
                bookCover.innerHTML = '';
            }
        });

        if (typeof loadUserIcons === 'function') {
            loadUserIcons('#similar-reviews-list');
        }

        // ドラッグスクロール処理（home.jsと同等）
        initDragScroll(list);

        section.style.display = 'block';
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                section.classList.add('visible');
                section.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            });
        });
    } catch (e) {
        console.error('類似レビュー読み込みエラー:', e);
    }
}

function initDragScroll(container) {
    let isDown = false;
    let startX;
    let scrollLeft;
    let hasMoved = false;
    const dragThreshold = 5;

    container.addEventListener('mousedown', (e) => {
        isDown = true;
        hasMoved = false;
        startX = e.pageX - container.offsetLeft;
        scrollLeft = container.scrollLeft;
    });

    container.addEventListener('mouseleave', () => {
        isDown = false;
        container.classList.remove('dragging');
    });

    container.addEventListener('mouseup', () => {
        isDown = false;
        container.classList.remove('dragging');
    });

    container.addEventListener('mousemove', (e) => {
        if (!isDown) return;
        const x = e.pageX - container.offsetLeft;
        const distance = Math.abs(x - startX);
        if (distance > dragThreshold) {
            e.preventDefault();
            hasMoved = true;
            container.classList.add('dragging');
            container.scrollLeft = scrollLeft - (x - startX) * 2;
        }
    });

    // カードクリック（ドラッグ中は無効）
    container.querySelectorAll('.review-card-small').forEach(card => {
        card.addEventListener('click', () => {
            if (!hasMoved) {
                window.location.href = `/ReviewDetails/${card.getAttribute('data-review-id')}`;
            }
        });
    });
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
    div.setAttribute('data-comment-id', comment.commentId);

    const formattedTime = formatPostingTime(new Date(comment.postingTime));
    const isOwnComment = window.currentUserPublicId === comment.userPublicId;

    // レビュー投稿者のコメントかどうか判定
    const isReviewAuthor = window.reviewOwnerPublicId && comment.userPublicId === window.reviewOwnerPublicId;

    // 投稿者の場合は専用クラスを付与
    div.className = isReviewAuthor ? 'comment-item review-author-comment' : 'comment-item';

    // 投稿者バッジのHTML（投稿者のみ表示）
    const authorBadgeHtml = isReviewAuthor
        ? `<span class="author-badge">投稿者</span>`
        : '';

    div.innerHTML = `
        <div class="comment-header">
            <div class="user-info">
                <div class="reviewer-icon" data-public-id="${comment.userPublicId}"></div>
                <a href="/user/UserProfile/${comment.userPublicId}" class="user-name">${comment.userName}</a>
                ${authorBadgeHtml}
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
        showCommentError('コメントを入力してください');
        return;
    }

    if (commentText.length > 1000) {
        showCommentError('コメントは1000文字以内で入力してください');
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
            showCommentError('コメントを投稿するにはログインが必要です');
            return;
        }

        if (response.ok) {
            input.value = '';
            const charCount = document.querySelector('.char-count');
            if (charCount) {
                charCount.textContent = '0 / 1000';
            }
            // エラーメッセージをクリア
            clearCommentError();
            await loadComments(reviewId);
        } else {
            const error = await response.json();
            showCommentError(error.error || 'コメントの投稿に失敗しました');
        }
    } catch (error) {
        console.error('コメント投稿エラー:', error);
        showCommentError('通信エラーが発生しました');
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

// コメントエラー表示
function showCommentError(message) {
    // 既存のエラーメッセージを削除
    clearCommentError();

    const commentForm = document.querySelector('.comment-form');
    if (!commentForm) return;

    const errorDiv = document.createElement('div');
    errorDiv.className = 'comment-error-message';
    errorDiv.innerHTML = `
        <svg class="error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
        </svg>
        ${escapeHtml(message)}
    `;
    commentForm.insertBefore(errorDiv, commentForm.firstChild);
}

// コメントエラーをクリア
function clearCommentError() {
    const existingError = document.querySelector('.comment-error-message');
    if (existingError) {
        existingError.remove();
    }
}


// ユーザーアイコン読み込み
function loadUserIcon(iconElement, publicId) {
    if (!iconElement || !publicId) return;

    // load_image.jsの関数を活用する場合
    iconElement.setAttribute('data-public-id', publicId);
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