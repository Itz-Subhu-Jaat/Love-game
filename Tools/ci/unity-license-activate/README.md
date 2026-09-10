# unity-license-activate (vendored, patched)

Automates Unity Personal license acquisition: logs into the licensing portal
with email + password, submits the `.alf` activation request, selects the free
Personal license and downloads the `.ulf`.

## Why vendored?

The npm package `unity-license-activate` (and the `game-ci/unity-license-activate`
repo) still target Unity's **old** `id.unity.com` login form
(`#new_conversations_create_session_form`), which no longer exists. Unity moved
to `login.unity.com` — a 2-step MUI flow (email → Continue → password), an
OneTrust cookie banner, and a unified `input[name="code"]` 2FA field.

This copy is based on the updated selectors from
`Starprince2021/unity-license-activate` (verified live against
`login.unity.com`), plus one local improvement: the `.ulf` download wait is
bounded to 5 minutes instead of looping forever.

## Usage

```bash
npm install --no-audit --no-fund          # installs puppeteer + chromium
node lib/cli.js EMAIL PASSWORD FILE.alf \
  [--email-password APP_PASSWORD]         # mailbox app password (device-verification code)
  [--authenticator-key TOTP_KEY]          # if Unity 2FA is enabled
```

The `.ulf` lands in the current working directory. On failure, `error.png` and
`error.html` (portal screenshot + DOM) are written for diagnostics.

## login.unity.com flow handled

1. cookie banner (OneTrust) dismissal,
2. 2-step login (email → Continue → password → sign in),
3. **device verification** ("Security check"): Unity emails a 6-digit code —
   read automatically from the mailbox via IMAP (`--email-password`, supports
   Gmail / Outlook / Yahoo / QQ / 163, searches inbox + spam, polls ~4 min),
4. TOTP 2FA via `--authenticator-key`, email 2FA via the same IMAP reader,
5. `.alf` upload → Personal license → `.ulf` download (bounded 5-min wait).

2FA by email code *and* device verification both need the mailbox's app
password (e.g. Gmail app password), passed as `--email-password` — typically
wired from the `EMAIL_PASSWORD` repository secret.
