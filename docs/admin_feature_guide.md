# Admin Feature Development Guide

This guide outlines the standards and patterns for developing features within the Admin area of the Onyx Identity Provider. Follow these guidelines to ensure consistency, maintainability, and a unified user experience.

## 1. Feature Architecture

We follow a **Vertical Slice Architecture**. Each admin feature should be self-contained within `Features/Admin/[FeatureName]`.

### Structure
- `[FeatureName]Controller.cs`: Handles HTTP requests.
- `[FeatureName]ViewModel.cs`: Dedicated ViewModels for List, Create, Edit, Delete.
- `Index.cshtml`: The main list view.
- `Create.cshtml`, `Edit.cshtml`: Forms for creating and updating entities.
- `Delete.cshtml`: **Dedicated confirmation page** (avoid modals for complex actions).

## 2. Styling and CSS

All styles should be defined in `wwwroot/css/site.css`. **Do not use inline styles.**

### Key CSS Classes
- **Layout**:
    - `.admin-wrapper`, `.admin-sidebar`, `.admin-content-wrapper`: Main structure.
    - `.admin-header`, `.page-title`: Header area.
    - `.admin-main`: Main content area (padding).
- **Data Tables**:
    - `.data-table-container`: Card wrapper for tables.
    - `.table-controls`: Area for search bars and action buttons.
    - `.data-table`: Main table class.
    - `.badge`, `.badge-primary`: Status indicators.
- **Buttons**:
    - `.btn`, `.btn-primary`: Primary actions.
    - `.btn-ghost`: Secondary/Cancel actions.
    - `.btn-danger`: Destructive actions.
    - `.btn-sm`, `.btn-md`, `.btn-lg`: Sizing.
- **Utilities**:
    - `.d-flex`, `.gap-2`, `.justify-content-end`: Flexbox helpers.
    - `.text-danger`, `.text-muted`, `.text-sm`: Typography helpers.
    - `.form-group`, `.form-control`, `.form-check`: Form elements.

### Cross-Referencing Rule
Before finalizing a feature:
1.  Scan your `.cshtml` files for class names.
2.  Verify **every** class exists in `site.css`.
3.  If a class is missing, add it to `site.css` (do not add it to a local `<style>` block).

## 3. UI Patterns

### Listings (Index)
- Use `.data-table` for tabular data.
- Include **Search**, **Sort**, and **Pagination** whenever possible.
- Use `.table-controls` to house the Search form and "Create New" button.

### Forms (Create/Edit)
- Use `.auth-card` (centered) for simple forms to focus user attention.
- Use `asp-validation-summary` and `asp-validation-for` for clear error messaging.
- Always provide a "Cancel" button linking back to the Index.

### Deletion Workflow
**Pattern**: Dedicated Confirmation Page.
**Why**: Avoids complex JavaScript state management and allows for "smart" deletion logic (e.g., handling dependencies).

1.  **Link**: The "Delete" button in the Index goes to `[Controller]/Delete/{id}`.
2.  **View**: The `Delete.cshtml` view checks the entity state.
    - **Safe to Delete**: Show standard "Are you sure?" message and "Delete" button.
    - **Dependencies Exits**: Show a warning (e.g., "X users assigned"). Provide an option to resolve it (e.g., "Unassign and Delete").
3.  **Action**: The POST action handles the logic (unassigning dependencies, then deleting).

## 4. Verification Checklist

- [ ] Area is accessible via Admin Sidebar.
- [ ] Grid displays correct data columns.
- [ ] Pagination, Search, and Sort work.
- [ ] Create/Edit forms validate input correctly.
- [ ] Styles are compact and consistent with `site.css`.
- [ ] Delete flow handles dependencies gracefully (no 500 errors).
