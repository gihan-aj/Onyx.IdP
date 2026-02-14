# OpenIddict Connect Controller Setup

The goal is to implement the Open ID Connect (OIDC) endpoints using OpenIddict in the Onyx.IdP application. This involves adding the `ConnectController`, the consent screen, and configuring the OpenIddict server services in `Program.cs`.

## Proposed Changes

### Web Layer (Onyx.IdP.Web)

#### [NEW] [ConnectController.cs](../src/Onyx.IdP.Web/Features/Connect/ConnectController.cs)
- Implements the OIDC endpoints: `Authorize`, `Exchange` (Token), `Userinfo`, `Logout`.
- Handles user consent logic.
- Uses `IOpenIddictApplicationManager`, `IOpenIddictAuthorizationManager`, and `SignInManager`.

#### [NEW] [ConsentViewModel.cs](../src/Onyx.IdP.Web/Features/Connect/ConsentViewModel.cs)
- View model for the Consent screen.
- Contains application name, scopes, descriptions, and return URL.

#### [NEW] [Consent.cshtml](../src/Onyx.IdP.Web/Features/Connect/Consent.cshtml)
- Razor view to display the consent form.
- Allows users to Accept or Deny the authorization request.

#### [MODIFY] [Program.cs](../src/Onyx.IdP.Web/Program.cs)
- **Add Quartz Configuration**: Configure Quartz for background jobs (token pruning) as it's already installed.
- **Configure OpenIddict**:
    - Enable Quartz integration.
    - Set Authorization, Token, UserInfo, Logout endpoints.
    - Enable Authorization Code, Refresh Token, Client Credentials flows.
    - Register scopes (Email, Profile, Roles, OfflineAccess).
    - **Development Security**: Use development certificates (as per current state).

### Infrastructure Layer (Onyx.IdP.Infrastructure)

#### [MODIFY] [DataSeeder.cs](../src/Onyx.IdP.Infrastructure/Data/DataSeeder.cs)
- Enhance the `DataSeeder` to use the correct OpenIddict permissions for OIDC flows (adding Email, Profile, Roles, Logout, Code Response Type).
