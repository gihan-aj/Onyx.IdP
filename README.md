# Onyx Identity Provider (Onyx.IdP)

> A multi-tenant OpenID Connect (OIDC) and OAuth 2.0 identity provider built with ASP.NET Core and OpenIddict — built to give [Onyx.Oms](https://github.com/gihan-aj/Onyx.Oms) real SSO and tenant management, and as a deliberate deep-dive into how multi-tenant auth actually works.

Onyx.IdP is a centralized authentication server providing OpenID Connect / OAuth 2.0 flows, tenant and user management, and impersonation support for the Onyx OMS ecosystem. It was built specifically to learn multi-tenant identity patterns properly before taking the wider Onyx product to a hosted, multi-business SaaS model.

## Core identity & security

- **OpenID Connect & OAuth 2.0** via [OpenIddict](https://github.com/openiddict/openiddict-core) — Authorization Code, Client Credentials, and Refresh Token flows.
- **Secure defaults**: enforced PKCE for public clients, secure cookie policies, anti-forgery protection.
- **Role-based access control** with fine-grained permissions for users and administrators.
- **Protected accounts**: built-in safeguards for "SuperAdmin" roles/users to prevent accidental lockout.

## Multi-tenancy

- Every user belongs to a tenant, and can manage or belong to **multiple businesses** — a user can switch their active tenant context.
- Users with the **impersonation permission** can act on behalf of a specific tenant by supplying it explicitly, which is how platform administration works without a separate admin-only auth path.
- Every user — including platform admins — has a tenant ID, with a default tenant seeded at first run. This keeps the "every entity has a tenant" rule uniform, though in hindsight it also means platform-admin accounts carry a tenant ID that doesn't really mean anything for them — a tradeoff documented below.

## Administration console

A full Admin UI built with ASP.NET Core MVC:
- **User management** — create, search, lock, deactivate, delete.
- **Role management** — create roles and assign permissions.
- **Scope management** — dynamic OIDC scope and resource configuration.
- **Client application management** — register Web, Mobile, and M2M clients; configure grant types, redirect URIs, and scopes; action-modal workflow for critical operations.

## Architecture & tech stack

- **Framework**: ASP.NET Core 10
- **Persistence**: Entity Framework Core (SQL Server)
- **Architecture**: Vertical Slice Architecture — code organized by feature (`Features/Admin/Users`, `Features/Connect`, etc.) rather than technical layers.
- **Background jobs**: [Quartz.NET](https://www.quartz-scheduler.net/) for cleanup of expired tokens and consents.
- **Frontend**: server-rendered Razor Views with a custom lightweight CSS system (no heavy CSS framework), [Lucide](https://lucide.dev/) icons.

## Project structure

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

## Design tradeoffs (honestly assessed)

Building this as a learning project surfaced a few decisions worth calling out rather than glossing over:

- **Tenant ID lives in the IdP database.** This works — tenant switching and impersonation both function correctly — but conceptually an identity provider shouldn't need to know about business-specific tenant data; that's arguably [Onyx.Oms](#)'s concern, not the IdP's. In a later SaaS-focused iteration of this architecture, tenant/organization membership is modeled in the auth provider (Clerk) but kept separate from the business's own Postgres database, synced via webhooks — a cleaner separation of concerns than what's here.
- **Every user needing a tenant ID**, including platform admins with a seeded default tenant, keeps the data model uniform but slightly conflates "platform admin" with "tenant member." A more precise model would treat platform administration as tenant-independent from the start.

These aren't bugs — the system works correctly — but they're the kind of thing you only see clearly once you've built it once.

## Getting started

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB or full instance)

### Installation

```bash
git clone <repository-url>
cd Onyx.IdP
dotnet run --project src/Onyx.IdP.Web
```

Migrations and seed data (admin user, standard scopes, default tenant) are applied automatically on startup. Update the connection string in `appsettings.json` if you're not using LocalDB.

- **Home**: `https://localhost:54320`
- **Admin console**: `https://localhost:54320/Admin/Users`

Default credentials (change immediately in any non-local environment):
- **Email**: `admin@onyx.com`
- **Password**: `Admin123!`

## Related repositories

- [`Onyx.Oms`](https://github.com/gihan-aj/Onyx.Oms) — the order management backend that relies on this IdP for authentication and tenant context
- [`Onyx.Oms.Client`](https://github.com/gihan-aj/Onyx.Oms.Client) — the WinUI desktop client that bundles both this IdP and the Oms API into a single installable app

## License

MIT
