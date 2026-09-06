# Google Drive Duplicate Finder

A C# console app that scans a Google Drive account via the Drive API v3
and reports duplicate files.

## Setup

`DuplicatedFilesGDrive/client_secret.json` in this repo is a placeholder
with the real `client_id`/`client_secret` redacted. To run this yourself:

1. Create (or reuse) a project in the
   [Google Cloud Console](https://console.cloud.google.com/), enable the
   **Google Drive API**, and create an **OAuth client ID** of type
   *Desktop app*.
2. Download its JSON and replace `client_secret.json` with it (or fill the
   `client_id`/`client_secret` placeholders in with your own values).
3. Run the app — the first run opens a browser for the OAuth consent
   flow and caches a token in `token.json` (also git-ignored).
