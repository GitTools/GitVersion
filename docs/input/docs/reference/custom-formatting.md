---
title: Format Strings
description: Using C# format strings in GitVersion configuration
---

GitVersion supports C# format strings in configuration, allowing you to apply standard .NET formatting and custom transformations to version properties. This enhancement provides more flexibility and control over how version information is displayed and used throughout your build process.

## Overview

The custom formatter functionality introduces several new formatters that can be used in GitVersion configuration files and templates:

- **FormattableFormatter**: Supports standard .NET format strings for numeric values, dates, and implements `IFormattable`
- **NumericFormatter**: Handles numeric formatting with culture-aware output
- **DateTimeFormatter**: Provides date and time formatting with standard and custom format specifiers
- **String Case Formatters**: Provides text case transformations with custom format specifiers

## Standard .NET Format Strings

### Numeric Formatting

You can now use standard .NET numeric format strings with version components:

```yaml
# GitVersion.yml
assembly-informational-format: "{Major}.{Minor}.{Patch:F2}-{PreReleaseLabel}"
```

**Supported Numeric Formats:**

- `F` or `f` (Fixed-point): `{Patch:F2}` → `"1.23"`
- `N` or `n` (Number): `{BuildMetadata:N0}` → `"1,234"`
- `C` or `c` (Currency): `{Major:C}` → `"¤1.00"`
- `P` or `p` (Percent): `{VersionSourceDistance:P}` → `"12,345.60 %"`
- `D` or `d` (Decimal): `{Major:D4}` → `"0001"`
- `B` or `b` (Binary): `{Minor:B4}` → `"0101"`
- `X` or `x` (Hexadecimal): `{Patch:X}` → `"FF"`

### Date and Time Formatting

When working with date-related properties like `CommitDate`:

```yaml
assembly-informational-format: "Build-{SemVer}-{CommitDate:yyyy-MM-dd}"
```

**Common Date Format Specifiers:**

- `yyyy-MM-dd` → `"2024-03-15"`
- `HH:mm:ss` → `"14:30:22"`
- `MMM dd, yyyy` → `"Mar 15, 2024"`
- `yyyy-MM-dd'T'HH:mm:ss'Z'` → `"2024-03-15T14:30:22Z"`

## Custom String Case Formatters

GitVersion introduces custom format specifiers for string case transformations that can be used in templates:

### Available Case Formats

| Format | Description                                                   | Example Input    | Example Output   |
|--------|---------------------------------------------------------------|------------------|------------------|
| `u`    | **Uppercase** - Converts entire string to uppercase           | `feature-branch` | `FEATURE-BRANCH` |
| `l`    | **Lowercase** - Converts entire string to lowercase           | `Feature-Branch` | `feature-branch` |
| `t`    | **Title Case** - Capitalizes first letter of each word        | `feature-branch` | `Feature-Branch` |
| `s`    | **Sentence Case** - Capitalizes only the first letter         | `feature-branch` | `Feature-branch` |
| `c`    | **PascalCase** - Removes separators and capitalizes each word | `feature-branch` | `FeatureBranch`  |

### Usage Examples

```yaml
# GitVersion.yml configuration
branches:
  feature:
    label: "{BranchName:c}"  # Converts to PascalCase

assembly-informational-format: "{Major}.{Minor}.{Patch}-{PreReleaseLabel:l}.{VersionSourceDistance:0000}"
```

**Template Usage:**

```yaml
# Using format strings in templates
assembly-informational-format: "{Major}.{Minor}.{Patch}-{VersionSourceDistance:0000}"
assembly-informational-format: "{SemVer}-{BranchName:l}"
```

## Examples

Based on actual test cases from the implementation:

### Zero-Padded Numeric Formatting

```yaml
# Zero-padded commit count
assembly-informational-format: "{Major}.{Minor}.{Patch}-{VersionSourceDistance:0000}"
# Result: "1.2.3-0042"
```

### String Case Transformations

```yaml
branches:
  feature:
    label: "{BranchName:c}"  # PascalCase: "feature-branch" → "FeatureBranch"
  hotfix:
    label: "hotfix-{BranchName:l}"  # Lowercase: "HOTFIX-BRANCH" → "hotfix-branch"
```

### Date and Time Formatting

```yaml
assembly-informational-format: "{SemVer}-build-{CommitDate:yyyy-MM-dd}"
# Result: "1.2.3-build-2021-01-01"
```

### Numeric Formatting

```yaml
# Currency format (uses InvariantCulture)
assembly-informational-format: "Cost-{Major:C}"  # Result: "Cost-¤1.00"

# Percentage format
assembly-informational-format: "Progress-{Minor:P}"  # Result: "Progress-200.00 %"

# Thousands separator
assembly-informational-format: "Build-{VersionSourceDistance:N0}"  # Result: "Build-1,234"
```

## Configuration Integration

The format strings are used in GitVersion configuration files through various formatting properties:

### Assembly Version Formatting

```yaml
# GitVersion.yml
assembly-informational-format: "{Major}.{Minor}.{Patch}-{VersionSourceDistance:0000}"
assembly-versioning-format: "{Major}.{Minor}.{Patch}.{env:BUILD_NUMBER}"
assembly-file-versioning-format: "{MajorMinorPatch}.{VersionSourceDistance}"
```

### Environment Variable Integration

```yaml
# Using environment variables with fallbacks
assembly-informational-format: "{Major}.{Minor}.{Patch}-{env:RELEASE_STAGE ?? 'dev'}"
assembly-informational-format: "{SemVer}+{env:BUILD_ID ?? 'local'}"
```

### Real-World Integration Examples

Based on the actual test implementation:

```yaml
# Example from VariableProviderTests.cs
assembly-informational-format: "{Major}.{Minor}.{Patch}-{VersionSourceDistance:0000}"
# Result: "1.2.3-0042" when VersionSourceDistance = 42

# Branch-specific formatting
branches:
  feature:
    label: "{BranchName:c}"  # PascalCase conversion
  hotfix:
    label: "hotfix.{VersionSourceDistance:00}"
```

## Invariant Culture Formatting

The formatting system uses `CultureInfo.InvariantCulture` by default through the chained `TryFormat` overload implementation. This provides:

- **Consistent results** across all environments and systems
- **Predictable numeric formatting** with period (.) as decimal separator and comma (,) as thousands separator
- **Standard date formatting** using English month names and formats
- **No localization variations** regardless of system locale

```csharp
// All environments produce the same output:
// {VersionSourceDistance:N0} → "1,234"
// {CommitDate:MMM dd, yyyy} → "Mar 15, 2024"
// {Major:C} → "¤1.00" (generic currency symbol)
```

This ensures that version strings generated by GitVersion are consistent across different build environments, developer machines, and CI/CD systems.
