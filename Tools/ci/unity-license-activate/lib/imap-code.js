#!/usr/bin/env node
/**
 * Reads the Unity verification code from the account's email inbox via IMAP.
 *
 * Designed for Unity's login.unity.com "Security check" (new-device email
 * verification) and the classic TFA email flow. Works with any email whose
 * body contains a standalone 6-digit code - no exact-phrase matching.
 */

"use strict";

const { ImapFlow } = require('imapflow');

function getHost(email) {
  const e = email.toLowerCase();
  if (e.includes('@gmail.com')) return 'imap.gmail.com';
  if (e.includes('@hotmail.com')) return 'imap-mail.outlook.com';
  if (e.includes('@outlook.com')) return 'imap-mail.outlook.com';
  if (e.includes('@live.com')) return 'imap-mail.outlook.com';
  if (e.includes('@yahoo.com')) return 'imap.mail.yahoo.com';
  if (e.includes('@foxmail.com')) return 'imap.qq.com';
  if (e.includes('@qq.com')) return 'imap.qq.com';
  if (e.includes('@163.com')) return 'imap.163.com';
  if (e.includes('@proton.me') || e.includes('@protonmail.com')) return '127.0.0.1'; // proton has no IMAP (bridge only)
  return null;
}

function extractCode(text) {
  if (!text) return null;
  // Prefer the pattern "code is 123456" / "code: 123456", else any standalone 6-digit run.
  let m = text.match(/(?:code\D{0,20}?)(\d{6})/i);
  if (m) return m[1];
  m = text.match(/\b(\d{6})\b/);
  return m ? m[1] : null;
}

async function fetchUnityCodeFromInbox(email, password, log) {
  const host = getHost(email);
  if (!host) {
    throw `Unsupported or unknown IMAP provider for ${email} (supported: gmail, outlook/hotmail, yahoo, qq/foxmail, 163)`;
  }

  const client = new ImapFlow({
    host: host,
    port: 993,
    secure: true,
    auth: { user: email, pass: password },
    logger: false,
    emitLogs: false,
  });

  await client.connect();

  // Inbox first; Unity mail occasionally lands in spam.
  const folders = ['INBOX', '[Gmail]/Spam', 'Junk', 'Spam'];
  try {
    for (const folder of folders) {
      const boxExists = await client.list().then((list) =>
        list.some((b) => b.path === folder)).catch(() => folder === 'INBOX');
      if (!boxExists) continue;

      const lock = await client.getMailboxLock(folder).catch(() => null);
      if (!lock) continue;
      try {
        const since = new Date(Date.now() - 1000 * 60 * 60 * 6); // last 6 hours
        const searchQuery = { since: since, from: 'unity' };
        let uids = await client.search(searchQuery, { uid: true });
        if (!uids || uids.length === 0) continue;

        const uid = uids[uids.length - 1]; // newest match
        const msg = await client.fetchOne(String(uid), { envelope: true, source: true }, { uid: true });
        if (!msg || !msg.source) continue;

        const raw = msg.source.toString('utf8');
        // Extract text body from the raw MIME source (handles quoted-printable/base64 loosely).
        let bodyText = raw;

        const qpMatch = raw.match(/Content-Transfer-Encoding:\s*quoted-printable[\s\S]*?\r?\n\r?\n([\s\S]*?)\r?\n--/i);
        if (qpMatch) bodyText = qpMatch[1].replace(/=\r?\n/g, '').replace(/=([0-9A-F]{2})/gi, (_, h) => String.fromCharCode(parseInt(h, 16)));

        const b64Match = raw.match(/Content-Transfer-Encoding:\s*base64[\s\S]*?\r?\n\r?\n([\s\S]*?)\r?\n--/i);
        if (b64Match) {
          try { bodyText = Buffer.from(b64Match[1].replace(/\s+/g, ''), 'base64').toString('utf8'); } catch (_) { /* keep raw */ }
        }

        // If HTML, strip tags.
        if (/<html[\s>]/i.test(bodyText) || /<div|<p|<table/i.test(bodyText)) {
          try { bodyText = bodyText.replace(/<style[\s\S]*?<\/style>/gi, ' ').replace(/<script[\s\S]*?<\/script>/gi, ' ').replace(/<[^>]+>/g, ' '); } catch (_) { /* keep raw */ }
        }

        const code = extractCode(bodyText);
        const subj = msg.envelope && msg.envelope.subject ? msg.envelope.subject : '(no subject)';
        log(`[INFO] IMAP: ${folder}: newest Unity mail "${subj}" -> code ${code ? 'found' : 'not found'}`);
        if (code) return code;
      } finally {
        lock.release();
      }
    }
    return null; // caller retries until timeout
  } finally {
    await client.logout().catch(() => client.close().catch(() => {}));
  }
}

/**
 * Poll the inbox until a 6-digit Unity code appears or the timeout passes.
 * @param {string} email       mailbox address
 * @param {string} password    mailbox app password
 * @param {function} log       logger fn
 * @param {number} timeoutMs   overall wait (default 4 minutes)
 * @param {number} intervalMs  poll interval (default 12 seconds)
 * @returns {Promise<string|null>} the code, or null on timeout
 */
async function getEmailVerificationCode(email, password, log, timeoutMs = 1000 * 60 * 4, intervalMs = 1000 * 12) {
  const start = Date.now();
  for (let attempt = 1; ; ++attempt) {
    const remaining = timeoutMs - (Date.now() - start);
    if (remaining <= 0) {
      log(`[INFO] IMAP: timed out after ${timeoutMs / 1000}s waiting for the Unity verification email`);
      return null;
    }
    try {
      log(`[INFO] IMAP: checking inbox for the Unity verification code (attempt ${attempt})...`);
      const code = await fetchUnityCodeFromInbox(email, password, log);
      if (code) return code;
    } catch (err) {
      log(`[INFO] IMAP: ${err && err.message ? err.message : err}`);
    }
    await new Promise((resolve) => setTimeout(resolve, Math.min(intervalMs, remaining)));
  }
}

/*
 * Module Exports
 */
module.exports.getEmailVerificationCode = getEmailVerificationCode;
