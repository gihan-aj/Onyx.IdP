# Onyx Identity Provider - API Reference

This API allows trusted client applications (like the Order Management System) to manage roles and onboard users programmatically.

## Authentication

All API endpoints require an OAuth 2.0 Access Token obtained via the **Client Credentials Flow**.

### Get Access Token

**Endpoint:** `POST /connect/token`

**Headers:**
- `Content-Type: application/x-www-form-urlencoded`

**Form Body:**
| Parameter | Value | Description |
| :--- | :--- | :--- |
| `grant_type` | `client_credentials` | Required. |
| `client_id` | `onyx-oms` | The Backend Client ID (M2M). |
| `client_secret` | `[YOUR_SECRET]` | The Backend Client Secret. |
| `scope` | `idp_roles_manage` | Required scope for these APIs. |

**Response:**
```json
{
  "access_token": "eyJhbGciOi...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "idp_roles_manage"
}
```

---

## Role Management

### Create Role
Defines a new role within the Identity Provider.
> **Note:** The role name will be automatically prefixed with your Client ID. 
> To create a role for a *different* client (e.g., the Frontend App), provide `targetClientId`.

**Endpoint:** `POST /api/roles`

**Request Body:**
```json
{
  "name": "RefundClerk",
  "targetClientId": "order-system" 
}
```
*Creates: `order-system_RefundClerk`*

**Response:** `200 OK`
```json
{
  "message": "Role 'RefundClerk' created successfully."
}
```

---

## User Management

### Get User
Checks if a user already exists in the system by email.

**Endpoint:** `GET /api/users`

**Query Parameters:**
- `email`: The email address to search for.

**Response:** `200 OK`
```json
{
  "id": "guid-user-id",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "isActive": true
}
```
*Returns `404 Not Found` if the user does not exist.*

### Invite User
Creates a new user account, sends a welcome email with a password setup link, and assigns an initial role.

**Endpoint:** `POST /api/users`

**Request Body:**
```json
{
  "email": "john@company.com",
  "roleNames": [
    "RefundClerk"
  ],
  "firstName": "John",
  "lastName": "Doe",
  "targetClientId": "order-system"
}
```
*Note: `roleNames` should contain the short names (e.g., "RefundClerk"). The system will automatically look for `order-system_RefundClerk`.*

**Response:** `200 OK`
```json
{
  "id": "new-guid-user-id",
  "email": "john@company.com",
  "firstName": "John",
  "lastName": "Doe",
  "isActive": true
}
```

### Assign Roles
Assigns additional roles to an existing user.

**Endpoint:** `POST /api/users/{userId}/roles`

**Path Parameters:**
- `userId`: The GUID of the user.

**Request Body:**
```json
{
  "roleNames": [
    "InventoryManager"
  ],
  "targetClientId": "order-system"
}
```

**Response:** `200 OK`
```json
{
  "message": "Roles assigned successfully.",
  "assignedRoles": [
    "InventoryManager"
  ]
}
```
