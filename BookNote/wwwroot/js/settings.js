// 設定ページの機能（Cropper.js使用 - 正方形のみ）

let cropper = null;

document.addEventListener('DOMContentLoaded', function () {
    initializeProfileCounter();
    initializeIconUpload();
    initializeFormValidation();
});

// プロフィール文字数カウント
function initializeProfileCounter() {
    const profileTextarea = document.querySelector('textarea[name="Profile"]');
    const profileCount = document.getElementById('profile-count');

    if (profileTextarea && profileCount) {
        profileTextarea.addEventListener('input', function () {
            profileCount.textContent = this.value.length;
        });
    }
}

// アイコンアップロード機能
function initializeIconUpload() {
    const selectIconBtn = document.getElementById('select-icon-btn');
    const iconInput = document.getElementById('icon-input');
    const cropModal = document.getElementById('crop-modal');
    const cropModalClose = document.getElementById('crop-modal-close');
    const cropCancel = document.getElementById('crop-cancel');
    const cropApply = document.getElementById('crop-apply');

    if (!selectIconBtn || !iconInput || !cropModal) {
        return;
    }

    // 画像選択ボタン
    selectIconBtn.addEventListener('click', function (e) {
        e.preventDefault();
        iconInput.click();
    });

    // ファイル選択時
    iconInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (!file) {
            return;
        }

        // ファイルタイプチェック
        if (!file.type.startsWith('image/')) {
            alert('画像ファイルを選択してください');
            iconInput.value = '';
            return;
        }

        // ファイルサイズチェック (10MB)
        const maxSize = 10 * 1024 * 1024;
        if (file.size > maxSize) {
            alert('ファイルサイズは10MB以下にしてください');
            iconInput.value = '';
            return;
        }

        // 画像を読み込んでモーダルを表示
        loadImageForCrop(file);
    });

    // モーダルを閉じる
    cropModalClose.addEventListener('click', closeCropModal);
    cropCancel.addEventListener('click', closeCropModal);

    // モーダル外クリックで閉じる
    cropModal.addEventListener('click', function (e) {
        if (e.target === cropModal) {
            closeCropModal();
        }
    });

    // 切り取り適用
    cropApply.addEventListener('click', applyCrop);
}

// 画像を読み込んでCropperを初期化
function loadImageForCrop(file) {
    const reader = new FileReader();
    const cropImage = document.getElementById('crop-image');
    const cropModal = document.getElementById('crop-modal');

    reader.onload = function (e) {
        cropImage.src = e.target.result;
        cropModal.classList.add('active');

        // 既存のCropperを破棄
        if (cropper) {
            cropper.destroy();
        }

        // Cropperを初期化（正方形）
        cropper = new Cropper(cropImage, {
            aspectRatio: 1,
            viewMode: 1,
            dragMode: 'move',
            autoCropArea: 0.8,
            restore: false,
            guides: true,
            center: true,
            highlight: false,
            cropBoxMovable: true,
            cropBoxResizable: true,
            toggleDragModeOnDblclick: false,
        });
    };

    reader.onerror = function () {
        alert('画像の読み込みに失敗しました');
    };

    reader.readAsDataURL(file);
}

// 切り取りを適用
function applyCrop() {
    if (!cropper) {
        return;
    }

    // 切り取った画像を取得（512x512）
    const canvas = cropper.getCroppedCanvas({
        width: 512,
        height: 512,
        imageSmoothingEnabled: true,
        imageSmoothingQuality: 'high',
    });

    if (!canvas) {
        alert('画像の切り取りに失敗しました');
        return;
    }

    // Base64に変換
    const base64Image = canvas.toDataURL('image/png', 0.9);

    // プレビューに表示
    const previewIcon = document.getElementById('preview-icon');
    if (previewIcon) {
        const img = document.createElement('img');
        img.src = base64Image;
        img.style.width = '100%';
        img.style.height = '100%';
        img.style.objectFit = 'cover';

        previewIcon.innerHTML = '';
        previewIcon.appendChild(img);
    }

    // Base64データをhiddenフィールドに設定
    const iconBase64Input = document.getElementById('icon-base64');
    if (iconBase64Input) {
        iconBase64Input.value = base64Image;
    }

    // モーダルを閉じる
    closeCropModal();
}

// モーダルを閉じる
function closeCropModal() {
    const cropModal = document.getElementById('crop-modal');
    const iconInput = document.getElementById('icon-input');

    if (cropModal) {
        cropModal.classList.remove('active');
    }

    // Cropperを破棄
    if (cropper) {
        cropper.destroy();
        cropper = null;
    }

    // ファイル入力をリセット
    if (iconInput) {
        iconInput.value = '';
    }
}

// フォームバリデーション
function initializeFormValidation() {
    const settingsForm = document.getElementById('settings-form');

    if (!settingsForm) {
        return;
    }

    settingsForm.addEventListener('submit', function (e) {
        const userNameInput = document.querySelector('input[name="UserName"]');
        const profileTextarea = document.querySelector('textarea[name="Profile"]');

        if (!userNameInput) {
            return;
        }

        const userName = userNameInput.value.trim();

        // ユーザー名チェック
        if (!userName) {
            e.preventDefault();
            alert('ユーザー名を入力してください');
            userNameInput.focus();
            return false;
        }

        if (userName.length > 50) {
            e.preventDefault();
            alert('ユーザー名は50文字以内で入力してください');
            userNameInput.focus();
            return false;
        }

        // プロフィールチェック
        if (profileTextarea && profileTextarea.value.length > 500) {
            e.preventDefault();
            alert('プロフィールは500文字以内で入力してください');
            profileTextarea.focus();
            return false;
        }

        // 送信確認
        const confirmMessage = '設定を保存しますか?';
        if (!confirm(confirmMessage)) {
            e.preventDefault();
            return false;
        }
    });
}