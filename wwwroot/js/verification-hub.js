/* Unified Verification Hub Interaction Logic */

document.addEventListener('DOMContentLoaded', function() {
    // Sync UI with existing filters
    const params = new URLSearchParams(window.location.search);
    const role = params.get('roleFilter');
    const status = params.get('statusFilter');
    const search = params.get('searchTerm');

    if (role && document.getElementById('roleFilter')) document.getElementById('roleFilter').value = role;
    if (status) {
        if (document.getElementById('statusFilter')) document.getElementById('statusFilter').value = status;
        highlightKPICard(status);
    }
    if (search && document.getElementById('hubSearch')) document.getElementById('hubSearch').value = search;

    // Reset modals
    window.onclick = function(event) {
        if (event.target.classList.contains('hub-modal')) {
            event.target.classList.remove('active');
        }
    };

    document.querySelectorAll('.btn-close-modal').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.hub-modal').forEach(m => m.classList.remove('active'));
        });
    });
});

function highlightKPICard(status) {
    document.querySelectorAll('.kpi-card').forEach(card => card.classList.remove('active'));
    if (status === 'Pending') document.getElementById('kpiPending')?.classList.add('active');
    else if (status === 'Verified') document.getElementById('kpiVerified')?.classList.add('active');
    else if (status === 'Waitlist') document.getElementById('kpiWaitlist')?.classList.add('active');
}

/**
 * Filter by clicking on KPI cards
 */
function filterByQuickStatus(status) {
    const url = new URL(window.location.href);
    url.searchParams.set('statusFilter', status);
    // When using quick filters, we might want to clear role or search if they conflict, 
    // but for now let's keep them and just update status.
    window.location.href = url.href;
}

async function handleRejection(event) {
    event.preventDefault();
    const form = event.target;
    const formData = new FormData(form);
    
    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData
        });
        
        if (response.ok) {
            location.reload();
        } else {
            alert('Failed to reject application. Please try again.');
        }
    } catch (error) {
        console.error('Error rejecting user:', error);
    }
}

async function handleApproval(event) {
    event.preventDefault();
    const form = event.target;
    const formData = new FormData(form);
    
    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData
        });
        
        if (response.ok) {
            location.reload();
        } else {
            alert('Failed to approve application. Please try again.');
        }
    } catch (error) {
        console.error('Error approving user:', error);
    }
}

function openRejectModal(userId, userName) {
    const modal = document.getElementById('rejectModal');
    const form = document.getElementById('rejectForm');
    const title = document.getElementById('rejectModalTitle');
    if (title) title.innerText = `Reject Application: ${userName}`;
    if (form) {
        form.action = `/Admin/RejectUser/${userId}`;
        form.onsubmit = handleRejection;
    }
    modal?.classList.add('active');
}

function openApproveModal(userId, userName) {
    const modal = document.getElementById('approveModal');
    const form = document.getElementById('approveForm');
    const title = document.getElementById('approveModalTitle');
    if (title) title.innerText = `Approve Application: ${userName}`;
    if (form) {
        form.action = `/Admin/ApproveUser/${userId}`;
        form.onsubmit = handleApproval;
    }
    modal?.classList.add('active');
}

/**
 * Document Preview Modal
 */
/**
 * Document Preview Modal - Unified Loader
 */
async function previewDocument(docName, userId, userType = 'Supplier') {
    const modal = document.getElementById('documentModal');
    const docTitle = document.getElementById('docTitle');
    const modalBody = document.querySelector('#documentModal .modal-body');
    const downloadBtn = document.querySelector('#documentModal .btn-primary, #documentModal [style*="background:#5B8FF9"]');
    
    if (docTitle) docTitle.textContent = docName;
    
    // Show loading state
    if (modalBody) {
        modalBody.innerHTML = `
            <div class="w-100 rounded-3 d-flex align-items-center justify-content-center" style="height: 350px; background: var(--hover-state); border: 1px dashed var(--border-color);">
                <div class="text-center">
                    <div class="spinner-border text-primary" role="status"></div>
                    <p class="small text-muted mt-2">Loading document security layer...</p>
                </div>
            </div>
        `;
    }

    // Using Bootstrap 5 Modal
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();

    try {
        const response = await fetch(`/Admin/GetDocument?userId=${userId}&userType=${userType}&docName=${encodeURIComponent(docName)}`);
        
        if (response.ok) {
            const blob = await response.blob();
            const fileUrl = URL.createObjectURL(blob);
            const ext = docName.split('.').pop().toLowerCase();
            
            if (modalBody) {
                if (ext === 'pdf') {
                    modalBody.innerHTML = `
                        <div class="document-viewer-container" style="height: 500px; background: #525659; border-radius: 8px; overflow: hidden;">
                            <iframe src="${fileUrl}#toolbar=0" width="100%" height="100%" style="border: none;"></iframe>
                        </div>
                    `;
                } else if (['jpg', 'jpeg', 'png', 'gif'].includes(ext)) {
                    modalBody.innerHTML = `
                        <div class="document-viewer-container d-flex align-items-center justify-content-center" style="height: 500px; background: #111; border-radius: 8px; overflow: hidden;">
                            <img src="${fileUrl}" style="max-width: 100%; max-height: 100%; object-fit: contain;">
                        </div>
                    `;
                } else {
                    const icon = getFileIcon(docName);
                    modalBody.innerHTML = `
                        <div class="w-100 rounded-3 d-flex align-items-center justify-content-center" style="height: 350px; background: var(--hover-state); border: 1px dashed var(--border-color); flex-direction: column;">
                            <div class="text-center">
                                ${icon}
                                <h6 class="mt-3" style="color: var(--text-color); font-weight: 700;">${docName}</h6>
                                <p class="small text-muted px-4">This file type cannot be previewed. Please download it for inspection.</p>
                            </div>
                        </div>
                    `;
                }
            }

            if (downloadBtn) {
                downloadBtn.onclick = () => {
                    const a = document.createElement('a');
                    a.href = fileUrl;
                    a.download = docName;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                };
            }
        } else {
            if (modalBody) modalBody.innerHTML = `<div class="p-4 text-center text-danger"><i class="fas fa-exclamation-triangle fa-2x mb-2"></i><br>Document not found</div>`;
        }
    } catch (error) {
        console.error('Error fetching document:', error);
        if (modalBody) modalBody.innerHTML = `<div class="p-4 text-center text-danger"><i class="fas fa-exclamation-triangle fa-2x mb-2"></i><br>Error loading document</div>`;
    }
}

function getFileIcon(filename) {
    const ext = filename.split('.').pop().toLowerCase();
    if (ext === 'pdf') {
        return '<i class="fas fa-file-pdf fa-4x mb-3 text-danger opacity-75"></i>';
    } else if (['jpg', 'jpeg', 'png', 'gif'].includes(ext)) {
        return '<i class="fas fa-file-image fa-4x mb-3 text-primary opacity-75"></i>';
    }
    return '<i class="fas fa-file fa-4x mb-3 text-muted opacity-50"></i>';
}

function applyFilters() {
    const role = document.getElementById('roleFilter')?.value;
    const status = document.getElementById('statusFilter')?.value;
    const search = document.getElementById('hubSearch')?.value;
    
    const url = new URL(window.location.href);
    if (role) url.searchParams.set('roleFilter', role); else url.searchParams.delete('roleFilter');
    if (status) url.searchParams.set('statusFilter', status); else url.searchParams.delete('statusFilter');
    if (search) url.searchParams.set('searchTerm', search); else url.searchParams.delete('searchTerm');
    
    window.location.href = url.href;
}

let searchTimeout;
function handleSearchInput() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => applyFilters(), 500);
}
