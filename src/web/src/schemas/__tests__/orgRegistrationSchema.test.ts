/**
 * Tests for orgRegistrationSchema (Zod).
 *
 * Pure logic tests — no React, no mocks, no I/O.
 * Mirrors RegisterOrganisationCommandValidator on the backend, giving
 * confidence that client-side rules are not weaker than the server's.
 */
import { describe, it, expect } from 'vitest'
import { orgRegistrationSchema, ORG_TYPES } from '../../schemas/orgRegistrationSchema'

// ── Helpers ──────────────────────────────────────────────────────────────────

type Fields = {
  orgName?: string
  addressLine1?: string
  city?: string
  postalCode?: string
  country?: string
  orgType?: string
  adminName?: string
  adminEmail?: string
}

const VALID: Required<Fields> = {
  orgName:      'Acme Corp',
  addressLine1: '123 Main Street',
  city:         'London',
  postalCode:   'SW1A 1AA',
  country:      'United Kingdom',
  orgType:      'Healthcare',
  adminName:    'Jane Smith',
  adminEmail:   'admin@acme.com',
}

function parse(overrides: Fields = {}) {
  return orgRegistrationSchema.safeParse({ ...VALID, ...overrides })
}

function errorsFor(field: keyof Fields, overrides: Fields) {
  const result = parse(overrides)
  if (result.success) return []
  return result.error.issues
    .filter((i) => i.path[0] === field)
    .map((i) => i.message)
}

// ── Valid payload ─────────────────────────────────────────────────────────────

describe('orgRegistrationSchema — valid payload', () => {
  it('accepts a fully valid payload', () => {
    expect(parse().success).toBe(true)
  })

  it('accepts all five valid orgType values', () => {
    for (const t of ORG_TYPES) {
      expect(parse({ orgType: t }).success).toBe(true)
    }
  })
})

// ── Required fields ───────────────────────────────────────────────────────────

describe('orgRegistrationSchema — required field errors', () => {
  const requiredCases: Array<[keyof Fields, string]> = [
    ['orgName',      'Organisation name is required'],
    ['addressLine1', 'Address line 1 is required'],
    ['city',         'City is required'],
    ['postalCode',   'Postal code is required'],
    ['country',      'Country is required'],
    ['adminName',    'Admin name is required'],
    ['adminEmail',   'Admin email is required'],
  ]

  for (const [field, expectedMessage] of requiredCases) {
    it(`rejects empty ${field}`, () => {
      const errors = errorsFor(field, { [field]: '' })
      expect(errors).toContain(expectedMessage)
    })
  }

  it('rejects empty orgType (not in enum)', () => {
    const errors = errorsFor('orgType', { orgType: '' })
    expect(errors.length).toBeGreaterThan(0)
  })
})

// ── adminEmail ────────────────────────────────────────────────────────────────

describe('orgRegistrationSchema — adminEmail field', () => {
  it('rejects an email without an @ symbol', () => {
    const errors = errorsFor('adminEmail', { adminEmail: 'notanemail' })
    expect(errors.some((e) => /valid email/i.test(e))).toBe(true)
  })

  it('rejects an email missing the domain part', () => {
    const errors = errorsFor('adminEmail', { adminEmail: 'user@' })
    expect(errors.some((e) => /valid email/i.test(e))).toBe(true)
  })

  it('accepts a valid email address', () => {
    const errors = errorsFor('adminEmail', { adminEmail: 'valid@example.com' })
    expect(errors.length).toBe(0)
  })
})

// ── orgType enum ──────────────────────────────────────────────────────────────

describe('orgRegistrationSchema — orgType field', () => {
  it('rejects an arbitrary invalid string', () => {
    const errors = errorsFor('orgType', { orgType: 'InvalidType' })
    expect(errors.length).toBeGreaterThan(0)
    expect(errors[0]).toMatch(/valid organisation type/i)
  })

  it('rejects a lowercase valid enum name (enum is case-sensitive in Zod)', () => {
    // Backend's FluentValidation does ignoreCase — the Zod enum is intentionally
    // case-sensitive because the select options already use the exact casing.
    const errors = errorsFor('orgType', { orgType: 'healthcare' })
    expect(errors.length).toBeGreaterThan(0)
  })

  it('accepts each value from ORG_TYPES exactly', () => {
    for (const t of ORG_TYPES) {
      expect(parse({ orgType: t }).success).toBe(true)
    }
  })
})

// ── Max-length guards ─────────────────────────────────────────────────────────

describe('orgRegistrationSchema — max-length guards', () => {
  it('rejects orgName longer than 200 characters', () => {
    const errors = errorsFor('orgName', { orgName: 'A'.repeat(201) })
    expect(errors.some((e) => /200/i.test(e))).toBe(true)
  })

  it('accepts orgName of exactly 200 characters', () => {
    expect(parse({ orgName: 'A'.repeat(200) }).success).toBe(true)
  })

  it('rejects adminEmail longer than 320 characters', () => {
    const longEmail = 'a@b.' + 'c'.repeat(317)
    const errors = errorsFor('adminEmail', { adminEmail: longEmail })
    expect(errors.some((e) => /320/i.test(e))).toBe(true)
  })
})
