# Development Guide

This guide outlines the development patterns and architectural decisions for the Onyx Identity Provider application.

## Architecture

We follow a **Vertical Slice Architecture** approach, organizing code by features rather than technical layers (e.g., Controllers, Views, Models).

### Folder Structure

Features are located in `src/Onyx.IdP.Web/Features`. Each feature folder contains all the necessary files for that feature:

- **Controller**: Handles the HTTP requests.
- **ViewModels**: Data transfer objects for the views.
- **Views**: Razor views (`.cshtml`).

Example structure for a `Profile` feature:
```
Features/
  ├── Profile/
  │   ├── ProfileController.cs
  │   ├── ProfileViewModel.cs
  │   ├── ChangePasswordViewModel.cs
  │   ├── Index.cshtml
  │   └── ChangePassword.cshtml
```

## Adding a New Feature

Follow these steps to add a new feature (e.g., "Orders"):

1.  **Create Feature Folder**: Create a new folder in `Features/` (e.g., `Features/Orders`).
2.  **Define ViewModels**: Create validation-ready models (e.g., `CreateOrderViewModel.cs`).
3.  **Create Controller**:
    - Inherit from `Controller`.
    - Apply `[Authorize]` if needed.
    - Inject dependencies (e.g., `DbContext`, `UserManager`).
    - Define Actions (`Index`, `Create`, etc.).
4.  **Create Views**:
    - Create `.cshtml` files matching Action names.
    - Use `_Layout` (default) or `_AuthLayout` (for login/register pages).
5.  **Register Services**: If your feature needs new services, register them in `Program.cs` or `DependencyInjection.cs`.

## Coding Standards

- **Dependency Injection**: Always use constructor injection.
- **Async/Await**: Use asynchronous patterns for all I/O operations (DB, API calls).
- **Validation**: Use Data Annotations in ViewModels and check `ModelState.IsValid` in Controllers.
- **Styling**: Refer to `docs/design_guide.md` for UI components.
