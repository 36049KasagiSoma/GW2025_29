// レビュー一覧Js
// タブ切り替え機能
document.querySelectorAll('.tab').forEach(tab => {
    tab.addEventListener('click', () => {
        // すべてのタブとコンテンツから active クラスを削除
        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
        // クリックされたタブとそのコンテンツに active クラスを追加
        tab.classList.add('active');
        const tabName = tab.getAttribute('data-tab');
        document.getElementById(`${tabName}-content`).classList.add('active');
    });
});

// 検索機能
document.getElementById('btnSearch')?.addEventListener('click', () => {
    performSearch();
});

document.getElementById('searchKeyword')?.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        performSearch();
    }
});

document.getElementById('searchSortOrder')?.addEventListener('change', () => {
    performSearch();
});

document.addEventListener('DOMContentLoaded', function () {

    const urlParams = new URLSearchParams(window.location.search);
    const activeTab = urlParams.get('tab') || 'popular'; // 指定なしは人気タブ

    // すべてのタブとコンテンツから active を外す
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

    // 対象タブとコンテンツに active を付与
    const tabElement = document.querySelector(`.tab[data-tab="${activeTab}"]`);
    const contentElement = document.getElementById(`${activeTab}-content`);
    if (tabElement) tabElement.classList.add('active');
    if (contentElement) contentElement.classList.add('active');

    // 検索ボタンのイベント
    const searchButton = document.querySelector('.search-button');
    if (searchButton) {
        searchButton.addEventListener('click', performSearch);
    }

    // Enterキーでの検索
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                performSearch();
            }
        });
    }

    // ソート順変更時の検索
    const sortSelect = document.querySelector('.filter-select');
    if (sortSelect) {
        sortSelect.addEventListener('change', () => {
            const searchInput = document.querySelector('.search-input');
            if (searchInput && searchInput.value.trim()) {
                performSearch();
            }
        });
    }
});

async function formatDate(dateString) {
    try {
        const response = await fetch(`/api/utility/format-date?date=${encodeURIComponent(dateString)}`);
        const result = await response.text();
        return result;
    } catch (error) {
        console.error('Format date error:', error);
        return dateString;
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function performSearch() {
    const searchInput = document.querySelector('.search-input');
    const sortSelect = document.querySelector('.filter-select');
    const resultsContainer = document.getElementById('search-results');

    const keyword = searchInput.value.trim();
    const sortOrder = sortSelect.value;

    if (!keyword) {
        resultsContainer.innerHTML = '<p class="empty-message">検索キーワードを入力してください</p>';
        return;
    }

    resultsContainer.innerHTML = '<p class="empty-message">検索中...</p>';

    try {
        const response = await fetch(`/api/reviews/search?keyword=${encodeURIComponent(keyword)}&sortOrder=${sortOrder}`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const reviews = await response.json();

        if (reviews.length === 0) {
            resultsContainer.innerHTML = '<p class="empty-message">検索結果が見つかりませんでした</p>';
            return;
        }

        const formattedReviews = await Promise.all(reviews.map(async review => ({
            ...review,
            formattedTime: await formatDate(review.postingTime)
        })));

        resultsContainer.innerHTML = formattedReviews.map(review => `
            <div class="review-card-large" data-isbn="${escapeHtml(review.isbn)}" data-review-id="${review.reviewId}">
                <div class="book-cover-large">書影</div>
                <div class="review-content-large">
                    <div class="review-title">${escapeHtml(review.title)}</div>
                    <div class="book-title">${escapeHtml(review.book.title)}</div>
                    <div class="review-text">${escapeHtml(review.review)}</div>
                    <div class="reviewer-info">
                        <div class="reviewer-icon" data-public-id="${escapeHtml(review.user.userPublicId)}"></div>
                        <span class="reviewer-name">${escapeHtml(review.user.userName)}</span>
                        <span class="post-time">${escapeHtml(review.formattedTime)}</span>
                    </div>
                </div>
            </div>
        `).join('');
        await loadBookImages('#search-results');
        await loadUserIcons('#search-results');
        setupCardClickEvents('#search-results');
    } catch (error) {
        resultsContainer.innerHTML = '<p class="empty-message">検索エラーが発生しました</p>';
        console.error('Search error:', error);
    }
}

// Enterキーで検索
document.querySelector('.search-input')?.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        document.querySelector('.search-button').click();
    }
});