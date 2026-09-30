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
  // Prefer a direct "Message"/"InMail" button on the profile.
  const messageButton = await waitForButton([/^message$/i, /inmail/i], [
    "[aria-label='Message']",
    "[aria-label*='Message']",
    "[aria-label*='InMail']",
  ]);

  if (messageButton) {
    await clickEl(messageButton);
    return await fillComposerAndSend(body, subject);
  }

  // Otherwise try to Connect with a note.
  const connectButton = await findTopCardConnect();
  if (connectButton) {
    await clickEl(connectButton);

    const addNote = await waitForButton([/add a note/i, /add note/i], [
      "div[role='menu'] button[aria-label*='Add a note']",
    ]);
    if (!addNote) {
      throw new Error("Could not find the 'Add a note' option after Connect.");
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

  throw new Error('No Message or Connect button found on this profile.');
}

async function fillComposerAndSend(body, subject) {
  const box = await waitForSelector(["div[role='textbox']", '.msg-form__contenteditable']);
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
  ]);
  if (!send) {
    throw new Error('Could not find the Send button.');
  }

  await clickEl(send);
  return { sent: true };
}

async function findTopCardConnect() {
  const title = document.title || '';
  const name = title.includes(' | ') ? title.split(' | ')[0].trim() : '';
  if (!name) {
    return null;
  }

  return waitForButton(
    [],
    [
      `a[aria-label*='${name}'][aria-label*='connect' i]`,
      `button[aria-label*='${name}'][aria-label*='connect' i]`,
    ],
    8000,
  );
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
