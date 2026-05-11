/* admin-messages.js */

let currentFilters = {
    type: 'all',
    role: 'all',
    time: 'all',
    search: ''
};

$(document).ready(function () {
    // Initial load logic is usually handled by Razor, 
    // but some pages might want to refresh immediately
    
    // Search Input
    $('#searchInput').on('input', debounce(function() {
        currentFilters.search = $(this).val();
        applyFilters();
    }, 500));

    // Role Filter
    $('#roleFilter').on('change', function() {
        currentFilters.role = $(this).val();
        applyFilters();
    });

    // Time Range Filter
    $('#timeRangeFilter').on('change', function() {
        currentFilters.time = $(this).val();
        applyFilters();
    });
    
    // Refresh Button
    $('#refreshBtn').on('click', function() {
        refreshData();
    });

    // KPI Card Click Filters
    $('#kpiTotal, #kpiTotalMessages, #kpiTotalBlocked').on('click', () => filterByType('all'));
    $('#kpiBlocked, #kpiBlockedViolations, #kpiPending').on('click', () => filterByType('blocked'));
    $('#kpiActive, #kpiActivePenalties, #kpiResolved').on('click', () => filterByType('active'));
});

function filterByType(type) {
    currentFilters.type = type;
    
    // Update active state on KPI cards
    $('.kpi-card').removeClass('active');
    const idMap = {
        'all': ['kpiTotal', 'kpiTotalMessages', 'kpiTotalBlocked'],
        'blocked': ['kpiBlocked', 'kpiBlockedViolations', 'kpiPending'],
        'active': ['kpiActive', 'kpiActivePenalties', 'kpiResolved']
    };
    
    idMap[type].forEach(id => $(`#${id}`).addClass('active'));
    
    applyFilters();
}

async function applyFilters() {
    const isBlockedPage = window.location.pathname.includes('BlockedMessages');
    const endpoint = isBlockedPage ? '/Admin/GetFilteredBlockedMessages' : '/Admin/GetFilteredMessages';
    
    // Show loading state in table
    const tbody = document.querySelector('tbody');
    if (tbody) {
        tbody.style.opacity = '0.5';
    }

    try {
        const queryParams = new URLSearchParams({
            type: currentFilters.type,
            role: currentFilters.role,
            time: currentFilters.time,
            search: currentFilters.search
        });

        const response = await fetch(`${endpoint}?${queryParams.toString()}`);
        const data = await response.json();

        if (isBlockedPage) {
            renderBlockedTable(data.items);
        } else {
            renderMessagesTable(data.items);
        }
        
        // Sync KPI counts if provided
        if (data.counts) {
            updateKPICounts(data.counts);
        }
        
        $('#filteredCount').text(`${data.items.length} results`);

    } catch (error) {
        console.error('Filter error:', error);
    } finally {
        if (tbody) {
            tbody.style.opacity = '1';
        }
    }
}

function renderMessagesTable(items) {
    const tbody = $('#messagesTableBody');
    if (!tbody.length) return;

    if (items.length === 0) {
        tbody.html('<tr><td colspan="7" class="text-center py-5 text-muted">No matching messages found.</td></tr>');
        return;
    }

    let html = '';
    items.forEach(item => {
        const date = new Date(item.sentAt);
        const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const dateStr = date.toLocaleDateString([], { month: 'short', day: '2-digit' });
        
        html += `
            <tr>
                <td>
                    <div class="fw-bold text-primary">${dateStr}</div>
                    <div class="small text-muted">${timeStr}</div>
                </td>
                <td>
                    <div class="fw-bold">${item.senderName}</div>
                    <div class="small text-muted">${item.senderEmail || ''}</div>
                </td>
                <td>
                    <span class="badge-soft ${item.senderRole === 'Supplier' ? 'badge-soft-success' : 'badge-soft-info'}">
                        ${item.senderRole}
                    </span>
                </td>
                <td>${item.conversationBetween}</td>
                <td>
                    <span class="message-preview-link" onclick="viewConversation(${item.conversationId})">
                        ${item.content.length > 50 ? item.content.substring(0, 50) + '...' : item.content}
                    </span>
                </td>
                <td>
                    ${item.containsFlaggedWords ? '<span class="badge-soft badge-soft-danger"><i class="fas fa-exclamation-triangle"></i> Flagged</span>' : ''}
                </td>
                <td class="text-end pe-4">
                    <a href="/Admin/ViewConversation/${item.conversationId}" class="btn btn-sm btn-refresh px-3">View</a>
                </td>
            </tr>
        `;
    });
    tbody.html(html);
}

function renderBlockedTable(items) {
    const tbody = $('#blockedMessagesTableBody');
    if (!tbody.length) return;

    if (items.length === 0) {
        tbody.html('<tr><td colspan="7" class="text-center py-5 text-muted">No violations found.</td></tr>');
        return;
    }

    let html = '';
    items.forEach(item => {
        const date = new Date(item.createdAt);
        const dateStr = date.toLocaleDateString([], { month: 'short', day: '2-digit', year: 'numeric' });
        const timeStr = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        
        html += `
            <tr>
                <td class="ps-4">
                    <div class="fw-bold">${dateStr}</div>
                    <div class="small text-muted">${timeStr}</div>
                </td>
                <td class="fw-bold">${item.senderName}</td>
                <td>
                    <span class="badge-soft ${item.senderRole === 'Supplier' ? 'badge-soft-success' : 'badge-soft-info'}">
                        ${item.senderRole}
                    </span>
                </td>
                <td>
                    <span class="violation-badge ${item.isSevere ? 'violation-severe' : 'violation-standard'}">
                        ${item.violationType}
                    </span>
                </td>
                <td>
                    <span class="message-preview-link" onclick="viewConversation(${item.conversationId})">
                        ${item.content.length > 60 ? item.content.substring(0, 60) + '...' : item.content}
                    </span>
                </td>
                <td>
                    <span class="status-pill ${item.isResolved ? 'status-resolved' : 'status-pending'}">
                        ${item.isResolved ? 'Resolved' : 'Pending'}
                    </span>
                </td>
                <td class="text-end pe-4">
                    <div class="d-flex gap-2 justify-content-end">
                        <a href="/Admin/ViewConversation/${item.conversationId}" class="btn-refresh btn-sm" title="View"><i class="fas fa-eye"></i></a>
                        ${!item.isResolved ? `<button class="btn-refresh btn-sm" onclick="resolveViolation(${item.violationId})" title="Resolve"><i class="fas fa-check"></i></button>` : ''}
                    </div>
                </td>
            </tr>
        `;
    });
    tbody.html(html);
}

function updateKPICounts(counts) {
    // Message Monitoring Counts
    if ($('#kpiTotalMessages').length) {
        $('#kpiTotalMessages .kpi-value').text(counts.total);
        $('#kpiBlockedViolations .kpi-value').text(counts.blocked);
        $('#kpiActivePenalties .kpi-value').text(counts.penalties);
    }
    
    // Blocked Messages Counts
    if ($('#kpiTotalBlocked').length) {
        $('#kpiTotalBlocked .kpi-value').text(counts.total);
        $('#kpiPending .kpi-value').text(counts.pending);
        $('#kpiResolved .kpi-value').text(counts.resolved);
    }
}

function refreshData() {
    const btn = $('#refreshBtn');
    const originalHtml = btn.html();
    btn.html('<i class="fas fa-spinner fa-spin"></i>').prop('disabled', true);
    
    // Reset filters
    currentFilters = { type: 'all', role: 'all', time: 'all', search: '' };
    $('#searchInput').val('');
    $('#roleFilter').val('all');
    $('#timeRangeFilter').val('all');
    $('.kpi-card').removeClass('active');
    $('.kpi-card:first-child').addClass('active');

    applyFilters().then(() => {
        setTimeout(() => {
            btn.html(originalHtml).prop('disabled', false);
        }, 500);
    });
}

function viewConversation(id) {
    window.location.href = `/Admin/ViewConversation/${id}`;
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
