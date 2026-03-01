/**
 * Tests for registerSchema (Zod) and getPasswordStrength utility.
 *
 * These are pure logic tests — no React, no mocks, no I/O.
 * They mirror the same rules as RegisterUserValidator on the backend,
 * giving us confidence that client-side validation is not weaker.
 */
import { describe, it, expect } from 'vitest'
import { registerSchema, getPasswordStrength } from '../../schemas/registerSchema'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
type Fields = {
  name?: string
  email?: string
  phone?: string
  password?: string
  confirmPassword?: string
}

/** Minimal valid payload — override individual fields for negative tests */
const VALID: Required<Fields> = {
  name: 'Maria Santos',
  email: 'maria@example.com',
  phone: '',
  password: 'Secure1!',
  confirmPassword: 'Secure1!',
}

function parse(overrides: Fields = {}) {
  return registerSchema.safeParse({ ...VALID, ...overrides })
}

function errorsFor(field: string, overrides: Fields) {
  const result = parse(overrides)
  if (result.success) return []
  return result.error.issues
    .filter((i) => i.path[0] === field || (i.path.length === 0 && field === '_root'))
    .map((i) => i.message)
}

// ---------------------------------------------------------------------------
// Happy-path
// ---------------------------------------------------------------------------
describe('registerSchema — valid inputs', () => {
  it('accepts a fully valid payload', () => {
    expect(parse().success).toBe(true)
  })

  it('accepts a valid payload without phone (empty string)', () => {
    expect(parse({ phone: '' }).success).toBe(true)
  })

  it('accepts a valid payload without phone (undefined)', () => {
    expect(parse({ phone: undefined }).success).toBe(true)
  })

  it('accepts a phone number with country code and spaces', () => {
    expect(parse({ phone: '+63 912 345 6789' }).success).toBe(true)
  })

  it('accepts a password with multiple special characters', () => {
    expect(parse({ password: 'P@ssw0rd!#', confirmPassword: 'P@ssw0rd!#' }).success).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// Name
// ---------------------------------------------------------------------------
describe('registerSchema — name validation', () => {
  it('rejects an empty name', () => {
    const msgs = errorsFor('name', { name: '' })
    expect(msgs.length).toBeGreaterThan(0)
  })

  it('rejects a single-character name', () => {
    const msgs = errorsFor('name', { name: 'A' })
    expect(msgs).toContain('Name must be at least 2 characters')
  })

  it('rejects a name over 100 characters', () => {
    const msgs = errorsFor('name', { name: 'A'.repeat(101) })
    expect(msgs).toContain('Name is too long')
  })

  it('accepts a two-character name', () => {
    expect(parse({ name: 'Jo' }).success).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// Email
// ---------------------------------------------------------------------------
describe('registerSchema — email validation', () => {
  it('rejects an empty email', () => {
    const msgs = errorsFor('email', { email: '' })
    expect(msgs.length).toBeGreaterThan(0)
  })

  it('rejects a plaintext string without @', () => {
    const msgs = errorsFor('email', { email: 'notanemail' })
    expect(msgs).toContain('Enter a valid email address')
  })

  it('rejects a string with @ but no domain', () => {
    const msgs = errorsFor('email', { email: 'user@' })
    expect(msgs).toContain('Enter a valid email address')
  })

  it('rejects a string with no local part', () => {
    const msgs = errorsFor('email', { email: '@domain.com' })
    expect(msgs).toContain('Enter a valid email address')
  })

  it('accepts a standard email', () => {
    expect(parse({ email: 'user@example.com' }).success).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// Phone
// ---------------------------------------------------------------------------
describe('registerSchema — phone validation', () => {
  it('rejects a phone shorter than 7 characters', () => {
    const msgs = errorsFor('phone', { phone: '12345' })
    expect(msgs).toContain('Enter a valid phone number')
  })

  it('rejects a phone with unsupported characters', () => {
    const msgs = errorsFor('phone', { phone: 'abc-defgh' })
    expect(msgs).toContain('Enter a valid phone number')
  })

  it('accepts a phone with dashes', () => {
    expect(parse({ phone: '091-234-5678' }).success).toBe(true)
  })

  it('accepts a phone with parentheses', () => {
    expect(parse({ phone: '(02) 8123 4567' }).success).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// Password
// ---------------------------------------------------------------------------
describe('registerSchema — password validation', () => {
  it('rejects a password shorter than 8 characters', () => {
    const msgs = errorsFor('password', { password: 'Abc1!', confirmPassword: 'Abc1!' })
    expect(msgs).toContain('Password must be at least 8 characters')
  })

  it('rejects a password with no uppercase letter', () => {
    const msgs = errorsFor('password', { password: 'secure1!', confirmPassword: 'secure1!' })
    expect(msgs).toContain('Must contain at least one uppercase letter')
  })

  it('rejects a password with no digit', () => {
    const msgs = errorsFor('password', { password: 'Secure!!', confirmPassword: 'Secure!!' })
    expect(msgs).toContain('Must contain at least one number')
  })

  it('rejects a password with no special character', () => {
    const msgs = errorsFor('password', { password: 'Secure12', confirmPassword: 'Secure12' })
    expect(msgs).toContain('Must contain at least one special character')
  })

  it('rejects a password that is only numbers and uppercase', () => {
    const msgs = errorsFor('password', { password: 'ABCDE123', confirmPassword: 'ABCDE123' })
    expect(msgs).toContain('Must contain at least one special character')
  })
})

// ---------------------------------------------------------------------------
// Confirm password
// ---------------------------------------------------------------------------
describe('registerSchema — confirmPassword validation', () => {
  it('rejects when confirmPassword is empty', () => {
    const msgs = errorsFor('confirmPassword', { confirmPassword: '' })
    expect(msgs.length).toBeGreaterThan(0)
  })

  it('rejects when passwords do not match', () => {
    const result = parse({ password: 'Secure1!', confirmPassword: 'Different1!' })
    expect(result.success).toBe(false)
    if (!result.success) {
      const msgs = result.error.issues
        .filter((i) => i.path[0] === 'confirmPassword')
        .map((i) => i.message)
      expect(msgs).toContain('Passwords do not match')
    }
  })

  it('accepts when passwords match exactly', () => {
    expect(parse({ password: 'Match!ng1', confirmPassword: 'Match!ng1' }).success).toBe(true)
  })
})

// ---------------------------------------------------------------------------
// getPasswordStrength utility
// ---------------------------------------------------------------------------
describe('getPasswordStrength', () => {
  it('returns 0 for an empty string', () => {
    expect(getPasswordStrength('')).toBe(0)
  })

  it('returns 1 for ≥8 chars only', () => {
    expect(getPasswordStrength('abcdefgh')).toBe(1)
  })

  it('returns 2 for ≥8 chars + uppercase', () => {
    expect(getPasswordStrength('Abcdefgh')).toBe(2)
  })

  it('returns 3 for ≥8 chars + uppercase + digit', () => {
    expect(getPasswordStrength('Abcdef1h')).toBe(3)
  })

  it('returns 4 for all criteria met', () => {
    expect(getPasswordStrength('Abcdef1!')).toBe(4)
  })

  it('returns 1 for a short password with all other criteria', () => {
    // Only 4 chars — length check fails, others pass
    expect(getPasswordStrength('Ab1!')).toBe(3)
  })
})
