// notification.js - Handled Notification polling and interactions
let notificationInterval;

document.addEventListener('DOMContentLoaded', function() {
    initNotifications();
});

function initNotifications() {
    loadNotifications();
    // Poll every 30 seconds
    notificationInterval = setInterval(loadNotifications, 30000);
}

async function loadNotifications() {
    try {
        const response = await fetch('/Notification/GetRecentJson');
        const data = await response.json();
        
        updateBellBadge(data.unreadCount);
        updateNotificationDropdown(data.notifications);
    } catch (error) {
        console.error('Failed to load notifications:', error);
    }
}

function updateBellBadge(count) {
    const badge = document.getElementById('notificationBadge');
    if (!badge) return;

    if (count > 0) {
        badge.textContent = count > 99 ? '99+' : count;
        badge.style.display = 'flex';
        // Add a subtle bounce animation if count increased
        if (parseInt(badge.getAttribute('data-count') || '0') < count) {
            badge.classList.add('animate-bounce');
            setTimeout(() => badge.classList.remove('animate-bounce'), 2000);
        }
    } else {
        badge.style.display = 'none';
        badge.textContent = '0';
    }
    badge.setAttribute('data-count', count);
}

function updateNotificationDropdown(notifications) {
    const list = document.getElementById('notificationList');
    if (!list) return;

    if (!notifications || notifications.length === 0) {
        list.innerHTML = '<div class="p-4 text-center text-muted"><p class="mb-0">No new notifications</p></div>';
        return;
    }

    let html = '';
    notifications.forEach(note => {
        html += `
            <div class="notification-item ${note.isRead ? '' : 'unread'} p-3 border-bottom position-relative" onclick="handleNotificationClick(event, ${note.id}, '${note.actionUrl}')">
                <div class="d-flex align-items-start">
                    <div class="notification-icon me-3">
                        <span class="fs-4">${note.icon}</span>
                    </div>
                    <div class="flex-grow-1">
                        <div class="d-flex justify-content-between">
                            <h6 class="notification-title mb-1 small">${escapeHtml(note.title)}</h6>
                            <span class="notification-time text-muted small">${note.timeAgo}</span>
                        </div>
                        <p class="notification-message mb-0 text-muted small">${escapeHtml(note.message)}</p>
                    </div>
                </div>
            </div>
        `;
    });
    list.innerHTML = html;
}

async function handleNotificationClick(event, id, url) {
    event.preventDefault();
    event.stopPropagation();

    try {
        await fetch(`/Notification/MarkAsRead?id=${id}`, { method: 'POST' });
        // Redirect after marking as read
        window.location.href = url || '/Notification/Index';
    } catch (error) {
        console.error('Error marking as read:', error);
        window.location.href = url || '/Notification/Index';
    }
}

async function markAllNotificationsAsRead(event) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    try {
        const response = await fetch('/Notification/MarkAllAsRead', { method: 'POST' });
        if (response.ok) {
            loadNotifications();
        }
    } catch (error) {
        console.error('Error marking all as read:', error);
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
