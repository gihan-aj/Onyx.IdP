# Design Guide

This guide outlines the styling guidelines for the Onyx Identity Provider application to ensure consistency across all pages.

## Core Variables

We use CSS variables for theming. These are defined in `:root`.

- **Colors**:
    - `--primary-color`: Main action color (e.g., buttons, links).
    - `--background-color`: Page background.
    - `--surface-color`: Card and container background.
    - `--text-color`: Primary text color.
    - `--text-muted`: Secondary text color.
    - `--border-color`: Border color for inputs and cards.

## Typography

- **Font Family**: Inter, system-ui, sans-serif.
- **Headings**: Use `<h1>` to `<h6>` with appropriate hierarchy.
- **Body Text**: Default size is 1rem (16px).

## Components

### Forms

Use the following classes for form elements:

- `.form-group`: Wrapper for label and input pairs. Adds bottom margin.
- `.form-label`: Label for inputs. Displayed as block with 500 font weight.
- `.form-control`: Styling for textual input fields (text, email, password).
    - Width: 100%
    - Padding: 0.75rem
    - Rounded corners
- `.form-header`: Flex container for labels and "Forgot Password" links.
- `.btn`: Base class for buttons.
- `.btn-primary`: Primary action button style.

### Grid System

We use a simplified custom grid system:

- `.row`: Flex container with negative margins.
- `.col-md-6`: 50% width column on medium screens (768px+) and full width on smaller screens.

### Utilities

- `.w-100`: Width 100%.
- `.mb-3`: Margin bottom 1rem.
- `.mt-5`: Margin top 3rem.
- `.text-danger`: Red text for errors.
- `.text-center`: Center-aligned text.
- `.justify-content-center`: Center flex items.

### Alerts (Status Messages)

Use alerts to display success or error messages to the user.

- `.alert`: Base class for alerts. Adds padding and border radius.
- `.alert-success`: Green background/text for success messages.
- `.alert-danger`: Red background/text for error messages.

### Authentication Views

- `.auth-container`: Centered flex container for auth pages (used in `_AuthLayout`).
- `.auth-wrapper`: Wrapper for auth content within the main layout (e.g., Profile page).
- `.auth-card`: White/Surface colored card with shadow.
- `.auth-header`: Center-aligned title and subtitle area.
- `.auth-actions`: Container for secondary actions (links) below the main form.
- `.auth-link`: Styled link for auth actions.
- `.auth-footer`: Bottom section for secondary actions (e.g., "Sign up").

## Usage Example

```html
<div class="form-group">
    <label for="email" class="form-label">Email Address</label>
    <input type="email" id="email" class="form-control" />
</div>
```

## Development Guidelines

### 1. Unified Styling Source
- ALL custom styles must be defined in `wwwroot/css/site.css`.
- **Do not** use inline styles (`style="..."`) or `<style>` blocks within Views unless absolutely necessary for dynamic values.

### 2. Reuse Existing Utilities
- Before creating a new class, check `site.css` for existing utility classes.
- Common utilities available:
    - Margins: `.mt-4`, `.mt-5`, `.mb-3`, `.mb-4`, `.mb-5`
    - Typography: `.text-center`, `.text-muted`, `.text-danger`, `.display-1`, `.lead`, `.small`, `.font-weight-bold`
    - Navigation: `.d-inline`, `.me-3`

### 3. Creating New Styles
- If no existing utility fits, create a new semantic class in `site.css`.
- **Naming Convention**: Use **kebab-case** (e.g., `.error-container`, `.feature-card`).
- **Grouping**: Group related styles together in `site.css` (e.g., all "Error Pages" styles together).

### 4. Component-Based Approach
- Styles should be reusable components where possible.
- Example: Instead of styling a specific `div` on the error page, create a `.error-container` component that can be reused on any error-like page (as done for `Error.cshtml` and `NotFound.cshtml`).

