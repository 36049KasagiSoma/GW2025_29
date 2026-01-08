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

// 検索機能（デモ用）
document.querySelector('.search-button')?.addEventListener('click', () => {
    const searchInput = document.querySelector('.search-input');
    const resultsContainer = document.getElementById('search-results');

    if (searchInput.value.trim()) {
        resultsContainer.innerHTML = `
            <div class="review-card-large">
                <div class="book-cover-large">書影</div>
                <div class="review-content-large">
                    <div class="review-title">検索結果: ${searchInput.value}</div>
                    <div class="book-title">該当する書籍レビュー</div>
                    <div class="review-text">
                        検索キーワードに該当するレビューが表示されます...
                    </div>
                    <div class="reviewer-info">
                        <div class="reviewer-icon"></div>
                        <span class="reviewer-name">検索ユーザー</span>
                        <span class="post-time">検索結果</span>
                    </div>
                </div>
            </div>
        `;
    } else {
        resultsContainer.innerHTML = '<p class="empty-message">検索キーワードを入力してください</p>';
    }
});

// Enterキーで検索
document.querySelector('.search-input')?.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        document.querySelector('.search-button').click();
    }
});
