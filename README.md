# Clipboard Sync

Clipboard Sync is a Windows tray application for sending and receiving clipboard contents in coordination with Notemod-selfhosted.

It is an extended version of Clipboard Sender, and now supports sending and receiving **text / images / files**.

Combined with Notemod-selfhosted (server) + Clipboard Sync + iPhone Shortcuts / Back Tap / Action Button, etc., it is designed for a workflow like this:

    Copy text on iPhone, then press a hotkey once on Windows to complete paste automatically

## Version

**Clipboard Sync v1.1.0**

## Main Features

### Send
- Send **text** from the clipboard to `api.php`
- Send **images** from the clipboard to `api.php`
- Send **files** from the clipboard to `api.php`
- Always running in the tray and monitoring continuously
- Toggle sending ON / OFF
- Toggle send success message ON / OFF

### Receive
- **Receive Latest hotkey**
- **Receive Text hotkey**
- **Receive Image hotkey**
- **Receive File hotkey**
- Set received content to the Windows clipboard
- Optionally **auto-paste with Ctrl+V**

### Cleanup / Management
- **Delete all INBOX items** using `cleanup_api.php`
- **Backup count display**
- **Image count display**
- **File count display**
- **Delete all backups**
- **Delete all images and files (delete all media)**
- Automatic cleanup once per day or at fixed minute intervals

### UI / Operation
- Runs in the Windows tray
- Supports English / Japanese / Türkçe
- Hotkeys can be cleared with **Delete or Backspace**
- Helper button that extracts the `/api/` directory from the `api.php` field and copies it to the Read API URL / Cleanup API URL fields
- URL directory input is also supported

## System Requirements

- Windows 10 / 11
- Windows Forms application based on .NET 8
- The following APIs must exist on the Notemod-selfhosted side:
  - `api.php`
  - `read_api.php`
  - `cleanup_api.php`

## Installation
- Download the latest release from GitHub Releases
- Launch the EXE
- Enter the URL / token from **Settings** in the tray menu

## Uninstallation
- Delete the EXE
- Delete the entire `Clipboard Sync` folder from `C:\Users\<username>\AppData\`

## For Developers (Architecture / Project Structure)
This project uses a modular architecture that separates directories by responsibility in order to improve maintainability and extensibility.  
In Clipboard Sync v1.1.0, in addition to the traditional text send/receive functions, **image send/receive, file send/receive, Receive Latest hotkey, media deletion, and URL normalization support** have been added, and each feature is separated by responsibility.

- `Api/`  
  Handles HTTP communication.  
  Files such as `Sender.cs`, `Receiver.cs`, `CleanupApi.cs`, and `ConnectionTester.cs` are placed here, separating sending, receiving, cleanup, and connection testing.  
  In v1.1.0, it supports **text / image / file sending**, **text / image / file receiving**, the **Receive Latest hotkey** using `latest_clip_type`, and **automatic API URL normalization**.

- `Models/`  
  Manages settings and data structures.  
  `AppSettings.cs` stores send settings, cleanup settings, receive settings, multiple receive hotkey settings, Basic authentication information, language settings, and more.  
  In v1.1.0, multiple receive hotkey settings, including the **Receive Latest hotkey**, have been added.

- `Native/`  
  Handles Windows API, clipboard operations, and hotkey control.  
  It contains files such as `ClipboardWatcherForm.cs`, `ClipboardUtil.cs`, and `PasteHelper.cs`.  
  In v1.1.0, it includes processing that supports **clipboard monitoring for text / images / files**, **multiple hotkey registrations**, **hotkey clearing with Delete / Backspace**, and **prevention of resend loops after receiving**.

- `Services/`  
  Handles application logic and shared functionality.  
  It contains files such as `SettingsStore.cs`, `CleanupScheduler.cs`, `I18n.cs`, `AppInfo.cs`, and `EmbeddedIcon.cs`.  
  It covers settings storage, scheduled cleanup, multilingual support, app information retrieval, and more. In v1.1.0, it includes **multilingual text for the latest features** and **expanded automatic cleanup**.

- `UI/`  
  Handles UI control for the settings screen and tray-resident application.  
  It contains files such as `TrayAppContext.cs`, `SettingsForm.cs`, and `SettingsForm.Layout.cs`.  
  In v1.1.0, the UI has been extended mainly around the **Send / Delete tab** and the **Receive tab**, including:
  - Receive Latest hotkey
  - Text / Image / File receive hotkeys
  - Hotkey clear instruction label
  - “Copy directory to all API URLs” button
  - Image count / File count display

* The settings screen (`SettingsForm`) uses a partial class to separate **layout (appearance)** and **logic**.  
This makes it easier to maintain by separating UI placement adjustments from behavior implementation.

* As of v1.1.0, Clipboard Sync is no longer just a simple text sending tool; it is structured as a **bidirectional clipboard synchronization client** that handles **text / images / files**.

## API-side Requirements

### Write
Clipboard Sync uses `api.php` when sending.

Supported content:
- text
- image
- file

### Read
Clipboard Sync uses `read_api.php` when receiving.

Actions used:
- `action=latest_note`
- `action=latest_image`
- `action=latest_file`
- `action=latest_clip_type`

### Cleanup
Clipboard Sync uses `cleanup_api.php` for deletion and count checks.

Main operations used:
- `category=INBOX`
- `purge_bak`
- `purge_images`
- `purge_files`
- `purge_media`
- `dry_run=2`
- `action=backup_now`

## URL Input Specification

Each URL field is automatically normalized internally no matter which of the following formats is entered.

### Send URL / Connection Test
Any of the following can be entered:
- `https://example.com/notemod/api/`
- `https://example.com/notemod/api`
- `https://example.com/notemod/api/api.php`
- `https://example.com/notemod/api/read_api.php`
- `https://example.com/notemod/api/cleanup_api.php`

During sending and connection testing, it is automatically normalized to `api.php`.

### Read API URL
Any of the following can be entered:
- `https://example.com/notemod/api/`
- `https://example.com/notemod/api`
- `https://example.com/notemod/api/api.php`
- `https://example.com/notemod/api/read_api.php`
- `https://example.com/notemod/api/cleanup_api.php`

During receiving, it is automatically normalized to `read_api.php`.

### Cleanup API URL
Any of the following can be entered:
- `https://example.com/notemod/api/`
- `https://example.com/notemod/api`
- `https://example.com/notemod/api/api.php`
- `https://example.com/notemod/api/read_api.php`
- `https://example.com/notemod/api/cleanup_api.php`

During cleanup, it is automatically normalized to `cleanup_api.php`.

## Receive Latest Hotkey

The **Receive Latest hotkey** checks the return value of `read_api.php?action=latest_clip_type` and automatically determines what to receive.

Decision rules:
- `type = note` → `action=latest_note`
- `type = image` → `action=latest_image`
- `type = file` → `action=latest_file`

This allows a single hotkey to receive the latest clipboard item on the server, whether it is text, an image, or a file.

## Settings Screen

### Send / Delete Tab
- POST URL
- Token
- Connection Test
- Copy directory to all API URLs
- Enable / Disable sending
- Show send success message
- Send hotkey
- Basic authentication
- Cleanup API URL
- Cleanup token
- Automatic cleanup settings
- Backup count
- Image count
- File count
- Delete all backups
- Delete all media
- Language settings

### Receive Tab
- Read API URL
- Token
- Hotkeys can be cleared with Delete or Backspace
- Receive Latest hotkey
- Receive Text hotkey
- Receive Image hotkey
- Receive File hotkey
- Auto-paste
- Clipboard stabilization wait time

## About Hotkeys

- You can set hotkeys with modifier keys
- Example: `Ctrl + Alt + R`
- Press **Delete** or **Backspace** to clear that hotkey field
- Cleared hotkeys are shown as **Disabled** and will not be registered

## Priority of Sent Content

When monitoring the clipboard, content is detected in the following priority order:

1. File
2. Image
3. Text

In other words, if a file exists in the clipboard, it is prioritized over images and text as the send target.

## Loop Prevention

After received content is set to the local clipboard, suppression processing prevents that change from being sent again automatically.

This prevents an infinite loop like:

- Receive
- Reflect locally
- Resend the same content
- Receive again

## About Auto-paste

Auto-paste after receiving works by internally sending `Ctrl+V`.

Notes:
- It may not be possible to paste into apps running with administrator privileges
- It may fail depending on the state of the destination app

## Basic Authentication

You can connect to APIs protected with Basic authentication if needed.

Settings:
- Username
- Password

## Settings Storage

Settings are saved per Windows user.  
Tokens, Basic authentication passwords, cleanup tokens, and similar values are encrypted using DPAPI.

## Main Additions / Improvements in v1.1.0

Main differences from v1.0.2:

- Image sending
- File sending
- Image receiving
- File receiving
- Receive Latest hotkey
- Automatic detection of receive type using `latest_clip_type`
- Multiple receive hotkeys
- Hotkey clearing with Delete / Backspace
- More flexible URL input
- Image count / File count display
- Delete all media
- Improved settings screen layout
- Bulk API URL copy helper button

## Troubleshooting

### Sending works, but the connection test fails
Check URL normalization for the connection test and Basic authentication settings.  
Make sure the implementation accepts any of `/api/`, `api.php`, `read_api.php`, or `cleanup_api.php`.

### The filename is not as expected when receiving a file
Check whether `file.original_name` is included in the response from `latest_clip_type`.

### Image / file counts cannot be retrieved
Check whether `cleanup_api.php` supports `purge_images` / `purge_files` / `dry_run=2`.

### Auto-paste does not work
Check the permissions and focus state of the destination app.

## License

Please follow the license configured for this repository as needed.

## Links

- GitHub: https://github.com/StayHomeLabNet/ClipboardSync
- Help: https://stayhomelab.net/Clipboardsync
- Notemod-selfhosted: https://github.com/StayHomeLabNet/Notemod-selfhosted
