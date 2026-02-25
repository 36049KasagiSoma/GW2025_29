let cropper = null;
document.addEventListener('DOMContentLoaded', function () {
    initializeProfileCounter();
    initializeIconUpload();
    initializeFormValidation();
});
function initializeProfileCounter() {
    const profileTextarea = document.querySelector('textarea[name="Profile"]');
    const profileCount = document.getElementById('profile-count');
    if (profileTextarea && profileCount) {
        profileTextarea.addEventListener('input', function () {
            profileCount.textContent = this.value.length;
        });
    }
}
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
    selectIconBtn.addEventListener('click', function (e) {
        e.preventDefault();
        iconInput.click();
    });
    iconInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (!file) {
            return;
        }
        if (!file.type.startsWith('image/')) {
            alert('画像ファイルを選択してください');
            iconInput.value = '';
            return;
        }
        const maxSize = 10 * 1024 * 1024;
        if (file.size > maxSize) {
            alert('ファイルサイズは10MB以下にしてください');
            iconInput.value = '';
            return;
        }
        loadImageForCrop(file);
    });
    cropModalClose.addEventListener('click', closeCropModal);
    cropCancel.addEventListener('click', closeCropModal);
    cropModal.addEventListener('click', function (e) {
        if (e.target === cropModal) {
            closeCropModal();
        }
    });
    cropApply.addEventListener('click', applyCrop);
}
function loadImageForCrop(file) {
    const reader = new FileReader();
    const cropImage = document.getElementById('crop-image');
    const cropModal = document.getElementById('crop-modal');
    reader.onload = function (e) {
        cropImage.src = e.target.result;
        cropModal.classList.add('active');
        if (cropper) {
            cropper.destroy();
        }
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
function applyCrop() {
    if (!cropper) {
        return;
    }
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
    const base64Image = canvas.toDataURL('image/png', 0.9);
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
    const iconBase64Input = document.getElementById('icon-base64');
    if (iconBase64Input) {
        iconBase64Input.value = base64Image;
    }
    closeCropModal();
}
function closeCropModal() {
    const cropModal = document.getElementById('crop-modal');
    const iconInput = document.getElementById('icon-input');
    if (cropModal) {
        cropModal.classList.remove('active');
    }
    if (cropper) {
        cropper.destroy();
        cropper = null;
    }
    if (iconInput) {
        iconInput.value = '';
    }
}
function initializeFormValidation() {
    const settingsForm = document.getElementById('settings-form');
    if (!settingsForm) {
        return;
    }
    settingsForm.addEventListener('submit', function (e) {
        const userNameInput = document.querySelector('input[name="UserName"]');
        const profileTextarea = document.querySelector('textarea[name="Profile"]');
        const submitButton = this.querySelector('button[type="submit"]');
        const cancelButton = this.querySelector('a.btn-secondary');
        const selectIconBtn = document.getElementById('select-icon-btn');
        if (!userNameInput) {
            return;
        }
        const userName = userNameInput.value.trim();
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
        if (profileTextarea && profileTextarea.value.length > 500) {
            e.preventDefault();
            alert('プロフィールは500文字以内で入力してください');
            profileTextarea.focus();
            return false;
        }
        const confirmMessage = '設定を保存しますか?';
        if (!confirm(confirmMessage)) {
            e.preventDefault();
            return false;
        }
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.textContent = '保存中...';
        }
        if (cancelButton) {
            cancelButton.style.pointerEvents = 'none';
            cancelButton.style.opacity = '0.5';
        }
        if (selectIconBtn) {
            selectIconBtn.disabled = true;
            selectIconBtn.style.opacity = '0.5';
        }
    });
}