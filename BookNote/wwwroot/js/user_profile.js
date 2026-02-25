document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);
    const activeTab = urlParams.get('tab') || 'all';
    const parentSection = document.querySelector('.profile-section');
    if (parentSection) {
        const sectionTabButtons = parentSection.querySelectorAll('.tab-button');
        const sectionTabContents = parentSection.querySelectorAll('.tab-content');
        sectionTabButtons.forEach(btn => btn.classList.remove('active'));
        sectionTabContents.forEach(content => content.classList.remove('active'));
        const activeButton = parentSection.querySelector(`.tab-button[data-tab="${activeTab}"]`);
        if (activeButton) {
            activeButton.classList.add('active');
        }
        const activeContent = parentSection.querySelector(`#${activeTab}-reviews-tab`);
        if (activeContent) {
            activeContent.classList.add('active');
        }
    }
    const tabButtons = document.querySelectorAll('.tab-button');
    tabButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            const tabName = this.dataset.tab;
            const url = new URL(window.location);
            url.searchParams.set('tab', tabName);
            url.searchParams.set('page', '1');
            window.location.href = url.toString();
        });
    });
});
document.addEventListener('DOMContentLoaded', function () {
    const followButton = document.getElementById('followButton');
    const blockButton = document.getElementById('blockButton');
    if (followButton && blockButton) {
        if (blockButton.classList.contains('blocking')) {
            followButton.disabled = true;
        }
    }
    if (followButton) {
        followButton.addEventListener('click', async function (e) {
            e.preventDefault();
            if (this.disabled) return;
            const targetUserPublicId = this.getAttribute('data-user-id');
            await toggleFollow(targetUserPublicId);
        });
    }
    if (blockButton) {
        blockButton.addEventListener('click', async function (e) {
            e.preventDefault();
            if (this.disabled) return;
            const targetUserPublicId = this.getAttribute('data-user-id');
            if (this.classList.contains('blocking')) {
                await toggleBlock(targetUserPublicId);
            } else {
                showBlockConfirmModal(targetUserPublicId);
            }
        });
    }
});
function showBlockConfirmModal(targetUserPublicId) {
    const modal = document.createElement('div');
    modal.className = 'block-confirm-modal show';
    modal.innerHTML = `
        <div class="block-confirm-content">
            <h3>ユーザーをブロックしますか？</h3>
            <p>
                ブロックすると以下の効果があります：<br>
                • このユーザーのレビューが表示されなくなります<br>
                • フォローが自動的に解除されます<br>
                • 相手があなたをフォローできなくなります
            </p>
            <div class="block-confirm-buttons">
                <button class="block-cancel-button" id="blockCancelBtn">キャンセル</button>
                <button class="block-execute-button" id="blockExecuteBtn">ブロックする</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    document.getElementById('blockCancelBtn').addEventListener('click', function () {
        modal.remove();
    });
    modal.addEventListener('click', function (e) {
        if (e.target === modal) {
            modal.remove();
        }
    });
    document.getElementById('blockExecuteBtn').addEventListener('click', async function () {
        modal.remove();
        await toggleBlock(targetUserPublicId);
    });
}
function disableBothButtons() {
    const followButton = document.getElementById('followButton');
    const blockButton = document.getElementById('blockButton');
    if (followButton) followButton.disabled = true;
    if (blockButton) blockButton.disabled = true;
}
function enableButtons() {
    const followButton = document.getElementById('followButton');
    const blockButton = document.getElementById('blockButton');
    if (blockButton) blockButton.disabled = false;
    if (followButton && !blockButton?.classList.contains('blocking')) {
        followButton.disabled = false;
    }
}
async function toggleFollow(targetUserPublicId) {
    const followButton = document.getElementById('followButton');
    const blockButton = document.getElementById('blockButton');
    if (blockButton?.classList.contains('blocking')) {
        alert('ブロック中のユーザーはフォローできません');
        return;
    }
    disableBothButtons();
    try {
        const currentPath = window.location.pathname;
        const url = `${currentPath}?handler=ToggleFollow`;
        console.log('Follow request URL:', url);
        console.log('Target user:', targetUserPublicId);
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ targetUserPublicId: targetUserPublicId })
        });
        console.log('Response status:', response.status);
        console.log('Response content-type:', response.headers.get('content-type'));
        if (!response.ok) {
            const text = await response.text();
            console.error('Response text:', text);
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            const text = await response.text();
            console.error('Non-JSON response:', text);
            throw new Error('サーバーからの応答が正しくありません');
        }
        const result = await response.json();
        console.log('Follow result:', result);
        if (result.success) {
            updateFollowButton(result.isFollowing);
        } else {
            alert(result.message || 'エラーが発生しました');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('通信エラーが発生しました: ' + error.message);
    } finally {
        enableButtons();
    }
}
async function toggleBlock(targetUserPublicId) {
    const followButton = document.getElementById('followButton');
    const blockButton = document.getElementById('blockButton');
    disableBothButtons();
    try {
        const currentPath = window.location.pathname;
        const url = `${currentPath}?handler=ToggleBlock`;
        console.log('Block request URL:', url);
        console.log('Target user:', targetUserPublicId);
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ targetUserPublicId: targetUserPublicId })
        });
        console.log('Response status:', response.status);
        if (!response.ok) {
            const text = await response.text();
            console.error('Response text:', text);
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const contentType = response.headers.get('content-type');
        if (!contentType || !contentType.includes('application/json')) {
            const text = await response.text();
            console.error('Non-JSON response:', text);
            throw new Error('サーバーからの応答が正しくありません');
        }
        const result = await response.json();
        console.log('Block result:', result);
        if (result.success) {
            updateBlockButton(result.isBlocking);
            if (result.isBlocking) {
                updateFollowButton(false);
                if (followButton) {
                    followButton.disabled = true;
                }
            } else {
                if (followButton) {
                    followButton.disabled = false;
                }
            }
        } else {
            alert(result.message || 'エラーが発生しました');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('通信エラーが発生しました: ' + error.message);
    } finally {
        enableButtons();
    }
}
function updateFollowButton(isFollowing) {
    const followButton = document.getElementById('followButton');
    if (!followButton) return;
    if (isFollowing) {
        followButton.classList.add('following');
        followButton.textContent = 'フォロー中';
    } else {
        followButton.classList.remove('following');
        followButton.textContent = 'フォロー';
    }
}
function updateBlockButton(isBlocking) {
    const blockButton = document.getElementById('blockButton');
    if (!blockButton) return;
    if (isBlocking) {
        blockButton.classList.add('blocking');
        blockButton.textContent = 'ブロック中';
    } else {
        blockButton.classList.remove('blocking');
        blockButton.textContent = 'ブロック';
    }
}