/**
 * Tests for authToken utilities (utils/authToken.ts).
 *
 * Strategy: each test isolates localStorage via beforeEach/afterEach — no
 * state leaks between tests. We use vitest's built-in `localStorage` (jsdom).
 *
 * getTokenPayload tests use real JWTs crafted in-test (header.payload.sig
 * base64url encoded) rather than calling a real JWT library — the utility
 * is specifically documented to NOT verify the signature, so we test the
 * decoder directly.
 *
 * Tests:
 *  saveToken / getToken / clearToken — basic storage lifecycle
 *  getTokenPayload — happy path, fields decoded correctly
 *  getTokenPayload — missing token returns null
 *  getTokenPayload — malformed token (missing segment) returns null
 *  getTokenPayload — malformed base64 returns null
 */
import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import {
  saveToken,
  getToken,
  clearToken,
  getTokenPayload,
  type TokenPayload,
} from '../../utils/authToken'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Produce a base64url-encoded JWT with the given payload claim object.
 *  The header and signature are static stubs — only the payload segment matters. */
function makeJwt(payload: Partial<TokenPayload>): string {
  const header  = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  const body    = btoa(JSON.stringify(payload))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  const sig     = 'stub-sig'
  return `${header}.${body}.${sig}`
}

const SAMPLE_PAYLOAD: TokenPayload = {
  sub:   'c0ffee00-0000-0000-0000-000000000001',
  email: 'alice@example.com',
  name:  'Alice Smith',
  role:  'User',
  tid:   'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  exp:   Math.floor(Date.now() / 1000) + 3600, // 1 hour from now
  iat:   Math.floor(Date.now() / 1000),
}

// ---------------------------------------------------------------------------
// Setup / teardown
// ---------------------------------------------------------------------------
beforeEach(() => {
  localStorage.clear()
})

afterEach(() => {
  localStorage.clear()
})

// ---------------------------------------------------------------------------
// saveToken / getToken / clearToken
// ---------------------------------------------------------------------------
describe('saveToken / getToken / clearToken', () => {
  it('getToken returns null when no token has been saved', () => {
    expect(getToken()).toBeNull()
  })

  it('saveToken persists the token and getToken retrieves it', () => {
    const jwt = makeJwt(SAMPLE_PAYLOAD)
    saveToken(jwt)
    expect(getToken()).toBe(jwt)
  })

  it('clearToken removes the stored token', () => {
    saveToken(makeJwt(SAMPLE_PAYLOAD))
    clearToken()
    expect(getToken()).toBeNull()
  })

  it('overwriting with saveToken replaces the previous token', () => {
    const first  = makeJwt({ ...SAMPLE_PAYLOAD, sub: 'user-1' })
    const second = makeJwt({ ...SAMPLE_PAYLOAD, sub: 'user-2' })
    saveToken(first)
    saveToken(second)
    expect(getToken()).toBe(second)
  })
})

// ---------------------------------------------------------------------------
// getTokenPayload
// ---------------------------------------------------------------------------
describe('getTokenPayload', () => {
  it('returns null when no token is stored', () => {
    expect(getTokenPayload()).toBeNull()
  })

  it('decodes and returns the payload from a stored token', () => {
    saveToken(makeJwt(SAMPLE_PAYLOAD))
    const decoded = getTokenPayload()
    expect(decoded).not.toBeNull()
    expect(decoded!.email).toBe(SAMPLE_PAYLOAD.email)
    expect(decoded!.name).toBe(SAMPLE_PAYLOAD.name)
    expect(decoded!.role).toBe(SAMPLE_PAYLOAD.role)
    expect(decoded!.sub).toBe(SAMPLE_PAYLOAD.sub)
    expect(decoded!.tid).toBe(SAMPLE_PAYLOAD.tid)
  })

  it('returns null for a token with only one segment', () => {
    saveToken('onlyone')
    expect(getTokenPayload()).toBeNull()
  })

  it('returns null for a token with only two segments', () => {
    saveToken('header.payload')
    expect(getTokenPayload()).toBeNull()
  })

  it('returns null when the payload segment is not valid base64', () => {
    // Middle segment is garbage that atob cannot parse
    saveToken('header.!!!invalid!!!.sig')
    expect(getTokenPayload()).toBeNull()
  })
})
