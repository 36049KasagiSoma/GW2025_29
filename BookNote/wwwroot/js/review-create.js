function showDraftModal() {
    document.getElementById('draftModal').classList.add('active');
}
function closeDraftModal() {
    document.getElementById('draftModal').classList.remove('active');
}
async function deleteDraft(draftId) {
    if (!confirm('この下書きを削除しますか?')) return;
    try {
        const response = await fetch(`/review_create/SelectType?handler=DeleteDraft&draftId=${draftId}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        });
        if (response.ok) {
            location.reload();
        }
    } catch (error) {
        console.error('削除エラー:', error);
        alert('下書きの削除に失敗しました');
    }
}
document.getElementById('draftModal')?.addEventListener('click', (e) => {
    if (e.target.id === 'draftModal') {
        closeDraftModal();
    }
});