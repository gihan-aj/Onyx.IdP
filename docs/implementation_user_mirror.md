# Implementation: User Mirroring & Discovery

**Context:** This document details the "Local Mirror" pattern. The Client Application (Order System) maintains a local cache of users for performance and foreign key integrity, while the Identity Provider (IdP) remains the source of truth for authentication.

---

## 1. Database Schema (Client Side)

The Client App needs a local table to store user details for dropdowns, search, and foreign keys (e.g., `Order.CreatedBy`).

**SQL (OrderSystem Database):**
```sql
CREATE TABLE AppUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY, -- Local ID (Use this for FKs)
    IdentityUserId NVARCHAR(450) NOT NULL UNIQUE, -- The GUID from IdP (The Link)
    Email NVARCHAR(256) NOT NULL,
    DisplayName NVARCHAR(256) NOT NULL,
    IsActive BIT DEFAULT 1,
    LastLoginUtc DATETIME2,
    
    INDEX IX_AppUsers_IdentityUserId (IdentityUserId),
    INDEX IX_AppUsers_Email (Email)
);
```

* **Why Local ID?** Using an `INT` for foreign keys in your `Orders` table saves massive space compared to storing a `GUID` (36 chars) in every single order row.
* **Sync Strategy:** This table is **Read-Only** regarding identity data (Email/Name). Updates only happen via the IdP sync logic.

---

## 2. IdP API Requirements (Server Side)

The IdP must expose endpoints to allow trusted Client Apps to find users.

### Endpoint A: User Search (Directory)
* **Route:** `GET /api/directory/search`
* **Access:** Protected (Machine-to-Machine Token only).
* **Parameters:** `query` (string - matches email or name).
* **Response:**
  ```json
  [
    {
      "id": "guid-123",
      "email": "sarah@onyx.com",
      "displayName": "Sarah Jenkins",
      "department": "Sales"
    }
  ]
  ```

### Endpoint B: Bulk Sync (Optional)
* **Route:** `POST /api/directory/batch`
* **Purpose:** If the Client App misses a webhook or needs to refresh its cache.
* **Payload:** `["guid-123", "guid-456"]`
* **Response:** Returns current details for these IDs.

---

## 3. Client App Logic (The "Mirroring")

### A. Just-In-Time (JIT) Provisioning (Middleware)
This is the **most critical** component. It ensures that any user who logs in is immediately present in the local database.

**Implementation (C# Middleware):**
```csharp
public async Task InvokeAsync(HttpContext context, OrderDbContext db)
{
    if (context.User.Identity.IsAuthenticated)
    {
        var sub = context.User.FindFirst("sub")?.Value;
        
        // 1. Check Cache (Memory) first to avoid DB hits on every request
        if (!_memoryCache.TryGetValue($"User_{sub}", out _))
        {
            // 2. Check Database
            var user = await db.AppUsers.FirstOrDefaultAsync(u => u.IdentityUserId == sub);
            
            if (user == null)
            {
                // 3. CREATE: User is new to this app
                user = new AppUser 
                {
                    IdentityUserId = sub,
                    Email = context.User.FindFirst("email")?.Value,
                    DisplayName = context.User.FindFirst("name")?.Value,
                    LastLoginUtc = DateTime.UtcNow
                };
                db.AppUsers.Add(user);
            }
            else
            {
                // 4. UPDATE: Refresh details (in case they changed name in IdP)
                user.LastLoginUtc = DateTime.UtcNow;
                user.Email = context.User.FindFirst("email")?.Value; // Keep in sync
            }
            
            await db.SaveChangesAsync();
            
            // 5. Set Cache (e.g., for 10 minutes)
            _memoryCache.Set($"User_{sub}", true, TimeSpan.FromMinutes(10));
        }
    }
    
    await _next(context);
}
```

### B. The "Add User" Workflow (Controller)

When an Admin adds a user to a Role, the system must resolve the user first.

**Logic Flow:**
1.  **Search Local:** Query `AppUsers` table.
    * *Found?* Return result.
2.  **Search Remote (IdP):** If not found locally, call `IdP /api/directory/search`.
    * *Found?* Display "User exists in directory (Not in App yet)".
3.  **Action (On Selection):**
    * If the Admin selects a "Remote" user, the Client App **immediately creates** the `AppUser` record locally (mirroring them), *then* assigns the Role.

---

## 4. UI Implications

### The User Dropdown
* **Source:** Always bind your dropdowns to the `AppUsers` table.
* **Performance:** This ensures your UI loads instantly (SQL query) rather than waiting for an HTTP call to the IdP.

### The "Invite" Modal
* **Input:** Text Box (Email) + Search Button.
* **Behavior:**
    1.  Checks `AppUsers` (Local).
    2.  If empty, calls IdP API (Remote).
    3.  If empty, offers "Send Invitation Email" (creates new account in IdP).

---

## 5. Security Checklist

* [ ] **IdP API Protection:** Ensure `/api/directory/*` endpoints require a specific scope (e.g., `urn:onyx:directory.read`) that is **only** granted to the Backend Client, not the Frontend User.
* [ ] **Data Minimization:** The `AppUsers` table should **not** store passwords, hashes, or sensitive PII. Only store what is needed for display (Name, Email, Id).
* [ ] **Cache Invalidation:** If a user updates their profile in the IdP, the change propagates to the Client App upon their next login (via the JIT Middleware).