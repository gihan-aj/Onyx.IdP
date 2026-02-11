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

### Authentication Views

- `.auth-container`: Centered flex container for auth pages.
- `.auth-card`: White/Surface colored card with shadow.
- `.auth-header`: Center-aligned title and subtitle area.
- `.auth-footer`: Bottom section for secondary actions (e.g., "Sign up").

## Usage Example

```html
<div class="form-group">
    <label for="email" class="form-label">Email Address</label>
    <input type="email" id="email" class="form-control" />
</div>
```
