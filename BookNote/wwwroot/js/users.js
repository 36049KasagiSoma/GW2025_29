document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);
    const activeTab = urlParams.get('tab') || 'recommend';
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    const tabElement = document.querySelector(`.tab[data-tab="${activeTab}"]`);
    const contentElement = document.getElementById(`${activeTab}-content`);
    if (tabElement) tabElement.classList.add('active');
    if (contentElement) contentElement.classList.add('active');
    document.querySelectorAll('.tab').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            tab.classList.add('active');
            const tabName = tab.getAttribute('data-tab');
            document.getElementById(`${tabName}-content`).classList.add('active');
        });
    });
    if (typeof loadUserIcons === 'function') {
        loadUserIcons('#recommend-users');
    }
    document.getElementById('btnUserSearch')?.addEventListener('click', () => {
        performUserSearch();
    });
    document.getElementById('userSearchKeyword')?.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            performUserSearch();
        }
    });
    document.getElementById('userSearchType')?.addEventListener('change', () => {
        const keyword = document.getElementById('userSearchKeyword')?.value.trim();
        if (keyword) {
            performUserSearch();
        }
    });
});
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
async function performUserSearch() {
    const keyword = document.getElementById('userSearchKeyword')?.value.trim();
    const searchType = document.getElementById('userSearchType')?.value || 'name';
    const resultsContainer = document.getElementById('user-search-results');
    if (!keyword) {
        resultsContainer.innerHTML = '<p class="empty-message">検索キーワードを入力してください</p>';
        return;
    }
    resultsContainer.innerHTML = '<p class="empty-message">検索中...</p>';
    try {
        const response = await fetch(
            `/api/users/search?keyword=${encodeURIComponent(keyword)}&searchType=${encodeURIComponent(searchType)}`
        );
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const users = await response.json();
        if (users.length === 0) {
            resultsContainer.innerHTML = '<p class="empty-message">ユーザーが見つかりませんでした</p>';
            return;
        }
        resultsContainer.innerHTML = users.map(user => `
            <div class="user-card" onclick="location.href='/user/UserProfile/${escapeHtml(user.userPublicId)}'">
                <div class="user-card-icon reviewer-icon" data-public-id="${escapeHtml(user.userPublicId)}"></div>
                <div class="user-card-info">
                    <div class="user-card-name">${escapeHtml(user.userName)}</div>
                    <div class="user-card-id">@${escapeHtml(user.userPublicId)}</div>
                    ${user.userProfile ? `<div class="user-card-profile">${escapeHtml(user.userProfile)}</div>` : ''}
                </div>
            </div>
        `).join('');
        if (typeof loadUserIcons === 'function') {
            loadUserIcons('#user-search-results');
        }
    } catch (error) {
        resultsContainer.innerHTML = '<p class="empty-message">検索エラーが発生しました</p>';
        console.error('User search error:', error);
    }
}