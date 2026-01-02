# About the exe file size

## Why is the exe file around 150MB?

The exe file is large because this application is built as a
**self-contained .NET application**.

---

## What is a self-contained application?

This application **includes the .NET runtime inside the exe file**.

As a result:

- No separate .NET installation is required
- No additional DLLs or dependencies are needed
- The app can be run immediately after download
- Runtime version mismatch issues are avoided

---

## Why this approach was chosen

### Benefits for users
- No installation required
- Works out of the box
- Stable behavior across different environments
- Can be used offline
- Friendly for non-technical users

### Benefits for distribution
- High reliability and reproducibility
- Fewer environment-specific issues
- Single executable file distribution

---

## Is this size normal?

Yes, **this size is expected and normal**.

For Windows self-contained .NET applications,
a size of 100–200MB is common.

---

## Can the file size be reduced?

Technically yes, but with trade-offs:

- Users would need to install .NET manually
- The app may fail to start on some systems
- UI, localization, or reflection-related features may break

This project prioritizes
**reliability and ease of use over file size**.
