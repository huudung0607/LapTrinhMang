const $ = id => document.getElementById(id);
const COLORS = ['#0068ff', '#e8590c', '#2f9e44', '#9c36b5', '#d6336c', '#1098ad', '#f08c00'];
let me, peer, contacts = [], conn, registering = false;

const api = async (url, opts = {}) => {
  const r = await fetch(url, { credentials: 'same-origin', ...opts });
  const body = await r.text();
  if (!r.ok) throw new Error(body.replace(/^"|"$/g, '') || 'Có lỗi xảy ra.');
  try { return JSON.parse(body); } catch { return body; }
};
const post = (url, data) => api(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });

// Dùng textContent thay vì innerHTML để nội dung người dùng không chạy được script.
const el = (tag, cls, text) => { const e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; };
const initial = n => (n.trim()[0] || '?').toUpperCase();
// id là ObjectId (chuỗi) nên băm chuỗi để chọn màu avatar.
const color = id => COLORS[[...String(id)].reduce((h, ch) => (h * 31 + ch.charCodeAt(0)) >>> 0, 0) % COLORS.length];
const hhmm = d => new Date(d + (/Z|\+/.test(d) ? '' : 'Z')).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
const preview = m => !m ? '' : (m.mine ? 'Bạn: ' : '') + (m.kind === 'image' ? '[Hình ảnh]' : m.kind === 'file' ? '[File] ' + m.fileName : m.text);

function setAvatar(node, name, id, online) {
  node.textContent = initial(name);
  node.style.background = color(id);
  node.classList.toggle('on', !!online);
}

/* ---------- đăng nhập ---------- */
function setMode(reg) {
  registering = reg;
  $('fName').hidden = !reg; $('fName').required = reg;
  $('authBtn').textContent = reg ? 'Đăng ký' : 'Đăng nhập';
  $('authSub').textContent = reg ? 'Tạo tài khoản mới (cần admin duyệt)' : 'Đăng nhập tài khoản để nhắn tin';
  $('authSwitch').textContent = reg ? 'Đã có tài khoản? Đăng nhập' : 'Chưa có tài khoản? Đăng ký';
  $('authMsg').textContent = '';
}
$('authSwitch').onclick = () => setMode(!registering);
$('authForm').onsubmit = async e => {
  e.preventDefault();
  const msg = $('authMsg'); msg.className = 'auth-msg'; msg.textContent = '';
  try {
    if (registering) {
      const text = await post('/api/register', { username: $('fUser').value, displayName: $('fName').value, password: $('fPass').value });
      setMode(false); msg.className = 'auth-msg ok'; msg.textContent = text;
    } else {
      const u = await post('/api/login', { username: $('fUser').value, password: $('fPass').value });
      if (u.role === 'admin') location.href = '/admin.html'; else start(u);
    }
  } catch (err) { msg.textContent = err.message; }
};

$('logout').onclick = async () => { await api('/api/logout', { method: 'POST' }); location.reload(); };

/* ---------- chat ---------- */
async function start(u) {
  me = u;
  $('auth').hidden = true; $('app').hidden = false;
  setAvatar($('meAvatar'), me.displayName, me.id);
  $('meAvatar').title = me.displayName;

  conn = new signalR.HubConnectionBuilder().withUrl('/chat').withAutomaticReconnect().build();
  conn.on('message', onMessage);
  conn.on('presence', (id, online) => {
    const c = contacts.find(x => x.id === id);
    if (c) { c.online = online; if (!online) c.lastSeenAt = new Date().toISOString(); renderContacts(); updateHead(); }
  });
  conn.onreconnected(loadContacts);
  conn.onclose(() => location.reload()); // phiên hết hạn hoặc bị khóa
  await conn.start();
  await loadContacts();
}

async function loadContacts() { contacts = await api('/api/users'); renderContacts(); updateHead(); }

function renderContacts() {
  const q = $('search').value.trim().toLowerCase();
  const ul = $('contacts'); ul.replaceChildren();
  const shown = contacts.filter(c => c.displayName.toLowerCase().includes(q));
  if (!shown.length) { ul.append(el('li', 'none', 'Chưa có người dùng nào.')); return; }
  for (const c of shown) {
    const li = el('li', 'contact' + (peer?.id === c.id ? ' sel' : '') + (c.unread ? ' unread' : ''));
    const av = el('div', 'avatar'); setAvatar(av, c.displayName, c.id, c.online);
    const info = el('div', 'info');
    const row = el('div', 'row');
    row.append(el('span', 'name', c.displayName), el('span', 'time', c.last ? hhmm(c.last.sentAt) : ''));
    info.append(row, el('div', 'prev', preview(c.last)));
    li.append(av, info);
    li.onclick = () => open(c);
    ul.append(li);
  }
}
$('search').oninput = renderContacts;

function updateHead() {
  if (!peer) return;
  const c = contacts.find(x => x.id === peer.id) || peer;
  setAvatar($('peerAvatar'), c.displayName, c.id, c.online);
  $('peerName').textContent = c.displayName;
  $('peerStatus').textContent = c.online ? 'Đang hoạt động' : c.lastSeenAt ? 'Truy cập lúc ' + hhmm(c.lastSeenAt) : 'Ngoại tuyến';
}

async function open(c) {
  peer = c; c.unread = false;
  $('empty').hidden = true; $('pane').hidden = false;
  $('app').classList.add('in-chat');
  updateHead(); renderContacts();
  const msgs = await api('/api/messages/' + c.id);
  if (peer.id !== c.id) return; // người dùng đã chuyển cuộc trò chuyện khác
  $('msgs').replaceChildren(...msgs.map(bubble));
  scrollDown(); $('text').focus();
}
$('back').onclick = () => { peer = null; $('app').classList.remove('in-chat'); $('pane').hidden = true; $('empty').hidden = false; renderContacts(); };

function bubble(m) {
  const b = el('div', 'm' + (m.fromId === me.id ? ' mine' : ''));
  const url = m.url && m.url.startsWith('/files/') ? m.url : null;
  if (m.kind === 'image' && url) {
    const img = el('img'); img.src = url; img.alt = m.fileName || 'Hình ảnh'; img.loading = 'lazy';
    img.onclick = () => window.open(url, '_blank', 'noopener');
    b.append(img);
  } else if (m.kind === 'file' && url) {
    const a = el('a', 'file'); a.href = url + '?n=' + encodeURIComponent(m.fileName || 'file');
    const t = el('div'); t.append(el('b', '', m.fileName || 'file'), el('small', '', 'Nhấn để tải về'));
    a.append(el('span', 'ico', '📄'), t); b.append(a);
  } else b.append(el('span', '', m.text));
  b.append(el('time', '', hhmm(m.sentAt)));
  return b;
}

const scrollDown = () => { $('msgs').scrollTop = $('msgs').scrollHeight; };

function onMessage(m) {
  const otherId = m.fromId === me.id ? m.toId : m.fromId;
  const c = contacts.find(x => x.id === otherId);
  if (!c) return loadContacts();
  c.last = { kind: m.kind, text: m.text, fileName: m.fileName, sentAt: m.sentAt, mine: m.fromId === me.id };
  contacts.sort((a, b) => (b.last?.sentAt || '').localeCompare(a.last?.sentAt || ''));
  if (peer?.id === otherId) { $('msgs').append(bubble(m)); scrollDown(); } else if (m.fromId !== me.id) c.unread = true;
  renderContacts();
}

const send = (kind, text, url, fileName) => conn.invoke('Send', peer.id, kind, text, url, fileName);

$('composer').onsubmit = async e => {
  e.preventDefault();
  const t = $('text').value.trim();
  if (!t || !peer) return;
  $('text').value = '';
  await send('text', t, null, null);
};

async function upload(input) {
  const f = input.files[0]; input.value = '';
  if (!f || !peer) return;
  const fd = new FormData(); fd.append('file', f);
  try {
    const r = await api('/api/upload', { method: 'POST', body: fd });
    await send(r.kind, null, r.url, r.fileName);
  } catch (err) { alert(err.message); }
}
$('imgPick').onchange = e => upload(e.target);
$('filePick').onchange = e => upload(e.target);

/* ---------- khởi động: nếu đã có phiên thì vào thẳng ---------- */
(async () => {
  try {
    const u = await api('/api/me');
    if (u.role === 'admin') location.href = '/admin.html'; else start(u);
  } catch { /* chưa đăng nhập */ }
})();
