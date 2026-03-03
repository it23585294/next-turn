/**
 * Tests for loginSchema (Zod).
 *
 * Pure logic tests — no React, no mocks, no I/O.
 * The schema only enforces structural validity on the client:
 *  - Email must be present and a valid address format
 *  - Password must be present (no complexity rules — that's for registration)
 *
 * Business rules (correct credentials, lockout) are enforced server-side.
 * The frontend schema is intentionally thin to avoid frustrating the user
 * if their password doesn't match client-side complexity rules they never
 * saw when registering on a different device / old app version.
 */
import { describe, it, expect } from 'vitest'
import { loginSchema } from '../../schemas/loginSchema'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
type Fields = { email?: string; password?: string }

const VALID: Required<Fields> = {
  email: 'alice@example.com',
  password: 'AnyPassword1!',
}

function parse(overrides: Fields = {}) {
  return loginSchema.safeParse({ ...VALID, ...overrides })
}

function errorsFor(field: keyof Fields, overrides: Fields) {
  const result = parse(overrides)
  if (result.success) return []
  return result.error.issues
    .filter((i) => i.path[0] === field)
    .map((i) => i.message)
}

// ---------------------------------------------------------------------------
// Happy path
// ---------------------------------------------------------------------------
describe('loginSchema — valid inputs', () => {
  it('accepts a valid email and password', () => {
    expect(parse().success).toBe(true)
  })

  it('accepts any non-empty password regardless of complexity', () => {
    // Login schema does NOT enforce complexity — that is registration-only.
    expect(parse({ password: 'simple' }).success).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// Email validations
// ---------------------------------------------------------------------------
describe('loginSchema — email field', () => {
  it('rejects an empty email', () => {
    const errors = errorsFor('email', { email: '' })
    expect(errors.length).toBeGreaterThan(0)
    expect(errors.some((e) => /required/i.test(e))).toBe(true)
  })

  it('rejects a missing email (undefined)', () => {
    const result = loginSchema.safeParse({ password: VALID.password })
    expect(result.success).toBe(false)
  })

  it('rejects an email without an @ symbol', () => {
    const errors = errorsFor('email', { email: 'notanemail' })
    expect(errors.some((e) => /valid email/i.test(e))).toBe(true)
  })

  it('rejects an email missing the domain part', () => {
    const errors = errorsFor('email', { email: 'user@' })
    expect(errors.some((e) => /valid email/i.test(e))).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// Password validations
// ---------------------------------------------------------------------------
describe('loginSchema — password field', () => {
  it('rejects an empty password', () => {
    const errors = errorsFor('password', { password: '' })
    expect(errors.length).toBeGreaterThan(0)
    expect(errors.some((e) => /required/i.test(e))).toBe(true)
  })

  it('rejects a missing password (undefined)', () => {
    const result = loginSchema.safeParse({ email: VALID.email })
    expect(result.success).toBe(false)
  })
})
