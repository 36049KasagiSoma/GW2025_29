// ドロワーの開閉
function toggleDrawer() {
    const drawer = document.querySelector('.drawer');
    const overlay = document.querySelector('.overlay');
    const hamburger = document.querySelector('.hamburger');

    drawer.classList.toggle('open');
    overlay.classList.toggle('active');
    hamburger.classList.toggle('open');
}

// ログイン・ログアウトボタンの二重押し防止
document.addEventListener('DOMContentLoaded', () => {

    // ログインボタン（onclick="location.href=..."）
    const loginButton = document.querySelector('.auth-button:not(.auth-button-logout)');
    if (loginButton) {
        loginButton.addEventListener('click', () => {
            loginButton.disabled = true;
            loginButton.textContent = '移動中...';
        });
    }

    // ログアウトボタン（form submit）
    const logoutForm = document.querySelector('form[action="/Account/Logout"]');
    if (logoutForm) {
        let isSubmitting = false;
        logoutForm.addEventListener('submit', (e) => {
            if (isSubmitting) {
                e.preventDefault();
                return;
            }
            isSubmitting = true;
            const logoutButton = logoutForm.querySelector('button[type="submit"]');
            // setTimeout で送信後に非活性化（送信処理を妨げない）
            setTimeout(() => {
                logoutButton.disabled = true;
                logoutButton.textContent = 'ログアウト中...';
            }, 0);
        });
    }
});