# Identity Provider Implementation Plan

The goal is to implement an Identity Provider (IdP) using OpenIddict and ASP.NET Core Identity. This IdP will manage users, roles, and permissions, and issue tokens for other applications. We will follow a layered architecture with separate Core and Infrastructure projects.

## User Review Required

> [!IMPORTANT]
> We will be using SQL Server for the database.
> Connection String: `Server=gihan-aj-dsktp;Database=OnyxIdP;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`

## Required Packages (User Confirmed Installed)

### Onyx.IdP.Core
- `Microsoft.Extensions.Identity.Stores`

### Onyx.IdP.Infrastructure
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `OpenIddict.EntityFrameworkCore`

### Onyx.IdP.Web
- `Microsoft.EntityFrameworkCore.Tools`
- `OpenIddict.AspNetCore`
- `OpenIddict.Quartz` (Recommended for token cleanup)
- `Quartz.Extensions.Hosting`

## Proposed Changes

### Core Layer (Onyx.IdP.Core)

#### [NEW] [ApplicationUser.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Core/Entities/ApplicationUser.cs)
- Create a custom user entity inheriting from `IdentityUser`.
- Add properties: `FirstName`, `LastName`.

#### [NEW] [ApplicationRole.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Core/Entities/ApplicationRole.cs)
- Create a custom role entity inheriting from `IdentityRole`.
- Add property: `Description`.

#### [NEW] [IEmailSender.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Core/Interfaces/IEmailSender.cs)
- Define email sender interface.

#### [NEW] [DependencyInjection.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Core/DependencyInjection.cs)
- Extension method to register Core services.

### Infrastructure Layer (Onyx.IdP.Infrastructure)

#### [NEW] [ApplicationDbContext.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Infrastructure/Data/ApplicationDbContext.cs)
- Create the database context inheriting from `IdentityDbContext`.
- Configure OpenIddict entities.

#### [NEW] [EmailSender.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Infrastructure/Services/EmailSender.cs)
- Implement `IEmailSender` (log to console for dev).

#### [NEW] [DataSeeder.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Infrastructure/Data/DataSeeder.cs)
- Seed default roles (Admin, User).
- Seed default admin user.
- Seed OpenIddict client applications:
    - **Postman**: ClientId=`postman`, ClientSecret=`postman-secret`, AllowedGrantTypes=`client_credentials`, `authorization_code`, `refresh_token`.

#### [NEW] [DependencyInjection.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Infrastructure/DependencyInjection.cs)
- Extension method to register Infrastructure services (DbContext, Identity, OpenIddict).

### Web Layer (Onyx.IdP.Web)

#### [MODIFY] [Program.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Program.cs)
- Call `AddCoreServices` and `AddInfrastructureServices`.
- Configure the HTTP request pipeline.
- Run data seeding on startup.

### Registration Feature

#### [NEW] [Register.cshtml](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Features/Auth/Register.cshtml)
- Create the registration view using MVC patterns.

#### [NEW] [RegisterController.cs](file:///c:/Users/gihan/source/repos/Onyx.IdP/src/Onyx.IdP.Web/Features/Auth/RegisterController.cs)
- Handle user registration logic.
- Assign default role to new users.
- Generate email confirmation token.

## Verification Plan

### Automated Tests
- Integration tests for registration flow.

### Manual Verification
- Run the application and navigate to the registration page.
- Register a new user with First Name and Last Name.
- Verify user creation in SQL Server database.
- Check console logs for email confirmation.
- (Later) Use Postman to test OpenIddict endpoints using the seeded client.
