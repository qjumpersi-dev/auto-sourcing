const DEFAULT_API = 'https://www.quickmatchai.com';

const $ = (id) => document.getElementById(id);

function setStatus(text, kind) {
  const el = $('status');
  el.textContent = text;
  el.className = 'status' + (kind ? ' ' + kind : '');
}

async function render() {
  const state = await chrome.storage.local.get(['apiUrl', 'token', 'user', 'enabled']);
  $('apiUrl').value = state.apiUrl || DEFAULT_API;
  $('enabled').checked = !!state.enabled;

  if (state.token && state.user) {
    setStatus(`Connected as ${state.user.displayName || state.user.email}.`, 'ok');
  } else {
    setStatus('Not connected.');
  }
}

$('connect').addEventListener('click', async () => {
  const apiUrl = ($('apiUrl').value || DEFAULT_API).replace(/\/+$/, '');
  const code = ($('code').value || '').trim().toUpperCase();
  if (!code) {
    setStatus('Enter the connect code from the app.', 'err');
    return;
  }

  setStatus('Connecting...');
  try {
    const res = await fetch(`${apiUrl}/api/auth/extension/exchange`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code }),
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      setStatus(data.error || `Could not connect (${res.status}).`, 'err');
      return;
    }

    await chrome.storage.local.set({ apiUrl, token: data.token, user: data.user, enabled: true });
    $('enabled').checked = true;
    $('code').value = '';
    setStatus(`Connected as ${data.user?.displayName || data.user?.email}.`, 'ok');
    chrome.runtime.sendMessage({ type: 'RUN_NOW' });
  } catch (e) {
    setStatus(`Could not reach the app: ${e.message}`, 'err');
  }
});

$('enabled').addEventListener('change', async (e) => {
  await chrome.storage.local.set({ enabled: e.target.checked });
  if (e.target.checked) {
    chrome.runtime.sendMessage({ type: 'RUN_NOW' });
  }
});

$('disconnect').addEventListener('click', async () => {
  await chrome.storage.local.remove(['token', 'user']);
  await chrome.storage.local.set({ enabled: false });
  $('enabled').checked = false;
  setStatus('Disconnected.');
});

render();
