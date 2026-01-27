// タブ切り替え機能
document.addEventListener('DOMContentLoaded', function () {
    // 初期表示時にURLパラメータに基づいてタブを表示
    const urlParams = new URLSearchParams(window.location.search);
    const activeTab = urlParams.get('tab') || 'all';

    const parentSection = document.querySelector('.profile-section');
    if (parentSection) {
        const sectionTabButtons = parentSection.querySelectorAll('.tab-button');
        const sectionTabContents = parentSection.querySelectorAll('.tab-content');

        // すべてのタブボタンとコンテンツをリセット
        sectionTabButtons.forEach(btn => btn.classList.remove('active'));
        sectionTabContents.forEach(content => content.classList.remove('active'));

        // アクティブなタブを設定
        const activeButton = parentSection.querySelector(`.tab-button[data-tab="${activeTab}"]`);
        if (activeButton) {
            activeButton.classList.add('active');
        }

        const activeContent = parentSection.querySelector(`#${activeTab}-reviews-tab`);
        if (activeContent) {
            activeContent.classList.add('active');
        }
    }

    // タブボタンのクリックイベント
    const tabButtons = document.querySelectorAll('.tab-button');
    tabButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const tabName = this.dataset.tab;

            // URL更新(ページ番号を1にリセット)
            const url = new URL(window.location);
            url.searchParams.set('tab', tabName);
            url.searchParams.set('page', '1');
            window.location.href = url.toString();
        });
    });
});