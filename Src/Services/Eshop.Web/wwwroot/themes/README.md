# Theme System Documentation

## Overview
The application now uses a modular theme system that allows easy switching between different color schemes while maintaining consistent base styles.

## Theme Structure

```
wwwroot/
├── app.css (main entry point - imports base + theme)
└── themes/
    ├── base.css (common styles shared by all themes)
    ├── theme-brown.css (Warm Brown Theme - Matjar Albarae style)
    └── theme-green.css (Islamic Green Theme - Original)
```

## Available Themes

### 1. **Warm Brown Theme** (Default - Matjar Albarae Style)
- **Primary Color**: Warm Brown (#8B7355)
- **Secondary Color**: Charcoal (#2C3539)
- **Accents**: Warm Gold & Beige tones
- **Style**: Modern, elegant, Instagram-inspired e-commerce
- **Best For**: Contemporary fashion stores, luxury boutiques

### 2. **Islamic Green Theme** (Original)
- **Primary Color**: Forest Green (#2c5f2d)
- **Secondary Color**: Gold (#c9a961)
- **Accents**: Rose and Gold tones
- **Style**: Traditional Islamic aesthetic with modern elements
- **Best For**: Modest fashion, traditional clothing stores

## How to Switch Themes

### Method 1: Update app.css (Recommended)
Edit `wwwroot/app.css` and change the import statement:

```css
/* For Brown Theme (Current Default) */
@import url('themes/theme-brown.css');

/* For Green Theme */
@import url('themes/theme-green.css');
```

### Method 2: Update App.razor
Edit `Components/App.razor` and change the stylesheet reference:

```html
<!-- For Brown Theme -->
<link rel="stylesheet" href="@Assets["themes/theme-brown.css"]" />

<!-- For Green Theme -->
<link rel="stylesheet" href="@Assets["themes/theme-green.css"]" />
```

## Creating a New Theme

To create a new theme:

1. Create a new CSS file in `wwwroot/themes/` (e.g., `theme-blue.css`)
2. Copy the structure from an existing theme file
3. Update the CSS custom properties (`:root` variables):
   - `--primary-color`
   - `--primary-hover`
   - `--secondary-color`
   - `--accent-*` colors
4. Override any specific styles that need theme-specific adjustments
5. Import the new theme in `app.css`

### Example New Theme Structure:

```css
/* Root Variables - Your Theme Name */
:root {
    --primary-color: #YourColor;
  --primary-hover: #YourHoverColor;
    /* ... other variables ... */
}

/* Theme-specific overrides */
.hero-section {
    background: linear-gradient(/* your gradient */);
}

.footer {
    background: /* your background */;
}
```

## CSS Custom Properties (Variables)

All themes use the following CSS variables defined in `:root`:

### Colors
- `--primary-color` - Main brand color
- `--primary-hover` - Hover state for primary elements
- `--primary-light` - Lighter version of primary
- `--secondary-color` - Secondary brand color
- `--accent-warm` - Warm accent color
- `--accent-gold` - Gold accent color

### Text Colors
- `--text-dark` - Main text color
- `--text-medium` - Secondary text color
- `--text-light` - Tertiary text color

### Background Colors
- `--bg-light` - Light background
- `--bg-cream` - Cream background
- `--bg-white` - White background
- `--border-color` - Border color

### Status Colors
- `--success-color` - Success messages
- `--danger-color` - Error messages
- `--warning-color` - Warning messages
- `--info-color` - Info messages

### Shadows
- `--shadow-sm` - Small shadow
- `--shadow` - Normal shadow
- `--shadow-lg` - Large shadow
- `--shadow-hover` - Hover shadow effect

## Benefits of This Approach

1. **Easy Theme Switching**: Change one line to switch entire color scheme
2. **Maintainable**: Base styles are separate from theme-specific styles
3. **Consistent**: All themes share the same layout and spacing
4. **Extensible**: Easy to add new themes without touching base styles
5. **Performance**: Only one theme is loaded at a time
6. **DRY Principle**: No code duplication between themes

## Troubleshooting

### Theme not applying?
1. Clear browser cache (Ctrl+F5)
2. Check that the CSS import path is correct
3. Ensure the theme file exists in `wwwroot/themes/`
4. Check browser console for CSS loading errors

### Colors not changing?
1. Verify that CSS custom properties are defined in `:root`
2. Check that the theme file is loaded after base.css
3. Use browser DevTools to inspect element and check applied styles

## Future Enhancements

Consider adding:
- **Theme Switcher UI**: Allow users to change themes dynamically
- **Dark Mode**: Create dark theme variants
- **User Preferences**: Save theme choice in localStorage
- **Theme API**: Load themes from a configuration file
- **RTL/LTR Support**: Separate theme variants for different text directions

## Notes

- Always test theme changes across different browsers
- Ensure sufficient color contrast for accessibility (WCAG guidelines)
- Use CSS variables consistently for easy theme customization
- Keep the base.css file theme-neutral

## Support

For issues or questions about the theme system, please refer to:
- CSS Custom Properties Documentation: https://developer.mozilla.org/en-US/docs/Web/CSS/--*
- Color Accessibility: https://webaim.org/resources/contrastchecker/
