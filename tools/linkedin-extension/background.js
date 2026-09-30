const ALARM = 'aits-linkedin-poll';
const POLL_MINUTES = 1;

let running = false;

function ensureAlarm() {
  chrome.alarms.create(ALARM, { periodInMinutes: POLL_MINUTES });
}

chrome.runtime.onInstalled.addListener(() => {
  ensureAlarm();
  runQueue();
});

chrome.runtime.onStartup.addListener(() => {
  ensureAlarm();
  runQueue();
});

chrome.alarms.onAlarm.addListener((alarm) => {
  if (alarm.name === ALARM) {
    runQueue();
  }
});

chrome.runtime.onMessage.addListener((message) => {
  if (message && message.type === 'RUN_NOW') {
    runQueue();
  }
});

async function runQueue() {
  if (running) {
    return;
  }

  running = true;
  try {
    const { apiUrl, token, enabled } = await chrome.storage.local.get(['apiUrl', 'token', 'enabled']);
    if (!enabled || !token || !apiUrl) {
      return;
    }

    const res = await fetch(`${apiUrl}/api/linkedin/queue`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      console.warn('[AITS] queue fetch failed', res.status);
      return;
    }

    const items = await res.json();
    for (const item of items || []) {
      await processItem(apiUrl, token, item);
    }
  } catch (e) {
    console.warn('[AITS] queue run failed', e);
  } finally {
    running = false;
  }
}

async function processItem(apiUrl, token, item) {
  let result;
  if (!item.profileUrl) {
    result = { sent: false, error: 'Lead has no LinkedIn URL.' };
  } else {
    try {
      result = await sendViaTab(item.profileUrl, { body: item.body, subject: item.subject });
    } catch (e) {
      result = { sent: false, error: String((e && e.message) || e) };
    }
  }

  try {
    await fetch(`${apiUrl}/api/linkedin/queue/${item.messageId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ sent: result.sent, error: result.error || null }),
    });
  } catch (e) {
    console.warn('[AITS] could not report result', e);
  }
}

function sendViaTab(url, payload) {
  return new Promise((resolve) => {
    chrome.tabs.create({ url, active: false }, (tab) => {
      const tabId = tab.id;
      let settled = false;

      const finish = (value) => {
        if (settled) {
          return;
        }
        settled = true;
        chrome.tabs.onUpdated.removeListener(onUpdated);
        chrome.tabs.remove(tabId, () => void chrome.runtime.lastError);
        resolve(value);
      };

      const onUpdated = (updatedId, info) => {
        if (updatedId !== tabId || info.status !== 'complete') {
          return;
        }

        chrome.tabs.onUpdated.removeListener(onUpdated);

        // Give the single-page app a moment to render the profile actions.
        setTimeout(() => {
          chrome.tabs.sendMessage(tabId, { type: 'SEND_INMAIL', ...payload }, (response) => {
            if (chrome.runtime.lastError) {
              finish({ sent: false, error: chrome.runtime.lastError.message });
            } else {
              finish(response || { sent: false, error: 'No response from the page.' });
            }
          });
        }, 2500);
      };

      chrome.tabs.onUpdated.addListener(onUpdated);
      setTimeout(() => finish({ sent: false, error: 'Timed out waiting for LinkedIn.' }), 90000);
    });
  });
}
