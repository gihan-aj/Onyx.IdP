# Machine-to-Machine (M2M) Implementation Details

This document explains how the **Onyx OMS Backend** communicates with the **Identity Provider (IdP)** to manage roles and users.

> **Context**: The OMS Backend acts as a **Confidential Client** to the IdP. It authenticates itself (not on behalf of a user) to perform privileged administrative actions.

## 1. Authentication Flow (Client Credentials)

We use the **OAuth 2.0 Client Credentials Flow**.

### Configuration
The backend is configured with its own credentials in `appsettings.json`:
```json
"Authentication": {
  "Authority": "https://localhost:5001", // IdP URL
  "ClientId": "onyx-oms",
  "ClientSecret": "[YOUR_SECRET]",
  "Scope": "idp_roles_manage"
}
```

### Process (`IdPTokenHandler.cs`)
1.  **Discovery**: on the first request, the backend calls `GET {Authority}/.well-known/openid-configuration` to find the `token_endpoint`.
2.  **Token Request**: It sends a `POST` request to the token endpoint:
    ```http
    POST /connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials
    &client_id=onyx-oms
    &client_secret=[YOUR_SECRET]
    &scope=idp_roles_manage
    ```
3.  **Caching**: The resulting Access Token is cached in memory for its lifetime (minus 60s buffer).
4.  **Authorization Header**: Every API call to the IdP includes:
    `Authorization: Bearer <access_token>`

## 2. API Contract (`IIdentityProviderApi.cs`)

The backend expects the IdP to expose the following endpoints. 

### A. Create Role
Used when an Admin creates a new role in the OMS.

-   **Method**: `POST`
-   **URL**: `/api/roles`
-   **Headers**: `Authorization: Bearer <token>`
-   **Body**:
    ```json
    {
      "name": "RefundClerk"
    }
    ```
-   **Expected Response**: `200 OK` or `201 Created`. Error if `401 Unauthorized` or `403 Forbidden`.

### B. Invite User (Create)
Used when inviting a new user.

-   **Method**: `POST`
-   **URL**: `/api/users`
-   **Body**:
    ```json
    {
      "email": "john@company.com",
      "roleName": "RefundClerk",
      "firstName": "John",
      "lastName": "Doe"
    }
    ```
-   **Expected Response**: `200 OK` (returning user ID).

### C. Assign Role
Used to add a role to an existing user.

-   **Method**: `POST`
-   **URL**: `/api/users/{userId}/roles`
-   **Body**:
    ```json
    {
      "roleName": "InventoryManager"
    }
    ```

## 3. Troubleshooting Checklist for IdP

If the API calls are failing or behaving unexpectedly, check these on the **IdP side**:

1.  **Client Configuration**: Does a client with ID `onyx-oms` exist?
2.  **Allowed Scopes**: Is the client allowed to request the `idp_roles_manage` scope?
3.  **API Resource**: Is the API receiving the request configured to require the `idp_roles_manage` scope?
4.  **Token Validation**:
    -   Is the API validating the token signature correctly?
    -   Is the `Audience` correct? (The token issued to `onyx-oms` might have an audience of the *API Resource*, not `onyx-oms`).
5.  **Payload Handling**: Is the controller action binding the JSON body correctly (e.g., `[FromBody]`)?

## 4. Code References

-   **Token Logic**: `src/Onyx.Oms.Infrastructure/Identity/IdP/IdPTokenHandler.cs`
-   **API Definition**: `src/Onyx.Oms.Infrastructure/Identity/IdP/IIdentityProviderApi.cs`
