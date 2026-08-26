# Security

## Supported version

Only the latest commit on `main` is supported.

## Security properties

- The game contains no HTTP client, socket client, telemetry uploader, updater,
  shell execution, process launching, native plugin loading, or remote code path.
- No API key, signing key, password, OAuth token, or GitHub credential belongs in
  this repository or in release archives.
- Windows builds use IL2CPP and disallow unsafe C# compilation.
- The build validator permits only `StandaloneWindows64`.
- Release archives must be generated from a clean checkout and scanned before
  upload. Publish a SHA-256 checksum beside every archive.

The four-digit Phase 3 value is game progress stored in Unity PlayerPrefs. It is
not an authentication secret and must never be reused for a real security purpose.

## Reporting

Do not publish exploit details before the maintainer has reviewed them. Report the
affected commit, reproduction steps, impact, and relevant Unity player log through
a private maintainer contact channel.
