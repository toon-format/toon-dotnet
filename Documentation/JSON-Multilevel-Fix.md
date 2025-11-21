# JSON Multi-level Structure Support and Locale Fix

## Overview

This document describes the bug fix and test implementation for handling complex multi-level JSON structures in the TOON format encoder/decoder, specifically addressing a critical locale-dependent numeric encoding issue.

## Problem Statement

### Issue #1: Locale-Dependent Numeric Encoding

The TOON encoder was using culture-specific number formatting when encoding numeric values. This caused the following problems:

- **Systems with comma decimal separators** (e.g., Spanish, German, French locales) would encode `12500.75` as `12500,75`
- The TOON decoder expects period (`.`) as decimal separator per JSON spec
- Round-trip encoding/decoding would fail with `InvalidOperationException`

**Error Message:**

```text
System.InvalidOperationException : A value of type 'System.String' cannot be converted to a 'System.Double'.
```

### Issue #2: Lack of Complex Structure Testing

There were no comprehensive tests validating:

- Multi-level nested objects
- Arrays within objects
- Special characters preservation
- URL encoding/decoding
- DateTime serialization
- Mixed primitive and complex types

## Solution

### 1. Fixed Numeric Encoding (`src/ToonFormat/Internal/Encode/Primitives.cs`)

**Changed:** All numeric `ToString()` calls now use `CultureInfo.InvariantCulture`

**Before (Culture-dependent):**

```csharp
if (jsonValue.TryGetValue<int>(out var intVal))
    return intVal.ToString();

if (jsonValue.TryGetValue<double>(out var doubleVal))
    return doubleVal.ToString("G17");
```

**After (Culture-invariant):**

```csharp
using System.Globalization;

if (jsonValue.TryGetValue<int>(out var intVal))
    return intVal.ToString(CultureInfo.InvariantCulture);

if (jsonValue.TryGetValue<double>(out var doubleVal))
    return doubleVal.ToString("G17", CultureInfo.InvariantCulture);
```

**Impact:** Ensures consistent numeric encoding regardless of system locale settings.

### 2. Comprehensive Test Suite (`tests/ToonFormat.Tests/JsonComplexRoundTripTests.cs`)

Added 4 test cases covering:

1. **`ComplexJson_RoundTrip_ShouldPreserveKeyFields`**
   - Tests complete round-trip of multi-level JSON
   - Validates 15+ nested fields
   - Ensures data integrity through encode/decode cycle

2. **`ComplexJson_Encode_ShouldProduceValidToonFormat`**
   - Validates TOON format structure
   - Checks for proper array notation `phases[2]`
   - Verifies key-value pair formatting

3. **`ComplexJson_SpecialCharacters_ShouldBePreserved`**
   - Tests special character escaping: `!@#$%^&*()_+=-{}[]|:;<>,.?/`
   - Ensures proper string encoding/decoding

4. **`ComplexJson_DateTime_ShouldBePreservedAsString`**
   - Validates ISO 8601 DateTime serialization
   - Tests format: `2025-11-20T10:32:00Z`

## Test Data Structure

The test uses a realistic project management JSON structure:

```json
{
  "project": {
    "id": "PX-4921",
    "name": "Customer Insights Expansion",
    "metadata": {
      "owner": "john.doe@example.com",
      "website": "https://example.org/products/insights?ref=test&lang=en",
      "tags": ["analysis", "insights", "growth", "R&D"],
      "cost": {
        "currency": "USD",
        "amount": 12500.75
      }
    },
    "phases": [
      {
        "phaseId": 1,
        "title": "Discovery & Research",
        "details": {
          "notes": "Team is conducting interviews...",
          "specialChars": "!@#$%^&*()_+=-{}[]|:;<>,.?/"
        }
      },
      {
        "phaseId": 2,
        "title": "Development",
        "budget": {
          "currency": "EUR",
          "amount": 7800.00
        }
      }
    ]
  }
}
```

## Behavior Comparison

### Before Fix (Spanish Locale Example)

**Input JSON:**

```json
{
  "cost": {
    "amount": 12500.75
  }
}
```

**TOON Encoded (INCORRECT):**

```text
cost:
  amount: 12500,75
```

**Decode Attempt:**

```text
❌ FAILS: System.InvalidOperationException
Parser cannot convert "12500,75" to double
```

### After Fix (Any Locale)

**Input JSON:**

```json
{
  "cost": {
    "amount": 12500.75
  }
}
```

**TOON Encoded (CORRECT):**

```text
cost:
  amount: 12500.75
```

**Decode Result:**

```json
✅ SUCCESS: {
  "cost": {
    "amount": 12500.75
  }
}
```

## Affected Files

| File | Change Type | Description |
|------|-------------|-------------|
| `src/ToonFormat/Internal/Encode/Primitives.cs` | Bug Fix | Added `CultureInfo.InvariantCulture` to all numeric conversions |
| `tests/ToonFormat.Tests/JsonComplexRoundTripTests.cs` | New File | Comprehensive test suite for complex JSON structures |

## Test Results

**Before Fix:**

- 20 tests passing
- Fails on non-US locales

**After Fix:**

- 24 tests passing (4 new)
- Works on all locales
- Zero regressions

## Validation

To verify the fix works correctly:

```bash
# Run all tests
dotnet test

# Run only complex JSON tests
dotnet test --filter "FullyQualifiedName~JsonComplexRoundTripTests"

# Expected output
# Total: 24, Passed: 24, Failed: 0
```

## Technical Details

### Root Cause Analysis

The .NET `ToString()` method without culture specification uses `CultureInfo.CurrentCulture`, which varies by system locale:

| Locale | Culture | Decimal Separator | Result of `12500.75.ToString()` |
|--------|---------|-------------------|----------------------------------|
| en-US | English (US) | `.` (period) | `12500.75` ✅ |
| es-ES | Spanish (Spain) | `,` (comma) | `12500,75` ❌ |
| de-DE | German (Germany) | `,` (comma) | `12500,75` ❌ |
| fr-FR | French (France) | `,` (comma) | `12500,75` ❌ |

### Why InvariantCulture?

`CultureInfo.InvariantCulture` provides:

- Consistent behavior across all systems
- JSON-compliant number formatting (RFC 8259)
- Predictable serialization/deserialization
- No dependency on user locale settings

## Future Considerations

1. **Performance:** Consider caching `CultureInfo.InvariantCulture` in a static field
2. **Edge Cases:** Add tests for:
   - Very large numbers (scientific notation)
   - Negative zero handling
   - Precision limits (more than 17 digits)
3. **Internationalization:** Document that TOON format uses JSON number rules

## References

- [RFC 8259 - The JavaScript Object Notation (JSON) Data Interchange Format](https://tools.ietf.org/html/rfc8259)
- [.NET CultureInfo Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo)
- [TOON Format Specification](https://github.com/toon-format/spec)

---

**Date:** November 20, 2025  
**Branch:** `feature/test-fix-JSON-multilevel`  
**Status:** ✅ All tests passing
