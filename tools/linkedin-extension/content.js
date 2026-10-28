chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message && message.type === 'FILL_COMPOSER') {
    // The composer opened in its own tab; just fill and send.
    fillComposerAndSend(message.body, message.subject)
      .then((result) => sendResponse(result))
      .catch((e) => sendResponse({ sent: false, error: String((e && e.message) || e) }));
    return true;
  }

  if (!message || message.type !== 'SEND_INMAIL') {
    return;
  }

  handleSend(message)
    .then((result) => sendResponse(result))
    .catch((e) => sendResponse({ sent: false, error: String((e && e.message) || e) }));

  return true;
});

async function handleSend({ body, subject }) {
  // Give the profile page a moment to finish rendering, and nudge lazy sections.
  await sleep(1500);
  window.scrollTo({ top: 0, behavior: 'instant' });
  await sleep(500);

  if (isAuthWall()) {
    throw new Error('Not signed in to LinkedIn (a login screen was shown).');
  }

  // The profile "Message" action is a link to the messaging composer - follow it directly.
  // (Clicking it programmatically doesn't reliably trigger LinkedIn's own handler.)
  const messageHref = await waitForMessageHref(15000);
  if (messageHref) {
    location.assign(messageHref);
    // Let the navigation tear this context down; the background fills the composer on the new page.
    return await new Promise(() => {});
  }

  // Fallback: a real click, for profiles where the action isn't a link.
  const messageButton = await waitForMessageButton(15000);
  if (messageButton) {
    await clickEl(messageButton);
    await sleep(3000);
    const afterClick = describeComposer();
    return await fillComposerAndSend(body, subject, afterClick);
  }

  // Otherwise try to Connect with a note.
  const connectButton = await waitForConnectButton(8000);
  if (connectButton) {
    await clickEl(connectButton);

    const addNote = await waitForButton([/add a note/i, /add note/i], [
      "div[role='menu'] button[aria-label*='Add a note']",
    ]);
    if (!addNote) {
      throw new Error(`Could not find 'Add a note'. ${describePage()} Buttons: ${describeVisibleButtons()}`);
    }

    await clickEl(addNote);

    const noteBox = await waitForSelector([
      "textarea[name='message']",
      '#connect-cta-form__message',
      "div[role='dialog'] textarea",
      'textarea',
    ]);
    if (!noteBox) {
      throw new Error('Could not find the note field.');
    }

    setNativeValue(noteBox, (body || '').slice(0, 300));

    const send = await waitForButton([], [
      "div[role='dialog'] button[aria-label*='Send']",
      "button[aria-label*='Send now']",
      "button[aria-label*='Send']",
    ]);
    if (!send) {
      throw new Error('Could not find the Send button in the connect dialog.');
    }

    await clickEl(send);
    return { sent: true };
  }

  throw new Error(
    `No Message or Connect button found. ${describePage()} MessagingLinks: ${describeMessagingLinks()} Buttons: ${describeVisibleButtons()}`,
  );
}

function describeMessagingLinks() {
  const links = [];
  for (const el of document.querySelectorAll('a[href*="/messaging/"]')) {
    links.push((el.getAttribute('href') || '').slice(0, 70));
    if (links.length >= 5) {
      break;
    }
  }
  return links.join(' | ') || '(none)';
}

async function fillComposerAndSend(body, subject, afterClickSnapshot) {
  const box = await waitForComposer(30000);
  if (!box) {
    throw new Error(
      `Could not open the message composer. ${describePage()} AfterClick: ${afterClickSnapshot ?? '(n/a)'} Candidates: ${describeComposer()}`,
    );
  }

  box.focus();
  await clickEl(box);

  if (!insertText(box, body || '')) {
    box.textContent = body || '';
    box.dispatchEvent(new InputEvent('input', { bubbles: true }));
  }

  const subjectInput = document.querySelector("input[name='subject'], input[placeholder*='Subject']");
  if (subjectInput && subject) {
    setNativeValue(subjectInput, subject);
  }

  const send = await waitForButton([], [
    "button[aria-label*='Send now']",
    "button[aria-label*='Send']",
    '.msg-form__send-button',
  ], 20000);
  if (!send) {
    throw new Error(`Could not find the Send button. Buttons: ${describeVisibleButtons()}`);
  }

  await clickEl(send);
  return { sent: true };
}

// A real "Message" action on a profile - deliberately excludes the global "Messaging" nav link.
function isMessageControl(el) {
  const text = (el.innerText || '').trim().toLowerCase();
  const label = (el.getAttribute('aria-label') || '').trim().toLowerCase();

  if (text === 'messaging' || label === 'messaging') {
    return false;
  }

  if (text === 'message' || label === 'message') {
    return true;
  }

  // e.g. aria-label="Message Jane Doe"
  if (label.startsWith('message ')) {
    return true;
  }

  return text === 'inmail' || label.includes('inmail');
}

// Finds the profile's "Message" link, which points at the messaging composer.
async function waitForMessageHref(timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    for (const el of document.querySelectorAll('a[href]')) {
      const href = el.getAttribute('href') || '';
      if (!href.includes('/messaging/')) {
        continue;
      }
      if (!isVisible(el)) {
        continue;
      }

      const text = (el.innerText || '').trim().toLowerCase();
      const label = (el.getAttribute('aria-label') || '').trim().toLowerCase();
      if (text === 'messaging' || label === 'messaging') {
        continue; // the global nav, not a compose action
      }

      const isCompose = href.includes('thread/new') || href.includes('compose') || isMessageControl(el);
      if (isCompose) {
        return new URL(href, location.origin).toString();
      }
    }

    await sleep(300);
  }

  return null;
}

async function waitForMessageButton(timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    let best = null;
    for (const el of document.querySelectorAll('button, a')) {
      if (!isVisible(el) || !isMessageControl(el)) {
        continue;
      }

      const inTopCard = el.closest('.pv-top-card, .pv-top-card-v2-ctas, .pv-top-card--list, main') ? 2 : 1;
      if (!best || inTopCard > best.score) {
        best = { el, score: inTopCard };
      }
      if (best.score === 2) {
        break;
      }
    }

    if (best) {
      return best.el;
    }

    await sleep(300);
  }

  return null;
}

async function waitForConnectButton(timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    for (const el of document.querySelectorAll('button, a')) {
      if (!isVisible(el)) {
        continue;
      }
      const text = (el.innerText || '').trim().toLowerCase();
      const label = (el.getAttribute('aria-label') || '').trim().toLowerCase();
      if (text === 'connect' || label === 'connect' || label.startsWith('invite ') || label.startsWith('connect ')) {
        return el;
      }
    }
    await sleep(300);
  }
  return null;
}

function isAuthWall() {
  return !!document.querySelector('#username') || /\/login|\/authwall|\/checkpoint/.test(location.pathname);
}

function describePage() {
  return `url=${location.href} title="${document.title}"`;
}

// Used in error messages so we can see what the page actually contains.
function describeVisibleButtons() {
  const nodes = document.querySelectorAll('button, a, [role=button]');
  const seen = [];
  for (const el of nodes) {
    if (!isVisible(el)) {
      continue;
    }

    const label = (el.getAttribute('aria-label') || '').trim();
    const text = (el.innerText || '').trim().replace(/\s+/g, ' ');
    const value = (text || label).slice(0, 28);
    if (value && !seen.includes(value)) {
      seen.push(value);
    }
    if (seen.length >= 18) {
      break;
    }
  }

  return seen.join(' | ') || '(none)';
}

function describeTextboxes() {
  const nodes = document.querySelectorAll("[role='textbox'], [contenteditable='true'], textarea, input");
  const seen = [];
  for (const el of nodes) {
    if (!isVisible(el)) {
      continue;
    }

    const cls = typeof el.className === 'string' && el.className ? '.' + el.className.split(' ')[0].slice(0, 24) : '';
    const value = `${el.tagName.toLowerCase()}${cls}`;
    if (!seen.includes(value)) {
      seen.push(value);
    }
    if (seen.length >= 12) {
      break;
    }
  }

  return seen.join(' | ') || '(none)';
}

// The composer may exist before it has a laid-out size in a busy SPA, so don't require visibility.
async function waitForComposer(timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const el = findComposer();
    if (el) {
      return el;
    }
    await sleep(300);
  }
  return null;
}

function findComposer() {
  const scoped = document.querySelector(
    ".msg-form [contenteditable], .msg-form [role='textbox'], .msg-form__contenteditable, " +
      "[role='dialog'] [contenteditable], [role='dialog'] [role='textbox'], " +
      "[class*='compose'] [contenteditable], [class*='compose'] [role='textbox']",
  );
  if (scoped) {
    return scoped;
  }

  return (
    document.querySelector("[role='textbox']") ||
    document.querySelector("[contenteditable='true'], [contenteditable='']") ||
    document.querySelector('textarea') ||
    null
  );
}

// Detailed dump of composer candidates, for when detection fails.
function describeComposer() {
  const seen = [];

  const add = (label, el) => {
    const cls =
      typeof el.className === 'string' && el.className
        ? '.' + el.className.trim().split(/\s+/).slice(0, 2).join('.')
        : '';
    const entry = `${label}:${el.tagName.toLowerCase()}${cls}`;
    if (!seen.includes(entry)) {
      seen.push(entry);
    }
  };

  for (const el of document.querySelectorAll("[contenteditable], [role='textbox']")) {
    add('editable', el);
    if (seen.length >= 12) break;
  }
  for (const el of document.querySelectorAll('textarea, input')) {
    add(el.tagName.toLowerCase(), el);
    if (seen.length >= 20) break;
  }
  for (const el of document.querySelectorAll("[class*='msg-form'], [class*='compose'], [role='dialog']")) {
    add('container', el);
    if (seen.length >= 26) break;
  }

  return seen.join(' | ') || '(none)';
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitForSelector(selectors, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    for (const selector of selectors) {
      const el = document.querySelector(selector);
      if (el && isVisible(el)) {
        return el;
      }
    }
    await sleep(300);
  }
  return null;
}

async function waitForButton(textPatterns, cssFallbacks, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    for (const selector of cssFallbacks) {
      const el = document.querySelector(selector);
      if (el && isVisible(el)) {
        return el;
      }
    }

    if (textPatterns.length > 0) {
      const candidates = document.querySelectorAll('button, a, [role=button]');
      for (const el of candidates) {
        if (!isVisible(el)) {
          continue;
        }
        const label = (el.getAttribute('aria-label') || '').trim();
        const text = (el.innerText || '').trim();
        if (textPatterns.some((re) => re.test(label) || re.test(text))) {
          return el;
        }
      }
    }

    await sleep(300);
  }
  return null;
}

function isVisible(el) {
  const rect = el.getBoundingClientRect();
  return rect.width > 0 && rect.height > 0;
}

async function clickEl(el) {
  el.scrollIntoView({ block: 'center' });
  await sleep(150);

  // Some LinkedIn controls listen on pointer/mouse events rather than click.
  const options = { bubbles: true, cancelable: true, view: window };
  try {
    el.dispatchEvent(new PointerEvent('pointerdown', options));
    el.dispatchEvent(new MouseEvent('mousedown', options));
    el.dispatchEvent(new PointerEvent('pointerup', options));
    el.dispatchEvent(new MouseEvent('mouseup', options));
  } catch {
    // Older engines may not support PointerEvent; the click below still fires.
  }

  el.click();
}

function insertText(el, text) {
  try {
    el.focus();
    return document.execCommand('insertText', false, text);
  } catch {
    return false;
  }
}

function setNativeValue(el, value) {
  const proto =
    el.tagName === 'TEXTAREA'
      ? HTMLTextAreaElement.prototype
      : el.tagName === 'INPUT'
        ? HTMLInputElement.prototype
        : null;

  const setter = proto && Object.getOwnPropertyDescriptor(proto, 'value')?.set;
  if (setter) {
    setter.call(el, value);
  } else {
    el.value = value;
  }

  el.dispatchEvent(new Event('input', { bubbles: true }));
  el.dispatchEvent(new Event('change', { bubbles: true }));
}
