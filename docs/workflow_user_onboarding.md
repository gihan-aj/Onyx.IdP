# Workflow: Role Creation & User Onboarding

**Context:** This document outlines the workflow for an **Authorized User (Admin)** in the Client System (Order Management) to create new roles, define permissions, and onboard new users via the central Identity Provider (IdP).

**Actors:**
* **Admin:** Sarah (Lead Manager) - *Already has permission to manage users.*
* **New Role:** "Refund Clerk" - *Can view/refund orders, but cannot delete.*
* **New User:** John - *New employee needing access.*

---

## Phase 1: Role Creation (The Definition)

**Goal:** Define what a "Refund Clerk" can do and ensure the IdP knows this role exists.

### 1. User Actions (Frontend - Order System)
1.  Sarah logs into the **Order Management System**.
2.  Navigates to **Settings > Roles & Permissions**.
3.  Clicks **"Create New Role"**.
4.  Enters Role Name: **"Refund Clerk"**.
5.  Selects granular permissions from the system's checkbox list:
    * [x] `Order.View`
    * [x] `Order.Refund`
    * [ ] `Order.Delete`
    * [ ] `User.Manage`
6.  Clicks **Save**.

### 2. System Actions (Backend Magic)
1.  **Local Save:** The Order System backend saves the definition in its own database.
    * *Table `LocalRoles`:* `{ Name: "Refund Clerk", Permissions: ["Order.View", "Order.Refund"] }`
2.  **Remote Sync (API Call):** The Order System backend immediately calls the IdP API to register the role name.
    * *Request:* `POST https://idp.onyx.com/api/roles`
    * *Payload:* `{ name: "OrderSystem_Refund Clerk" }`
    * *Auth:* Uses a **Machine-to-Machine Token** (Client Credentials) with `idp.roles.manage` scope.
3.  **Result:** The IdP creates the role `OrderSystem_Refund Clerk` in the global identity database. *Note: The IdP does not know or care about the specific permissions.*

---

## Phase 2: User Onboarding (The Assignment)

**Goal:** Create an account for John and grant him the "Refund Clerk" role.

### 1. User Actions (Frontend - Order System)
1.  Sarah navigates to the **Users** screen in the Order Management System.
2.  Clicks **"Invite User"**.
3.  Enters John's email: `john@company.com`.
4.  Selects Role from dropdown: **"Refund Clerk"**.
5.  Clicks **Send Invite**.

### 2. System Actions (Backend Magic)
1.  **Identity Check:** The Order System calls the IdP API to check if the user exists.
    * *Request:* `GET https://idp.onyx.com/api/users?email=john@company.com`
2.  **Create/Link User:**
    * *If New:* The IdP creates a placeholder account for John and triggers a "Welcome/Set Password" email.
    * *If Exists:* The IdP returns the existing `UserId`.
3.  **Assign Role:** The Order System commands the IdP to assign the role.
    * *Request:* `POST https://idp.onyx.com/api/users/{john_id}/roles`
    * *Payload:* `{ roleName: "OrderSystem_Refund Clerk" }`
4.  **Local Mapping (Optional):** The Order System may store a reference (`John_Id` -> `Refund Clerk`) locally for UI display purposes, but the **Source of Truth** for the role assignment is the IdP.

---

## Phase 3: Login & Authorization (The Result)

**Goal:** John logs in and the system enforces the limited permissions.

1.  **Login:** John logs in via the IdP.
2.  **Token Issuance:** The IdP issues an Access Token containing:
    * `sub: "john_id"`
    * `role: "OrderSystem_Refund Clerk"`
3.  **Access Attempt:** John tries to click the **"Refund Order #123"** button.
4.  **Authorization Check:**
    * The Order System API inspects the token: *"User has role 'OrderSystem_Refund Clerk'"*.
    * It queries its **Local Database**: *"What permissions does 'Refund Clerk' have?"*
    * *Result:* `["Order.View", "Order.Refund"]`.
5.  **Decision:** The endpoint requires `Order.Refund`. The user has it. **Access Granted.**

---

## Summary of Responsibilities

| Feature | Order System (Client) | Identity Provider (IdP) |
| :--- | :--- | :--- |
| **User Interface** | Provides the "Create Role" & "Invite User" UI. | Provides the "Login" & "Consent" screens only. |
| **Permissions Logic** | **Owner.** Defines what "Refund" actually means (`Order.Refund`). | **Ignorant.** Doesn't know what permissions exist. |
| **Role Names** | **Creator.** Defines the business role names. | **Store.** Stores the official role name (often prefixed). |
| **User Accounts** | **Proxy.** Acts as a window to manage users. | **Owner.** Holds the actual credentials (password hashes). |

---

## Security & Architecture Note

To make this workflow secure, we strictly follow the **Trusted Backend Pattern**:

1.  **No Direct Frontend Access:** The WinUI Client (running on Sarah's PC) **never** talks directly to the IdP's Admin API. It talks to the *Order System Backend*.
2.  **Privileged Communication:** The *Order System Backend* talks to the IdP using a special **Client Credentials Flow** token.
    * This token has a high-privilege scope: `idp.roles.manage`.
    * The IdP trusts this token because it was issued to the **Order System Backend** (which has a kept secret), not to Sarah (a human user).
3.  **Isolation:** This ensures that a random user or a compromised frontend client cannot flood the IdP with fake roles or delete users. Only the trusted API can command the IdP to change state.