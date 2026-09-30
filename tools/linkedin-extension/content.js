chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
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

  // Prefer a direct "Message"/"InMail" button on the profile.
  const messageButton = await waitForButton(
    [/message/i, /inmail/i],
    [
      "[aria-label='Message']",
      "[aria-label*='Message']",
      "[aria-label*='InMail']",
      "button.artdeco-button--primary[aria-label*='essage']",
    ],
    20000,
  );

  if (messageButton) {
    await clickEl(messageButton);
    return await fillComposerAndSend(body, subject);
  }

  // Otherwise try to Connect with a note.
  const connectButton = await waitForButton([/connect/i], [
    "button[aria-label*='Connect']",
    "a[aria-label*='Connect']",
  ], 8000);

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

  throw new Error(`No Message or Connect button found. ${describePage()} Buttons: ${describeVisibleButtons()}`);
}

function describePage() {
  return `url=${location.href} title="${document.title}"`;
}

async function fillComposerAndSend(body, subject) {
  const box = await waitForSelector(["div[role='textbox']", '.msg-form__contenteditable'], 20000);
  if (!box) {
    throw new Error('Could not open the message composer.');
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

function isAuthWall() {
  return !!document.querySelector('#username') || /\/login|\/authwall|\/checkpoint/.test(location.pathname);
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
