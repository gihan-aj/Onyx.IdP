# Onyx Identity Provider

> A modern, secure OpenID Connect (OIDC) and OAuth 2.0 Identity Provider built with ASP.NET Core and OpenIddict.

Onyx.IdP serves as a centralized authentication server, providing Single Sign-On (SSO) and API security capabilities for distributed systems. It features a robust administration console, dynamic scope management, and a clean, vertical-slice architecture.

## key Features

### Core Identity & Security
-   **OpenID Connect & OAuth 2.0**: Powered by [OpenIddict](https://github.com/openiddict/openiddict-core), supporting standard flows (Authorization Code, Client Credentials, Refresh Token).
-   **Secure Defaults**: Enforced PKCE for public clients, secure cookie policies, and anti-forgery protection.
-   **Role-Based Access Control (RBAC)**: Fine-grained permissions for users and administrators.
-   **Protected Accounts**: Built-in protection for critical "SuperAdmin" roles and users to prevent accidental lockout.

### Administration Console
A comprehensive Admin UI built with ASP.NET Core MVC:
-   **User Management**: create, search, lock, deactivate, and delete users.
-   **Role Management**: Create and manage roles with permission assignment.
-   **Scope Management**: Dynamic management of OIDC scopes and associated resources.
-   **Client Application Management**:
    -   Register Web Apps, Mobile Apps, and Machine-to-Machine (M2M) services.
    -   Configure Grant Types (Auth Code, Client Credentials, Refresh Token).
    -   Manage Redirect URIs and Scopes.
    -   Action Modal workflow for critical operations.

### Architecture & Tech Stack
-   **Framework**: ASP.NET Core 8 / 9
-   **Persistence**: Entity Framework Core (SQL Server)
-   **Architecture**: **Vertical Slice Architecture** organizing code by feature (e.g., `Features/Admin/Users`, `Features/Connect`) rather than technical layers, promoting cohesion and maintainability.
-   **Background Jobs**: [Quartz.NET](https://www.quartz-scheduler.net/) for automated cleanup of expired tokens and consents.
-   **Frontend**: Server-side rendered Razor Views with a custom, lightweight CSS design system (no heavy CSS frameworks). Uses [Lucide](https://lucide.dev/) for iconography.

## Getting Started

### Prerequisites
-   .NET 8.0 SDK or later
-   SQL Server (LocalDB or full instance)

### Installation

1.  **Clone the repository**
    ```bash
    git clone https://github.com/yourusername/Onyx.IdP.git
    cd Onyx.IdP
    ```

2.  **Configure Database**
    Update `appsettings.json` connection string if necessary (defaults to LocalDB).

3.  **Run Migrations & Seed Data**
    The application automatically applies migrations and seeds initial data (Admin user, standard scopes) on startup.
    ```bash
    dotnet run --project src/Onyx.IdP.Web
    ```

4.  **Access the Application**
    -   **Home**: `https://localhost:5001`
    -   **Admin Console**: `https://localhost:5001/Admin/Users`

### Default Credentials
-   **Email**: `admin@onyx.com`
-   **Password**: `Admin123!`

## Project Structure

```
src/Onyx.IdP.Web/
├── Features/               # Vertical Slices
│   ├── Admin/              # Administration Context
│   │   ├── ClientApps/     # Client Management
│   │   ├── Roles/          # Role Management
│   │   ├── Scopes/         # Scope Management
│   │   └── Users/          # User Management
│   ├── Auth/               # Authentication (Login, Register)
│   ├── Connect/            # OIDC Endpoints (Authorize, Token)
│   └── Shared/             # Shared Layouts & Components
├── Core/                   # Domain Entities
├── Infrastructure/         # Data Access & Seeding
└── wwwroot/                # Static Assets (CSS, JS)
```