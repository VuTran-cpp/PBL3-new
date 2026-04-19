/* ════════════════════════════════
   app.js — Application logic
   ABC Coffee Management System
   ════════════════════════════════ */

// ── State ──
let orderItems = {};
let currentTable = null;

// ══════════════════════════════
// AUTH
// ══════════════════════════════
function doLogin() {
  document.getElementById('loginPage').style.display = 'none';
  document.getElementById('appShell').style.display = 'flex';
}

function doLogout() {
  document.getElementById('appShell').style.display = 'none';
  document.getElementById('loginPage').style.display = 'flex';
}

// ══════════════════════════════
// NAVIGATION
// ══════════════════════════════
function go(pageId, el, title) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.nav-link').forEach(n => n.classList.remove('active'));
  document.getElementById(pageId).classList.add('active');
  el.classList.add('active');
  document.getElementById('pageTitle').textContent = title;
}

// ══════════════════════════════
// TABS
// ══════════════════════════════
document.querySelectorAll('.pill-tabs').forEach(group => {
  group.querySelectorAll('.pill-tab').forEach(tab => {
    tab.addEventListener('click', () => {
      group.querySelectorAll('.pill-tab').forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
    });
  });
});

document.querySelectorAll('.tabs').forEach(group => {
  group.querySelectorAll('.tab').forEach(tab => {
    tab.addEventListener('click', () => {
      group.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
    });
  });
});

// ══════════════════════════════
// TABLE SELECTION
// ══════════════════════════════
function selectTable(el, tableName) {
  if (el.classList.contains('busy') || el.classList.contains('cleaning')) return;

  document.querySelectorAll('.tc').forEach(t => t.classList.remove('selected'));
  el.classList.add('selected');
  currentTable = tableName;
  el.querySelector('.tc-status').textContent = 'Đang chọn';
  el.querySelector('.tc-status').className = 'tc-status sel';
  document.getElementById('orderTitle').textContent = 'Order — ' + tableName;
  orderItems = {};
  renderOrder();
}

// ══════════════════════════════
// MENU FILTER
// ══════════════════════════════
function filterMenu(val) {
  document.querySelectorAll('#menuList .menu-row').forEach(row => {
    const name = row.dataset.name.toLowerCase();
    row.style.display = name.includes(val.toLowerCase()) ? 'flex' : 'none';
  });
}

// ══════════════════════════════
// ORDER MANAGEMENT
// ══════════════════════════════
function addItem(name, price) {
  if (!currentTable) {
    alert('Vui lòng chọn bàn trước!');
    return;
  }
  if (orderItems[name]) {
    orderItems[name].qty++;
  } else {
    orderItems[name] = { price, qty: 1 };
  }
  renderOrder();
}

function removeItem(name) {
  delete orderItems[name];
  renderOrder();
}

function renderOrder() {
  const container = document.getElementById('orderItems');
  const names = Object.keys(orderItems);

  if (names.length === 0) {
    container.innerHTML = '<p class="oi-empty">Chưa có món nào</p>';
    document.getElementById('orderTotal').textContent = '0đ';
    return;
  }

  let total = 0;
  container.innerHTML = names.map(name => {
    const item = orderItems[name];
    total += item.price * item.qty;
    return `<div class="oi-row">
      <div class="oi-left">
        <span class="oi-qty">×${item.qty}</span>
        <span class="oi-name">${name}</span>
      </div>
      <div class="oi-right">
        <span class="oi-price">${(item.price * item.qty).toLocaleString('vi')}đ</span>
        <button class="oi-del" onclick="removeItem('${name}')">×</button>
      </div>
    </div>`;
  }).join('');

  document.getElementById('orderTotal').textContent = total.toLocaleString('vi') + 'đ';
}

function confirmOrder() {
  if (!currentTable) { alert('Vui lòng chọn bàn!'); return; }
  if (Object.keys(orderItems).length === 0) { alert('Chưa có món nào!'); return; }
  const total = Object.values(orderItems).reduce((s, i) => s + i.price * i.qty, 0);
  alert(`Order ${currentTable} đã gửi bếp!\nTổng: ${total.toLocaleString('vi')}đ`);
  clearOrder();
}

function clearOrder() {
  orderItems = {};
  currentTable = null;
  document.querySelectorAll('.tc.selected').forEach(t => {
    t.classList.remove('selected');
    t.querySelector('.tc-status').textContent = 'Trống';
    t.querySelector('.tc-status').className = 'tc-status';
  });
  document.getElementById('orderTitle').textContent = 'Chọn bàn để order';
  renderOrder();
}

// ══════════════════════════════
// PRODUCT TOGGLE
// ══════════════════════════════
function toggleProd(el) {
  const isOn = el.classList.contains('on');
  el.classList.toggle('on', !isOn);
  el.classList.toggle('off', isOn);
  const card = el.closest('.prod-card');
  card.classList.toggle('inactive', isOn);
  el.nextElementSibling.textContent = isOn ? 'Đang ẩn' : 'Đang bán';
}
