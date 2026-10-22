/**
 * Employee Portal Client Script (.NET Core MVC)
 */

// Tab switching
function switchEmpTab(tabId, btn) {
    document.querySelectorAll('.emp-tab-content').forEach(tab => tab.classList.remove('active'));
    document.querySelectorAll('.dashboard-sidebar .nav-tab-btn').forEach(b => b.classList.remove('active'));

    const targetTab = document.getElementById(tabId);
    if (targetTab) targetTab.classList.add('active');

    if (btn) {
        btn.classList.add('active');
    } else {
        const matchingBtn = document.querySelector(`.dashboard-sidebar button[data-tab="${tabId}"]`);
        if (matchingBtn) matchingBtn.classList.add('active');
    }
}

// Quick Manual Punch
async function handleQuickManualPunch(action) {
    try {
        const res = await fetch('/api/employee/attendance/punch', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ action })
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast(data.message || 'Attendance logged successfully!', 'success');
            setTimeout(() => window.location.reload(), 800);
        } else {
            showToast(data.message || 'Punch operation failed.', 'error');
        }
    } catch (err) {
        showToast('Server error while recording punch.', 'error');
    }
}

// Leave Application
async function handleApplyLeave(e) {
    e.preventDefault();
    const payload = {
        leaveType: document.getElementById('leaveType').value,
        fromDate: document.getElementById('leaveFromDate').value,
        toDate: document.getElementById('leaveToDate').value,
        reason: document.getElementById('leaveReason').value.trim()
    };

    if (new Date(payload.toDate) < new Date(payload.fromDate)) {
        showToast('To Date cannot be earlier than From Date.', 'warning');
        return;
    }

    try {
        const res = await fetch('/api/employee/leaves', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Leave request submitted successfully!', 'success');
            setTimeout(() => window.location.reload(), 800);
        } else {
            showToast(data.message || 'Failed to submit leave.', 'error');
        }
    } catch (err) {
        showToast('Server error during leave application.', 'error');
    }
}

// Mark Notification as Read
async function markNotificationRead(id, btn) {
    try {
        const res = await fetch(`/api/employee/notifications/${id}/read`, { method: 'PUT' });
        if (res.ok) {
            const card = document.querySelector(`.notif-card[data-id="${id}"]`);
            if (card) {
                card.classList.remove('notif-unread');
                card.classList.add('notif-read');
            }
            if (btn) btn.remove();
            showToast('Marked as read.', 'info');
        }
    } catch (err) {
        // silent catch
    }
}

// Update Profile Contact Info
async function handleUpdateProfile(e) {
    e.preventDefault();
    const payload = {
        phone: document.getElementById('profilePhone').value.trim(),
        address: document.getElementById('profileAddress').value.trim()
    };

    try {
        const res = await fetch('/api/employee/profile', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Contact info updated successfully!', 'success');
        } else {
            showToast(data.message || 'Failed to update profile.', 'error');
        }
    } catch (err) {
        showToast('Server error updating profile.', 'error');
    }
}

// Change Password
async function handleChangePassword(e) {
    e.preventDefault();
    const currentPassword = document.getElementById('currentPassword').value;
    const newPassword = document.getElementById('newPassword').value;
    const confirmNewPassword = document.getElementById('confirmNewPassword').value;

    if (newPassword !== confirmNewPassword) {
        showToast('New passwords do not match.', 'warning');
        return;
    }

    try {
        const res = await fetch('/api/employee/profile/password', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ currentPassword, newPassword, confirmPassword: confirmNewPassword })
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Password changed successfully!', 'success');
            document.getElementById('currentPassword').value = '';
            document.getElementById('newPassword').value = '';
            document.getElementById('confirmNewPassword').value = '';
        } else {
            showToast(data.message || 'Failed to change password.', 'error');
        }
    } catch (err) {
        showToast('Server error while changing password.', 'error');
    }
}

// ================= Profile Photo Upload =================
let selectedPhotoFile = null;

function previewSelectedPhoto(input) {
    selectedPhotoFile = null;
    const btn = document.getElementById('uploadPhotoBtn');
    if (!input.files || input.files.length === 0) {
        if (btn) btn.disabled = true;
        return;
    }
    const file = input.files[0];
    if (file.size > 2 * 1024 * 1024) {
        showToast('Photo must be under 2MB.', 'warning');
        input.value = '';
        if (btn) btn.disabled = true;
        return;
    }
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
        showToast('Only JPG, PNG, or WEBP images allowed.', 'warning');
        input.value = '';
        if (btn) btn.disabled = true;
        return;
    }
    selectedPhotoFile = file;
    if (btn) btn.disabled = false;

    // Show live preview
    const reader = new FileReader();
    reader.onload = function(e) {
        const preview = document.getElementById('currentPhotoPreview');
        if (preview) {
            if (preview.tagName === 'IMG') {
                preview.src = e.target.result;
            } else {
                const img = document.createElement('img');
                img.src = e.target.result;
                img.className = 'photo-preview-img';
                img.id = 'currentPhotoPreview';
                preview.parentNode.replaceChild(img, preview);
            }
        }
    };
    reader.readAsDataURL(file);
}

async function handleUploadPhoto() {
    if (!selectedPhotoFile) {
        showToast('Please choose a photo first.', 'warning');
        return;
    }
    const btn = document.getElementById('uploadPhotoBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Uploading...'; }

    try {
        const formData = new FormData();
        formData.append('photo', selectedPhotoFile);

        const res = await fetch('/api/employee/profile/photo', {
            method: 'POST',
            body: formData
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Profile photo updated successfully!', 'success');
            // Update sidebar photo
            const sidebarPhoto = document.getElementById('sidebarPhoto');
            if (sidebarPhoto) {
                if (sidebarPhoto.tagName === 'IMG') {
                    sidebarPhoto.src = data.photoUrl;
                } else {
                    const img = document.createElement('img');
                    img.src = data.photoUrl;
                    img.className = 'sidebar-photo';
                    img.id = 'sidebarPhoto';
                    img.alt = 'Employee';
                    sidebarPhoto.parentNode.replaceChild(img, sidebarPhoto);
                }
            }
            selectedPhotoFile = null;
        } else {
            showToast(data.message || 'Failed to upload photo.', 'error');
        }
    } catch (err) {
        showToast('Server error during photo upload.', 'error');
    } finally {
        if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Upload Photo'; }
    }
}
