# Clipboard Sender (Windows Tray App)

A Windows **system-tray resident** clipboard sender.  
Its goal is to let you send your clipboard contents to a configured destination as quickly as possible.

This app was developed with integration with **Notemod-selfhosted** in mind.  
https://github.com/StayHomeLabNet/Notemod-selfhosted

---

## Features

- **Runs in the system tray** (stays out of your way)
- Tray icon makes it easy to see whether sending is **enabled / disabled**
- No need to ship an `Assets` folder: supports embedding icons/resources into the app (**Resources**)
- Multi-language UI (Japanese/English and more)  
  - As a sign of respect for the original Notemod author, **Turkish (TR)** is also included

---

## Intended use

This app is a helper tool to speed up everyday copy/paste workflows **without relying on external services**.  
The purpose of this project is summarized in the first lines of this README:

> A Windows **system-tray resident** clipboard sender.  
> Its goal is to let you send your clipboard contents to a configured destination as quickly as possible.

When used together with Notemod-selfhosted, these are typical use cases:

- Quickly pass text copied on a PC to another device (iPhone / another PC)
- Send clipboard contents to a fixed destination (personal notes, a work server, a team endpoint, etc.)
- Reduce the friction of “finding where to paste”, so you can stay in the flow

Examples using iPhone Shortcuts:

- Use iPhone Shortcuts to quickly **fetch** the PC clipboard contents
- Use iPhone Shortcuts to quickly **send** clipboard contents to Notemod (PC)

---

## Requirements

- Windows 10 / 11
- .NET desktop (WinForms)
  - Target framework: **.NET 8**

---

## Installation

1. Download the latest release from **GitHub Releases**
 - https://github.com/StayHomeLabNet/ClipboardSender/releases
2. Extract and run the `.exe`

- 🇺🇸 [Why the exe file is large (English)](docs/exe_size_en.md)
This executable is built as a self-contained .NET application.
The .NET runtime is bundled inside the exe, so no additional installation is required.

The larger file size (~140MB) is expected and intentional.

---

## Usage

### Startup & basic behavior

- On startup, the app stays in the **system tray**
- The tray icon indicates whether sending is **enabled / disabled**

> Click actions: Left-click toggles enable/disable, right-click opens the menu.

### Settings screen

On the bottom row of the Settings screen (left to right):

- **Help** (opens the help page)
- **App name**
- **Version**
- **Website link** (shown to the right of Version)

---

## Build (for developers)

### Prerequisites

- Visual Studio (e.g., Visual Studio 2026)
- “.NET Desktop Development” workload

### Steps

1. Clone this repository
2. Open the `.sln` in Visual Studio
3. Build in `Release`
4. Run the generated `.exe`

---

## About embedding icons/resources (Resources)

To distribute the app as a single folder (without `Assets/`), it is recommended to embed tray icons into **Resources**.

This project uses the original **Notemod** app icon with respect.

- Example: enabled `tray_on.ico` / disabled `tray_off.ico` (names can be adjusted)
- Add them to `Properties/Resources.resx` and reference them from code to make shipping easier

---

## Security / Privacy

- Clipboard contents may include sensitive information.
- Make sure your destination is safe (HTTPS, authentication, storage policy, etc.).

---

## Acknowledgements

This project learned a lot from Notemod.  
As a sign of respect for the author, **Turkish (TR)** was added as one of the supported UI languages.

---

## License

- MIT License  
- Place the `LICENSE` file at the repository root.

---

## Contributing

Issues and Pull Requests are welcome.

- Bug reports: include steps to reproduce, expected vs actual behavior, OS version, and app version
- Feature requests: share the use case (“why” it matters) to help discussion

---

## Links

- Web: https://stayhomelab.net/
- Help: https://stayhomelab.net/ClipboardSender
- Notemod-selfhosted: https://github.com/StayHomeLabNet/Notemod-selfhosted
