# Frontend Testing Guide

**Last Updated:** December 14, 2025

---

## Table of Contents
1. [Test Structure](#test-structure)
2. [Running Tests](#running-tests)
3. [Writing Tests](#writing-tests)
4. [Best Practices](#best-practices)

---

## Test Structure

The frontend testing follows the same pattern as the backend, with separate folders for unit and integration tests **outside** the main source code. This ensures tests are easily excluded from production builds.

### Directory Structure

```
frontend/
├── src/                          # Main application code (production)
│   ├── lib/
│   ├── routes/
│   └── ...
├── tests/                        # Test code (excluded from production)
│   ├── unit/                     # Unit tests (fast, isolated)
│   │   ├── components/           # Component tests
│   │   │   └── TicketStatusBadge.test.ts
│   │   ├── services/             # Service/utility tests
│   │   │   └── Lookups.test.ts
│   │   └── helpers/              # Test helper utilities
│   └── integration/              # Integration tests (slower, E2E scenarios)
│       ├── workflows/            # User workflow tests
│       └── helpers/              # Integration test helpers
├── vitest.config.ts              # Vitest configuration
└── vitest.setup.ts               # Global test setup & mocks
```

### Why This Structure?

✅ **Clean Separation:** Tests are outside `src/`, making it easy to exclude from builds  
✅ **Consistent with Backend:** Matches `TicketingSystem.Api.Tests` and `TicketingSystem.Api.IntegrationTests` structure  
✅ **Scalable:** Clear organization as test suite grows  
✅ **No Build Overhead:** Tests don't increase production bundle size  

---

## Running Tests

### Available Commands

```bash
# Run all tests (unit + integration)
pnpm test

# Run tests in watch mode (auto-rerun on file changes)
pnpm test:watch

# Run tests with UI (interactive browser interface)
pnpm test:ui

# Run tests with coverage report
pnpm test:coverage

# Run only unit tests
pnpm test:unit

# Run only integration tests
pnpm test:integration
```

### Test Execution Flow

1. **Vitest** discovers tests matching pattern: `tests/**/*.{test,spec}.{js,ts}`
2. **Setup** runs `vitest.setup.ts` (mocks SvelteKit modules, i18n)
3. **Environment** creates jsdom browser simulation
4. **Tests** execute in parallel by default
5. **Results** display in terminal with pass/fail status

---

## Writing Tests

### Unit Tests

**Location:** `tests/unit/{category}/{ComponentOrService}.test.ts`

**Example: Component Test**

```typescript
// tests/unit/components/Button.test.ts
import { render, fireEvent } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import Button from '$lib/components/ui/Button.svelte';

describe('Button', () => {
  it('renders with text', () => {
    const { getByText } = render(Button, { 
      props: { children: 'Click Me' } 
    });
    expect(getByText('Click Me')).toBeInTheDocument();
  });

  it('calls onclick handler when clicked', async () => {
    let clicked = false;
    const { getByRole } = render(Button, {
      props: { 
        onclick: () => { clicked = true; }
      }
    });
    
    const button = getByRole('button');
    await fireEvent.click(button);
    expect(clicked).toBe(true);
  });
});
```

**Example: Service Test**

```typescript
// tests/unit/services/TicketService.test.ts
import { describe, it, expect, vi } from 'vitest';
import { fetchTickets } from '$lib/services/Tickets';

describe('TicketService', () => {
  it('fetches tickets with correct query params', async () => {
    const mockFetch = vi.fn(() => 
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ items: [], total: 0 })
      })
    );

    await fetchTickets({ page: 1, pageSize: 10 }, mockFetch as any);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('page=1'),
      expect.any(Object)
    );
  });
});
```

### Integration Tests

**Location:** `tests/integration/workflows/{Feature}.test.ts`

**Example: User Workflow Test**

```typescript
// tests/integration/workflows/TicketCreation.test.ts
import { describe, it, expect } from 'vitest';
import { render, fireEvent, waitFor } from '@testing-library/svelte';
import TicketFormModal from '$lib/components/modals/TicketFormModal.svelte';

describe('Ticket Creation Workflow', () => {
  it('creates ticket and shows success message', async () => {
    const { getByLabelText, getByText } = render(TicketFormModal);

    // Fill form
    await fireEvent.input(getByLabelText('Title'), { 
      target: { value: 'Test Ticket' } 
    });
    
    // Submit
    await fireEvent.click(getByText('Create'));

    // Verify success
    await waitFor(() => {
      expect(getByText('Ticket created successfully')).toBeInTheDocument();
    });
  });
});
```

---

## Best Practices

### ✅ DO

- **Use descriptive test names**: `MethodName_Scenario_ExpectedResult`
- **Test user behavior**, not implementation details
- **Use `$lib/` imports** for clean, absolute paths
- **Mock external dependencies** (API calls, navigation)
- **Test accessibility** (ARIA labels, keyboard navigation)
- **Keep tests fast** (<50ms per unit test)

### ❌ DON'T

- **Don't test framework internals** (SvelteKit routing, Svelte reactivity)
- **Don't test third-party libraries** (Bootstrap, svelte-i18n)
- **Don't use relative imports** (`../../../lib/...`)
- **Don't test private implementation** (test public API only)
- **Don't skip cleanup** (Vitest handles this, but be aware)

### Naming Conventions

**Test Files:**
```
{ComponentName}.test.ts        ✅ Good
{ComponentName}.spec.ts        ✅ Good (alternative)
test.{ComponentName}.ts        ❌ Bad
```

**Test Suites:**
```typescript
describe('ComponentName', () => { ... })          ✅ Good
describe('ComponentName Component', () => { ... }) ❌ Redundant
```

**Test Cases:**
```typescript
it('renders with correct props', () => { ... })                    ✅ Good
it('should render with correct props', () => { ... })              ⚠️ Acceptable
test('renders with correct props', () => { ... })                  ✅ Good (alternative)
```

---

## Test Helpers

### Available Mocks (from `vitest.setup.ts`)

**SvelteKit Navigation:**
```typescript
import { goto } from '$app/navigation'; // Mocked
await goto('/tickets'); // No-op in tests
```

**SvelteKit Stores:**
```typescript
import { page } from '$app/stores'; // Mocked
// Access via: $page.data.user
```

**i18n:**
```typescript
import { getMessage } from '$lib/i18n'; // Mocked
getMessage('key'); // Returns 'key' by default
```

### Custom Helpers

Create reusable test utilities in `tests/unit/helpers/` or `tests/integration/helpers/`:

```typescript
// tests/unit/helpers/renderWithContext.ts
import { render } from '@testing-library/svelte';

export function renderWithUser(Component: any, user: any) {
  return render(Component, {
    context: new Map([['user', user]])
  });
}
```

---

## Configuration Files

### vitest.config.ts

Key settings:
- **Environment:** `jsdom` (browser DOM simulation)
- **Globals:** `true` (use `describe`, `it`, `expect` without imports)
- **Setup:** `vitest.setup.ts` (runs before all tests)
- **Include:** `tests/**/*.{test,spec}.{js,ts}` (test file pattern)

### vitest.setup.ts

Provides global mocks for:
- `$app/navigation` (goto, invalidate, etc.)
- `$app/stores` (page, navigating, updated)
- `$app/state` (page data)
- `svelte-i18n` (t, locale, format)
- `$lib/i18n` (getMessage, init, setLocale)

---

## Coverage Targets

| Category | Target | Status |
|----------|--------|--------|
| Components | 70-80% | 🟡 In Progress |
| Services | 80-90% | 🟡 In Progress |
| Utilities | 90%+ | ⏳ Pending |
| Overall | 70%+ | ⏳ Pending |

### Viewing Coverage

```bash
pnpm test:coverage
# Opens coverage report in coverage/index.html
```

---

## Troubleshooting

### Tests Not Found

**Problem:** "No test files found"  
**Solution:** Ensure files match pattern `tests/**/*.{test,spec}.{js,ts}`

### Import Errors

**Problem:** "Cannot find module '$lib/...'"  
**Solution:** Check `vitest.config.ts` has correct path aliases

### Svelte Component Errors

**Problem:** "lifecycle_function_unavailable"  
**Solution:** Use `sveltekit()` plugin in vitest config, not `svelte()`

### Timeout Errors

**Problem:** Tests hang or timeout  
**Solution:** Add `testTimeout: 10000` to vitest.config.ts

---

## Resources

- **Vitest Docs:** https://vitest.dev/
- **Testing Library:** https://testing-library.com/docs/svelte-testing-library/intro
- **Backend Testing Guide:** `../backend/README.testing.md`

---

**Document Owner:** Development Team  
**Last Updated:** December 14, 2025
