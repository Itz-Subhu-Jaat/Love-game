#!/usr/bin/env python3
"""Store a file's contents as a GitHub Actions secret via the REST API.

Required environment variables (secrets stay off the command line):
  SET_SECRET_REPO   repository in "owner/name" form
  SET_SECRET_TOKEN  PAT with Actions secrets write permission
  SET_SECRET_NAME   secret name, e.g. UNITY_LICENSE
  SET_SECRET_FILE   path to the file whose contents become the secret value

Flow (documented GitHub API):
  1. GET  /repos/{repo}/actions/secrets/public-key
  2. libsodium sealed-box encryption of the value (pynacl)
  3. PUT  /repos/{repo}/actions/secrets/{name}

Exit codes: 0 = stored, 1 = configuration/validation problem.
"""
import base64
import json
import os
import sys
import urllib.error
import urllib.request

from nacl import encoding, public  # pynacl


def api(repo, token, method, suffix, payload=None):
    url = f"https://api.github.com/repos/{repo}/{suffix}"
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    request = urllib.request.Request(url, method=method, data=data, headers={
        "Accept": "application/vnd.github+json",
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json",
        "User-Agent": "love-game-ci",
    })
    with urllib.request.urlopen(request) as response:
        body = response.read()
    return json.loads(body) if body else {}


def main():
    repo = os.environ.get("SET_SECRET_REPO", "")
    token = os.environ.get("SET_SECRET_TOKEN", "")
    name = os.environ.get("SET_SECRET_NAME", "")
    path = os.environ.get("SET_SECRET_FILE", "")

    required = {
        "SET_SECRET_REPO": repo,
        "SET_SECRET_TOKEN": token,
        "SET_SECRET_NAME": name,
        "SET_SECRET_FILE": path,
    }
    missing = [key for key, value in required.items() if not value]
    if missing:
        print(f"::error title=set_secret configuration::Missing env vars: {', '.join(missing)}")
        return 1
    if not os.path.isfile(path):
        print(f"::error title=set_secret configuration::File not found: {path}")
        return 1

    try:
        key = api(repo, token, "GET", "actions/secrets/public-key")
        public_key = public.PublicKey(key["key"].encode("ascii"), encoding.Base64Encoder())
        value = open(path, "rb").read().strip()
        if not value:
            print(f"::error title=set_secret configuration::License file is empty: {path}")
            return 1
        encrypted = base64.b64encode(public.SealedBox(public_key).encrypt(value)).decode("ascii")
        api(repo, token, "PUT", f"actions/secrets/{name}",
            {"encrypted_value": encrypted, "key_id": key["key_id"]})
    except urllib.error.HTTPError as error:
        detail = error.read().decode("utf-8", "replace")[:300]
        print(f"::error title=set_secret API failure::HTTP {error.code} - {detail}")
        print("The ACCESS_TOKEN PAT needs the 'Secrets' permission (fine-grained) "
              "or 'repo' scope (classic) for this repository.")
        return 1

    print(f"Secret '{name}' updated ({len(value)} bytes).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
