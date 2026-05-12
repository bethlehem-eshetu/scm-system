/* admin-users.js */

let selectedUsers = [];
let currentPage = 1;
let pageSize = 10;
let totalUsers = 0;

$(document).ready(function () {
    // Initial load
    loadUsers();

    // Event Listeners
    $('#searchInput').on('input', debounce(function() {
        currentPage = 1;
        loadUsers();
    }, 500));

    $('#roleFilter, #statusFilter').on('change', function() {
        currentPage = 1;
        loadUsers();
    });

    $('#pageSizeSelect').on('change', function() {
        pageSize = $(this).val();
        currentPage = 1;
        loadUsers();
    });

    $('#refreshBtn').on('click', function() {
        currentPage = 1;
        loadUsers();
    });

    // KPI Card Click Filters
    $('#kpiTotalUsers').on('click', () => filterByStatus('All'));
    $('#kpiActiveUsers').on('click', () => filterByStatus('Active'));
    $('#kpiPendingVerification').on('click', () => filterByStatus('Pending'));

    // Select All Checkbox
    $('#selectAll').on('change', function() {
        const isChecked = $(this).is(':checked');
        $('.user-checkbox').each(function() {
            $(this).prop('checked', isChecked);
            toggleUserSelection($(this).val(), isChecked);
        });
        updateBulkBar();
    });

    // Row selection and individual checkbox
    $(document).on('change', '.user-checkbox', function(e) {
        toggleUserSelection($(this).val(), $(this).is(':checked'));
        updateBulkBar();
    });

    $(document).on('click', '.user-row', function(e) {
        if ($(e.target).closest('.action-btn, .form-check-input, .dropdown-menu').length) return;
        const checkbox = $(this).find('.user-checkbox');
        const isChecked = !checkbox.is(':checked');
        checkbox.prop('checked', isChecked);
        toggleUserSelection(checkbox.val(), isChecked);
        updateBulkBar();
    });

    // Single Actions
    $(document).on('click', '.btn-verify', function() {
        const userId = $(this).data('id');
        openVerifyModal(userId);
    });

    $(document).on('click', '.btn-suspend', function() {
        const userId = $(this).data('id');
        openSuspendModal(userId);
    });


    // --- Bulk Action Handlers ---
    $('#bulkVerifyBtn').on('click', function() {
        showBulkModal('Verify', 'success', 'fa-check-circle', true);
    });

    $('#bulkSuspendBtn').on('click', function() {
        showBulkModal('Suspend', 'warning', 'fa-user-slash', true, true);
    });

    $('#bulkRejectBtn').on('click', function() {
        showBulkModal('Reject', 'danger', 'fa-times-circle', true, true);
    });

    // Modal Confirmation Buttons
    $('#confirmVerifyBtn').on('click', async function() {
        const userId = $(this).data('id');
        await executeAction('/Admin/VerifyUser', { userId });
    });

    $('#confirmSuspendBtn').on('click', async function() {
        const userId = $(this).data('id');
        const reason = $('#suspendReason').val() === 'Other' ? $('#suspendComments').val() : $('#suspendReason').val();
        await executeAction('/Admin/SuspendUser', { userId, reason });
    });


    $('#confirmBulkActionBtn').on('click', async function() {
        const action = $(this).data('action');
        const endpoint = `/Admin/Bulk${action}Users`;
        const reason = $('#bulkActionReason').val();
        await executeAction(endpoint, { userIds: selectedUsers, reason });
    });

    $('#exportBtn').on('click', function() {
        window.location.href = '/Admin/ExportUsers?format=csv';
    });
});

// Load Users via AJAX
async function loadUsers() {
    const searchTerm = $('#searchInput').val();
    const role = $('#roleFilter').val();
    const status = $('#statusFilter').val();

    // Show loading state
    $('#userTableBody').html(`
        <tr>
            <td colspan="7" class="text-center py-5">
                <div class="spinner-border text-primary" role="status"></div>
                <div class="mt-2 text-muted">Fetching user data...</div>
            </td>
        </tr>
    `);

    try {
        const response = await fetch(`/Admin/GetUsersPaginated?page=${currentPage}&pageSize=${pageSize}&searchTerm=${searchTerm}&role=${role}&status=${status}`);
        const data = await response.json();
        
        totalUsers = data.totalCount;
        $('#resultsCount').text(`${totalUsers} results`);
        renderUsers(data.users);
        renderPagination();
        updateKPIs(data);
    } catch (error) {
        $('#userTableBody').html(`<tr><td colspan="7" class="text-center py-5 text-danger">Error loading users. Please refresh.</td></tr>`);
    }
}

// Quick Filter Handlers (Chips)
$(document).on('click', '.filter-chip', function() {
    $('.filter-chip').removeClass('active');
    $(this).addClass('active');
    const role = $(this).data('role');
    $('#roleFilter').val(role);
    currentPage = 1;
    loadUsers();
});

function filterByStatus(status) {
    // Update active state on KPI cards
    $('.kpi-card').removeClass('active');
    $(`#kpi${status === 'All' ? 'TotalUsers' : (status === 'Active' ? 'ActiveUsers' : 'PendingVerification')}`).addClass('active');
    
    // Update the dropdown filter
    $('#statusFilter').val(status);
    
    // Refresh table
    currentPage = 1;
    loadUsers();
    
    // Update URL without reload
    const url = new URL(window.location.href);
    url.searchParams.set('status', status.toLowerCase());
    window.history.pushState({}, '', url);
}

function renderUsers(users) {
    if (users.length === 0) {
        $('#userTableBody').html(`
            <tr>
                <td colspan="7" class="text-center py-5">
                    <div class="empty-state">
                        <i class="fas fa-users-slash mb-3"></i>
                        <h5>No users found</h5>
                        <p>Try adjusting your search or filters.</p>
                    </div>
                </td>
            </tr>
        `);
        return;
    }

    let html = '';
    users.forEach(user => {
        const isSelected = selectedUsers.includes(user.id.toString());
        const initials = user.fullName.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
        
        // Role Data
        let roleBadge = '';
        switch(user.role) {
            case 'Admin': roleBadge = `<span class="badge-role badge-admin"><i class="fas fa-crown"></i> Admin</span>`; break;
            case 'Supplier': roleBadge = `<span class="badge-role badge-supplier"><i class="fas fa-industry"></i> Supplier</span>`; break;
            case 'Retailer': roleBadge = `<span class="badge-role badge-retailer"><i class="fas fa-store"></i> Retailer</span>`; break;
            case 'DeliveryAgent': roleBadge = `<span class="badge-role badge-delivery"><i class="fas fa-truck"></i> Delivery</span>`; break;
            case 'WarehouseManager': roleBadge = `<span class="badge-role badge-warehouse"><i class="fas fa-warehouse"></i> Warehouse</span>`; break;
            default: roleBadge = `<span class="badge-role text-muted">${user.role}</span>`;
        }

        // Status pill
        const statusClass = `status-${user.accountStatus.toLowerCase()}`;
        
        html += `
            <tr class="user-row ${isSelected ? 'selected' : ''}" data-id="${user.id}">
                <td style="width: 50px;">
                    <div class="form-check">
                        <input class="form-check-input user-checkbox" type="checkbox" value="${user.id}" ${isSelected ? 'checked' : ''}>
                    </div>
                </td>
                <td>
                    <div class="d-flex align-items-center gap-3">
                        <div class="avatar-initials">${initials}</div>
                        <div>
                            <div class="fw-bold text-color">${user.fullName}</div>
                            <div class="small text-muted">ID: #${user.id}</div>
                        </div>
                    </div>
                </td>
                <td>
                    <div class="small fw-medium">${user.email}</div>
                    <div class="small text-muted">${user.phoneNumber || 'No phone'}</div>
                </td>
                <td>${roleBadge}</td>
                <td><span class="status-pill ${statusClass}">${user.accountStatus}</span></td>
                <td><div class="small text-muted">${user.lastActive}</div></td>
                <td>
                    <div class="action-buttons d-flex gap-1 justify-content-end">
                        ${user.accountStatus === 'Pending' ? `
                            <button class="btn btn-icon-sm btn-success-soft btn-verify" data-id="${user.id}" title="Verify User"><i class="fas fa-check"></i></button>
                        ` : ''}
                        <button class="btn btn-icon-sm btn-outline-card btn-suspend" data-id="${user.id}" title="Suspend User"><i class="fas fa-user-slash"></i></button>
                        <div class="dropdown">
                            <button class="btn btn-icon-sm btn-outline-card" data-bs-toggle="dropdown"><i class="fas fa-ellipsis-v"></i></button>
                            <ul class="dropdown-menu shadow-sm dropdown-menu-end">
                                <li><a class="dropdown-item" href="/Admin/UserDetails/${user.id}"><i class="fas fa-eye me-2"></i> View Details</a></li>
                                <li><a class="dropdown-item" href="#"><i class="fas fa-envelope me-2"></i> Message</a></li>
                                <li><hr class="dropdown-divider"></li>
                                <li><a class="dropdown-item text-danger" href="javascript:void(0)" onclick="openRejectModal(${user.id}, '${user.fullName}', '${user.role}')"><i class="fas fa-times-circle me-2"></i> Reject</a></li>
                            </ul>
                        </div>
                    </div>
                </td>
            </tr>
        `;
    });
    $('#userTableBody').html(html);
}

function renderPagination() {
    const totalPages = Math.ceil(totalUsers / pageSize);
    if (totalPages <= 1) {
        $('#paginationArea').hide();
        return;
    }

    $('#paginationArea').show();
    let html = `
        <button class="page-btn ${currentPage === 1 ? 'disabled' : ''}" onclick="changePage(${currentPage - 1})"><i class="fas fa-chevron-left"></i></button>
    `;

    for (let i = 1; i <= totalPages; i++) {
        if (i === 1 || i === totalPages || (i >= currentPage - 1 && i <= currentPage + 1)) {
            html += `<button class="page-btn ${i === currentPage ? 'active' : ''}" onclick="changePage(${i})">${i}</button>`;
        } else if (i === currentPage - 2 || i === currentPage + 2) {
            html += `<span class="px-2">...</span>`;
        }
    }

    html += `
        <button class="page-btn ${currentPage === totalPages ? 'disabled' : ''}" onclick="changePage(${currentPage + 1})"><i class="fas fa-chevron-right"></i></button>
    `;

    $('#paginationControls').html(html);
    $('#startRange').text(((currentPage - 1) * pageSize) + 1);
    $('#endRange').text(Math.min(currentPage * pageSize, totalUsers));
    $('#totalCount').text(totalUsers);
}

function changePage(page) {
    if (page < 1 || page > Math.ceil(totalUsers / pageSize)) return;
    currentPage = page;
    loadUsers();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

// Actions Execution
async function executeAction(endpoint, data) {
    const btn = event.target.tagName === 'BUTTON' ? $(event.target) : $(event.target).closest('button');
    const originalText = btn.html();
    btn.html('<i class="fas fa-spinner fa-spin"></i>').prop('disabled', true);

    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        const result = await response.json();

        if (result.success) {
            showToast(result.message || 'Action executed successfully', 'success');
            $('.modal').modal('hide');
            loadUsers();
            if (data.userIds) { // Clear bulk selection if bulk action
                selectedUsers = [];
                $('#selectAll').prop('checked', false);
                updateBulkBar();
            }
        } else {
            showToast(result.message || 'Action failed', 'error');
            btn.html(originalText).prop('disabled', false);
        }
    } catch (error) {
        showToast('Network error', 'error');
        btn.html(originalText).prop('disabled', false);
    }
}

// UI Helpers
function toggleUserSelection(id, isSelected) {
    if (isSelected) {
        if (!selectedUsers.includes(id)) selectedUsers.push(id);
        $(`tr[data-id="${id}"]`).addClass('selected');
    } else {
        selectedUsers = selectedUsers.filter(uid => uid !== id);
        $(`tr[data-id="${id}"]`).removeClass('selected');
    }
}

function updateBulkBar() {
    const bar = $('#bulkActionBar');
    if (selectedUsers.length > 0) {
        $('#bulkUserCount').text(selectedUsers.length);
        bar.addClass('active');
    } else {
        bar.removeClass('active');
    }
}

function updateKPIs(data) {
    // Update KPI card values with global counts from server
    $('#kpiTotalUsers .kpi-value').text(data.totalGlobal);
    $('#kpiActiveUsers .kpi-value').text(data.activeGlobal);
    $('#kpiPendingVerification .kpi-value').text(data.pendingGlobal);
}

function showBulkModal(action, color, icon, showCount, showReason = false) {
    $('#bulkActionTitle').text(`${action} Selected Users`);
    $('#bulkActionIcon').attr('class', `d-inline-flex align-items-center justify-content-center bg-${color} bg-opacity-10 rounded-circle`).html(`<i class="fas ${icon} fa-2x text-${color}"></i>`);
    $('#confirmBulkActionBtn').attr('class', `btn btn-${color} flex-grow-1 py-2 rounded-3 fw-bold text-white`).data('action', action);
    $('#bulkUserCount').text(selectedUsers.length);
    
    if (showReason) $('#bulkReasonGroup').removeClass('d-none');
    else $('#bulkReasonGroup').addClass('d-none');

    $('#bulkActionModal').modal('show');
}

// Modal Openers
function openVerifyModal(id) {
    $('#confirmVerifyBtn').data('id', id);
    $('#verifyUserModal').modal('show');
}

function openSuspendModal(id) {
    $('#confirmSuspendBtn').data('id', id);
    $('#suspendUserModal').modal('show');
}


// Debounce helper
function debounce(func, wait) {
    let timeout;
    return function() {
        const context = this, args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), wait);
    };
}
