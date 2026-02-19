# Testing User Onboarding API with Postman

This guide details how to verify the User Onboarding and Role Management APIs using Postman.

## 1. Setup Environment

*   **Base URL**: `https://localhost:7001` (or your configured HTTPS port)
*   **Client Credentials**:
    *   **Client ID**: `order-system`
    *   **Client Secret**: `order-system-secret`
    *   **Scope**: `idp_roles_manage`

## 2. Authenticate (Get M2M Token)

Before calling any API, you must obtain an access token acting as the **Order Management System** backend.

*   **Request**: `POST /connect/token`
*   **Body** (`x-www-form-urlencoded`):
    *   `grant_type`: `client_credentials`
    *   `client_id`: `order-system`
    *   `client_secret`: `order-system-secret`
    *   `scope`: `idp_roles_manage`
*   **Test Script** (Optional - to auto-set variable):
    ```javascript
    var jsonData = pm.response.json();
    pm.environment.set("access_token", jsonData.access_token);
    ```

## 3. Role Management Testing

### A. Create a Role
Create a new role (e.g., "RefundClerk").

*   **Request**: `POST /api/roles`
*   **Auth**: Bearer Token
*   **Body** (`JSON`):
    ```json
    {
      "name": "RefundClerk",
      "targetClientId": "order-system"
    }
    ```
*   **Expected Result**: `200 OK`
    *   Created Role in DB: `order-system_RefundClerk`

### B. Deactivate Role
Deactivate the role you just created.

*   **Request**: `PUT /api/roles/RefundClerk/deactivate`
*   **Auth**: Bearer Token
*   **Body** (`JSON`):
    ```json
    {
      "targetClientId": "order-system"
    }
    ```
*   **Expected Result**: `200 OK`
    *   `IsActive` flag for `order-system_RefundClerk` is set to `false`.

### C. Activate Role
Re-activate the role.

*   **Request**: `PUT /api/roles/RefundClerk/activate`
*   **Auth**: Bearer Token
*   **Body** (`JSON`):
    ```json
    {
      "targetClientId": "order-system"
    }
    ```
*   **Expected Result**: `200 OK`
    *   `IsActive` flag is set to `true`.

## 4. User Onboarding Testing

### A. Invite User
Invite a user and assign them the role.

*   **Request**: `POST /api/users`
*   **Auth**: Bearer Token
*   **Body** (`JSON`):
    ```json
    {
      "email": "test.user@example.com",
      "firstName": "Test",
      "lastName": "User",
      "roleName": "RefundClerk",
      "targetClientId": "order-system"
    }
    ```
*   **Expected Result**: `200 OK`
    *   User created.
    *   Assigned to role `order-system_RefundClerk`.
    *   Email sent (check console logs if using a mock email sender).

### B. Verify Token Claims (Simulated)
To verify the role prefix stripping, you would strictly need to log in as the **User** (Flow: Authorization Code) using the **Frontend Client**.

However, if you check the database or `ConnectController` logs:
1.  User `test.user@example.com` has role `order-system_RefundClerk`.
2.  When `order-system` (Frontend) requests a token, `ConnectController` filters roles starting with `order-system_`.
3.  It finds `order-system_RefundClerk`.
4.  It strips the prefix.
5.  The token claim is `role: RefundClerk`.
