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
node lib/cli.js EMAIL PASSWORD FILE.alf [--authenticator-key TOTP_KEY]
```

The `.ulf` lands in the current working directory. On failure, `error.png` and
`error.html` (portal screenshot + DOM) are written for diagnostics.

2FA: `--authenticator-key` takes the authenticator-app secret (works with the
`UNITY_TOTP_KEY` secret). Email-code 2FA requires the external
`unity-verify-code` package and is not installed by default.
