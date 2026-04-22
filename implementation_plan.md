# Goal Description
Enhance the Admin Account Settings page to include Two-Factor Authentication, Active Sessions, Login History, Notification Preferences, Theme/Language Selectors, Data Exports, and a Danger Zone for account deletion. These updates will turn the profile page into a comprehensive, modern admin control center using Bootstrap 5, FontAwesome, and Toastr.

## Proposed Changes

### Database Layer
- **[MODIFY]** `Models/Entities/User.cs`: Add the following new properties to store admin preferences:
  - `TwoFactorEnabled` (bool)
  - `ThemePreference` (string)
  - `LanguagePreference` (string)
  - `AlertNewRegistration` (bool)
  - `AlertSystemError` (bool)
  - `AlertDailySummary` (bool)
- Add and apply an Entity Framework Core migration to update the `Users` table.

---

### ViewModels
- **[MODIFY]** `Models/ViewModels/AdminSettingsViewModel.cs`: Map the new properties added to the `User` entity so they can be securely transferred and validated via the web form.

---

### Controllers
- **[MODIFY]** `Controllers/AdminController.cs`:
  - `Settings` (GET): Retrieve recent login data from the `AuditLog` table and load user preferences.
  - `Settings` (POST): Update and save the new preference fields.
  - `ExportAuditLogs` (GET): Add a new endpoint to generate and download a CSV file of the admin's `AuditLog` activity.
  - `DeleteAccount` (POST): Add a new endpoint for handling the Danger Zone action, requiring password validation before deactivating or deleting the account.

---

### Views
- **[MODIFY]** `Views/Admin/Settings.cshtml`: Build out a modern, grid-based dashboard using Bootstrap 5 cards. Incorporate the following new sections:
  - **Two-Factor Authentication (2FA)**: Toggle switch and a mock QR code UI.
  - **Active Sessions**: A mock view displaying current sessions, browsers, IPs, and a "Revoke All Other Sessions" button.
  - **Login History**: A responsive table displaying the last 5 logins retrieved from the `AuditLog` table.
  - **Notification Preferences**: Checkboxes for granular alerts.
  - **Appearance & Localization**: Dropdowns for Theme (Light/Dark/System) and Language (English/Amharic).
  - **Data Export**: A distinct button linking to the CSV download action.
  - **Danger Zone**: A distinct area highlighting the "Delete Account" capability requiring password confirmation.

## Verification Plan
### Automated Tests
- Run `.NET build` to ensure the project compiles after model and controller changes.
- Execute the Entity Framework migration command and `database update` to ensure the database schema receives the changes smoothly.

### Manual Verification
- Log in as the Admin and navigate to the Account Settings page.
- Verify the UI layout against the requirements (Bootstrap 5 cards, FontAwesome icons, Toastr notifications).
- Toggle preferences, click "Save", and ensure they persist in the database.
- Click the "Export Logs" button and verify the CSV file downloads successfully.
- Verify that the login history displays logs from the database correctly.
