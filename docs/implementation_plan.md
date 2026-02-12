# Email Confirmation Flow Implementation Plan

The goal is to implement a functional email confirmation flow using an SMTP server (specifically Google's SMTP with App Password) and a professional Razor-based email template.

## User Review Required

> [!IMPORTANT]
> **External Dependencies**: We will be using `MailKit` and `MimeKit` for sending emails.
> **Status**: User confirmed packages are installed.

> [!NOTE]
> **Configuration**: You will need to provide your Google App Password.
> **Status**: User requested instructions for User Secrets.

## Configuration Guide: User Secrets
To keep your credentials safe, right-click on the `Onyx.IdP.Web` project in Visual Studio and select **"Manage User Secrets"**.
Allowed structure:
```json
{
  "EmailSettings": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "Onyx Identity",
    "SenderEmail": "your-email@gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "YOUR_APP_PASSWORD_HERE"
  }
}
```

## Proposed Changes

### Core Layer (Onyx.IdP.Core)

#### [NEW] [EmailSettings.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Core/Settings/EmailSettings.cs)
- Define a class to hold SMTP configuration.

### Infrastructure Layer (Onyx.IdP.Infrastructure)

#### [NEW] [MailKitEmailSender.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Infrastructure/Services/MailKitEmailSender.cs)
- **New Class**: Implementation of `IEmailSender` using `MailKit`.
- Inject `IOptions<EmailSettings>` and `ILogger<MailKitEmailSender>`.
- Implement `SendEmailAsync` to connect to the SMTP server and send the email.

#### [MODIFY] [DependencyInjection.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Infrastructure/DependencyInjection.cs)
- Register `MailKitEmailSender` as the implementation for `IEmailSender`.
- *Note: We will remove the registration for the old `EmailSender` or just swap them.*

### Web Layer (Onyx.IdP.Web)

#### [NEW] [RazorViewToStringRenderer.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Services/RazorViewToStringRenderer.cs)
- A dedicated service to render `.cshtml` views into string HTML.

#### [NEW] [EmailConfirmationTemplate.cshtml](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Views/Shared/EmailTemplates/EmailConfirmationTemplate.cshtml)
- A professional, responsive HTML email template.
- Will accept a functional model (e.g., `ConfirmationEmailViewModel`).

#### [MODIFY] [AuthController.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Features/Auth/AuthController.cs)
- Inject `RazorViewToStringRenderer`.
- In `Register` action:
    - Generate the confirmation token and link.
    - Render the `EmailConfirmationTemplate` with the link.
    - Call `_emailSender.SendEmailAsync` with the rendered HTML.

#### [MODIFY] [appsettings.json](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/appsettings.json)
- Add `EmailSettings` section with placeholders (for structure reference).

#### [MODIFY] [Program.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Program.cs)
- Register `EmailSettings` configuration.
- Register `RazorViewToStringRenderer` as a transient service.

## Verification Plan

### Manual Verification
1.  **Configuration**: Add User Secrets as described above.
2.  **Execution**:
    - Run the application.
    - Navigate to `/Auth/Register`.
    - Register a new user.
3.  **Observation**:
    - Check the inbox of the registered email.
    - Verify the email is received and styled correctly.
    - Click the link and verify confirmation.
