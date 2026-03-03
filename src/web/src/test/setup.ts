/**
 * Vitest global setup — runs before every test file.
 * Extends expect with jest-dom matchers (toBeInTheDocument, etc.).
 */
import '@testing-library/jest-dom'
import { vi } from 'vitest'

// Static asset imports (PNG, SVG, etc.) return the filename string in Vite's
// transform pipeline; jsdom doesn't render img content, so this is fine.

// Silence the React "act()" warnings triggered by async state updates in tests.
// They're informational in this context and not test failures.
const originalError = console.error
beforeAll(() => {
  console.error = (...args: unknown[]) => {
    if (
      typeof args[0] === 'string' &&
      args[0].includes('Warning: An update to')
    ) {
      return
    }
    originalError(...args)
  }
})

afterAll(() => {
  console.error = originalError
})

afterEach(() => {
  vi.clearAllMocks()
})
