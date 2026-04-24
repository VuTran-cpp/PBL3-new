/**
 * api-client.js — Shared API helper cho toàn bộ CafeManagement MVC Views
 * Tự động đính kèm JWT từ window.JWT_TOKEN vào mọi request
 */

// ── Token helpers ──────────────────────────────────────────────────────
function getToken() {
    return window.JWT_TOKEN || sessionStorage.getItem('jwt_token') || '';
}

function authHeaders(extra = {}) {
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`,
        ...extra
    };
}

// ── Core fetch wrapper ──────────────────────────────────────────────────
async function apiFetch(url, options = {}) {
    try {
        const res = await fetch(url, {
            ...options,
            headers: authHeaders(options.headers || {})
        });

        let json;
        try { json = await res.json(); } catch { json = null; }

        if (res.status === 401) {
            showToast('⛔ Phiên đăng nhập hết hạn. Đang chuyển hướng...', 'error');
            setTimeout(() => window.location.href = '/Login', 1500);
            throw new Error('Unauthorized');
        }

        if (!json || !json.success) {
            throw new Error(json?.message || `HTTP ${res.status}`);
        }

        return json.data;
    } catch (err) {
        if (err.message !== 'Unauthorized') console.error(`[apiFetch] ${url}`, err);
        throw err;
    }
}

// ── Toast Notification ──────────────────────────────────────────────────
function showToast(msg, type = 'success', duration = 4000) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.style.cssText = 'position:fixed;top:20px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:8px;';
        document.body.appendChild(container);
    }

    const colors = { success: '#2ECC71', warn: '#F39C12', error: '#E74C3C', info: '#3498DB' };
    const icons  = { success: '✅', warn: '⚠️', error: '❌', info: 'ℹ️' };

    const toast = document.createElement('div');
    toast.style.cssText = `
        background:#2C1810; color:#fff; padding:14px 20px;
        border-radius:10px; box-shadow:0 5px 20px rgba(0,0,0,0.25);
        display:flex; align-items:center; gap:10px;
        border-left:4px solid ${colors[type] || colors.success};
        animation:toastIn .3s ease; max-width:360px; font-size:14px;
        font-family:'Be Vietnam Pro',sans-serif;
    `;
    toast.innerHTML = `<span>${icons[type] || '✅'}</span><span style="flex:1">${msg}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'toastOut .3s ease forwards';
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

// Inject keyframes if not present
if (!document.getElementById('toast-keyframes')) {
    const style = document.createElement('style');
    style.id = 'toast-keyframes';
    style.textContent = `
        @keyframes toastIn  { from{transform:translateX(110%);opacity:0} to{transform:translateX(0);opacity:1} }
        @keyframes toastOut { from{transform:translateX(0);opacity:1}    to{transform:translateX(110%);opacity:0} }
    `;
    document.head.appendChild(style);
}

// ── Number / Date Formatters ────────────────────────────────────────────
function formatVND(amount) {
    if (amount == null) return '0đ';
    return Number(amount).toLocaleString('vi-VN') + 'đ';
}

function formatDateTime(dt) {
    if (!dt) return '—';
    return new Date(dt).toLocaleString('vi-VN', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
}

function formatDate(dt) {
    if (!dt) return '—';
    return new Date(dt).toLocaleDateString('vi-VN');
}

// ── Confirm Dialog ──────────────────────────────────────────────────────
function confirmDialog(message) {
    return window.confirm(message);
}

// ── Role helper (đọc từ JWT payload) ───────────────────────────────────
function getUserRole() {
    try {
        const token = getToken();
        if (!token) return null;
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
            || payload['role']
            || null;
    } catch { return null; }
}

function isManagerOrAdmin() {
    const role = getUserRole();
    return role === 'MANAGER' || role === 'ADMIN';
}

// Export to global scope
window.apiFetch    = apiFetch;
window.showToast   = showToast;
window.formatVND   = formatVND;
window.formatDate  = formatDate;
window.formatDateTime = formatDateTime;
window.confirmDialog  = confirmDialog;
window.getUserRole    = getUserRole;
window.isManagerOrAdmin = isManagerOrAdmin;
