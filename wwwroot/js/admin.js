/**
 * Admin Portal Management Script (.NET Core MVC)
 */

// Tab switching
function switchAdminTab(tabId, btn) {
    document.querySelectorAll('.admin-tab-content').forEach(tab => tab.classList.remove('active'));
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

// Filter Employees Table
function filterEmployeesTable() {
    const search = (document.getElementById('employeeSearchInput')?.value || '').toLowerCase();
    const dept = document.getElementById('employeeDeptFilter')?.value || 'All';
    const rows = document.querySelectorAll('#employeesTableBody tr');

    rows.forEach(row => {
        const text = row.innerText.toLowerCase();
        const rowDept = row.getAttribute('data-dept') || '';
        const matchesSearch = text.includes(search);
        const matchesDept = (dept === 'All' || rowDept === dept);

        row.style.display = (matchesSearch && matchesDept) ? '' : 'none';
    });
}

// Add Employee Modal
function openAddEmployeeModal() {
    document.getElementById('addEmployeeForm')?.reset();
    openModal('addEmployeeModal');
}

async function handleCreateEmployee(e) {
    e.preventDefault();
    const payload = {
        employeeCode: document.getElementById('addEmpCode').value.trim(),
        fullName: document.getElementById('addFullName').value.trim(),
        email: document.getElementById('addEmail').value.trim(),
        phone: document.getElementById('addPhone').value.trim(),
        department: document.getElementById('addDepartment').value,
        designation: document.getElementById('addDesignation').value.trim(),
        salary: parseFloat(document.getElementById('addSalary').value) || 0,
        dob: document.getElementById('addDob').value || null,
        gender: document.getElementById('addGender').value,
        password: document.getElementById('addPassword').value || 'employee123',
        address: document.getElementById('addAddress').value.trim(),
        status: 'ACTIVE'
    };

    try {
        const res = await fetch('/api/admin/employees', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Employee created successfully!', 'success');
            closeModal('addEmployeeModal');
            setTimeout(() => window.location.reload(), 800);
        } else {
            showToast(data.message || 'Failed to create employee.', 'error');
        }
    } catch (err) {
        showToast('Server error while saving employee.', 'error');
    }
}

// Edit Employee Modal
async function openEditEmployeeModal(empId) {
    try {
        const res = await fetch(`/api/admin/employees/${empId}`);
        const data = await res.json();
        if (res.ok && data.success && data.employee) {
            const emp = data.employee;
            document.getElementById('editEmpId').value = emp.id;
            document.getElementById('editEmpCode').value = emp.employeeCode;
            document.getElementById('editFullName').value = emp.fullName;
            document.getElementById('editEmail').value = emp.email;
            document.getElementById('editPhone').value = emp.phone || '';
            document.getElementById('editDepartment').value = emp.department || 'Engineering';
            document.getElementById('editDesignation').value = emp.designation || '';
            document.getElementById('editSalary').value = emp.salary || 0;
            document.getElementById('editStatus').value = emp.status || 'ACTIVE';
            openModal('editEmployeeModal');
        } else {
            showToast(data.message || 'Could not load employee details.', 'error');
        }
    } catch (err) {
        showToast('Error loading employee.', 'error');
    }
}

async function handleUpdateEmployee(e) {
    e.preventDefault();
    const empId = document.getElementById('editEmpId').value;
    const payload = {
        id: parseInt(empId),
        employeeCode: document.getElementById('editEmpCode').value,
        fullName: document.getElementById('editFullName').value.trim(),
        email: document.getElementById('editEmail').value.trim(),
        phone: document.getElementById('editPhone').value.trim(),
        department: document.getElementById('editDepartment').value,
        designation: document.getElementById('editDesignation').value.trim(),
        salary: parseFloat(document.getElementById('editSalary').value) || 0,
        status: document.getElementById('editStatus').value
    };

    try {
        const res = await fetch(`/api/admin/employees/${empId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Employee updated successfully!', 'success');
            closeModal('editEmployeeModal');
            setTimeout(() => window.location.reload(), 800);
        } else {
            showToast(data.message || 'Failed to update employee.', 'error');
        }
    } catch (err) {
        showToast('Server error while updating employee.', 'error');
    }
}

// Delete Employee
async function deleteEmployeePrompt(empId, name) {
    if (!confirm(`Are you sure you want to permanently delete employee "${name}"?`)) return;

    try {
        const res = await fetch(`/api/admin/employees/${empId}`, { method: 'DELETE' });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Employee deleted successfully.', 'success');
            const row = document.querySelector(`tr[data-emp-id="${empId}"]`);
            if (row) row.remove();
        } else {
            showToast(data.message || 'Failed to delete employee.', 'error');
        }
    } catch (err) {
        showToast('Server error while deleting employee.', 'error');
    }
}

// Attendance Filtering
async function loadAttendanceByFilter() {
    const date = document.getElementById('attendanceDateFilter')?.value || '';
    const dept = document.getElementById('attendanceDeptFilter')?.value || 'All';

    try {
        const res = await fetch(`/api/admin/attendance?date=${encodeURIComponent(date)}&department=${encodeURIComponent(dept)}`);
        const data = await res.json();
        if (res.ok && data.success && data.attendance) {
            renderAttendanceRows(data.attendance);
        }
    } catch (err) {
        showToast('Error loading attendance logs.', 'error');
    }
}

function renderAttendanceRows(list) {
    const tbody = document.getElementById('attendanceTableBody');
    if (!tbody) return;

    if (list.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">No attendance records found for this date.</td></tr>';
        return;
    }

    tbody.innerHTML = list.map(att => `
        <tr>
            <td>
                <strong>${att.fullName || ''}</strong>
                <small class="d-block text-muted">${att.employeeCode || ''}</small>
            </td>
            <td>${att.department || '-'}</td>
            <td>${formatDate(att.workDate)}</td>
            <td><span class="time-badge">${att.formattedPunchIn || '-'}</span></td>
            <td><span class="time-badge">${att.formattedPunchOut || '-'}</span></td>
            <td>${att.formattedWorkHours || '-'}</td>
            <td>
                <span class="method-badge ${att.punchMethod === 'FACE' ? 'method-face' : 'method-manual'}">
                    <i class="fa-solid ${att.punchMethod === 'FACE' ? 'fa-camera' : 'fa-hand'}"></i> ${att.punchMethod}
                </span>
            </td>
            <td><span class="status-pill pill-emerald">${att.status}</span></td>
        </tr>
    `).join('');
}

// Manual Attendance
function openManualAttendanceModal() {
    openModal('manualAttendanceModal');
}

async function handleSaveManualAttendance(e) {
    e.preventDefault();
    const payload = {
        employeeId: parseInt(document.getElementById('manualEmpId').value),
        date: document.getElementById('manualDate').value,
        punchIn: document.getElementById('manualPunchIn').value + ':00',
        punchOut: document.getElementById('manualPunchOut').value ? document.getElementById('manualPunchOut').value + ':00' : null,
        status: document.getElementById('manualStatus').value
    };

    try {
        const res = await fetch('/api/admin/attendance/manual', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Manual attendance recorded!', 'success');
            closeModal('manualAttendanceModal');
            loadAttendanceByFilter();
        } else {
            showToast(data.message || 'Failed to save attendance.', 'error');
        }
    } catch (err) {
        showToast('Server error while saving attendance.', 'error');
    }
}

// Leave Approvals
function filterLeaves(status, btn) {
    document.querySelectorAll('.filter-btn-group button').forEach(b => b.classList.remove('active'));
    if (btn) btn.classList.add('active');

    const rows = document.querySelectorAll('#leavesTableBody tr');
    rows.forEach(row => {
        const rowStatus = row.getAttribute('data-status');
        row.style.display = (status === 'All' || rowStatus === status) ? '' : 'none';
    });
}

async function approveLeavePrompt(id) {
    const note = prompt('Optional approval remarks/note:', 'Approved by admin.');
    if (note === null) return;

    try {
        const res = await fetch(`/api/admin/leaves/${id}/approve`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ note })
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Leave request approved!', 'success');
            setTimeout(() => window.location.reload(), 600);
        } else {
            showToast(data.message || 'Failed to approve leave.', 'error');
        }
    } catch (err) {
        showToast('Server error while approving leave.', 'error');
    }
}

async function rejectLeavePrompt(id) {
    const note = prompt('Please enter rejection reason:', 'Scheduling conflict / insufficient quota.');
    if (note === null) return;

    try {
        const res = await fetch(`/api/admin/leaves/${id}/reject`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ note })
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Leave request rejected.', 'info');
            setTimeout(() => window.location.reload(), 600);
        } else {
            showToast(data.message || 'Failed to reject leave.', 'error');
        }
    } catch (err) {
        showToast('Server error while rejecting leave.', 'error');
    }
}

// Announcements
function openNewAnnouncementModal() {
    openModal('newAnnouncementModal');
}

async function handleCreateAnnouncement(e) {
    e.preventDefault();
    const payload = {
        title: document.getElementById('annTitle').value.trim(),
        category: document.getElementById('annCategory').value,
        message: document.getElementById('annMessage').value.trim()
    };

    try {
        const res = await fetch('/api/admin/announcements', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Announcement broadcast successfully!', 'success');
            closeModal('newAnnouncementModal');
            setTimeout(() => window.location.reload(), 700);
        } else {
            showToast(data.message || 'Failed to publish announcement.', 'error');
        }
    } catch (err) {
        showToast('Server error publishing announcement.', 'error');
    }
}

async function deleteAnnouncementPrompt(id) {
    if (!confirm('Are you sure you want to delete this announcement?')) return;
    try {
        const res = await fetch(`/api/admin/announcements/${id}`, { method: 'DELETE' });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Announcement deleted.', 'success');
            document.querySelector(`.announcement-card[data-id="${id}"]`)?.remove();
        } else {
            showToast(data.message || 'Failed to delete announcement.', 'error');
        }
    } catch (err) {
        showToast('Server error deleting announcement.', 'error');
    }
}

// Awards
function openIssueAwardModal() {
    openModal('issueAwardModal');
}

async function handleCreateAward(e) {
    e.preventDefault();
    const payload = {
        employeeId: parseInt(document.getElementById('awardEmpId').value),
        awardType: document.getElementById('awardType').value,
        awardTitle: document.getElementById('awardTitle').value.trim(),
        awardPeriod: document.getElementById('awardPeriod').value.trim(),
        awardDate: document.getElementById('awardDate').value || new Date().toISOString().split('T')[0],
        reason: document.getElementById('awardReason').value.trim()
    };

    try {
        const res = await fetch('/api/admin/awards', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Award issued successfully!', 'success');
            closeModal('issueAwardModal');
            setTimeout(() => window.location.reload(), 700);
        } else {
            showToast(data.message || 'Failed to issue award.', 'error');
        }
    } catch (err) {
        showToast('Server error while issuing award.', 'error');
    }
}

async function deleteAwardPrompt(id) {
    if (!confirm('Are you sure you want to delete this award record?')) return;
    try {
        const res = await fetch(`/api/admin/awards/${id}`, { method: 'DELETE' });
        const data = await res.json();
        if (res.ok && data.success) {
            showToast('Award deleted.', 'success');
            document.querySelector(`.award-card[data-id="${id}"]`)?.remove();
        }
    } catch (err) {
        showToast('Server error deleting award.', 'error');
    }
}

function formatDate(isoStr) {
    if (!isoStr) return '-';
    const d = new Date(isoStr);
    return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}
