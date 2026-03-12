/**
 * authToken — lightweight localStorage wrapper for the JWT access token.
 *
 * Deliberately simple: Sprint 1 stores the token in localStorage so the
 * frontend stays stateless across page refreshes. Sprint 2 will harden this
 * by moving the access token to in-memory state and using an httpOnly cookie
 * for the refresh token (see NT-XX: security hardening).
 *
 * token lifecycle:
 *  saveToken  → called after successful login, persists JWT
 *  getToken   → called by API interceptor / auth guard to attach Bearer header
 *  clearToken → called on logout or 401 response
 *
 * getTokenPayload — decodes the JWT payload (base64url) without verifying the
 * signature. The signature is verified server-side on every authenticated
 * request. Client-side we decode purely for UX (display name, role, expiry
 * check) — never for security decisions.
 */

const TOKEN_KEY = 'nt_access_token'

/** Persist the JWT in localStorage. */
export function saveToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

/** Retrieve the stored JWT, or null if absent. */
export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

/** Remove the JWT — used on logout or after a 401 from the API. */
export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}

/**
 * Decoded JWT payload shape.
 * Only the claims our frontend uses — the full payload may carry more.
 */
export interface TokenPayload {
  /** userId — maps to JWT 'sub' claim */
  sub: string
  email: string
  name: string
  /** UserRole enum string: "User" | "Staff" | "OrgAdmin" | "SystemAdmin" */
  role: string
  /** tenantId */
  tid: string
  /** expiry — Unix timestamp (seconds) */
  exp: number
  /** issued-at — Unix timestamp (seconds) */
  iat: number
}

/**
 * Decode and return the JWT payload from the stored token.
 *
 * Returns null if:
 *  - no token is stored
 *  - the token is malformed (wrong number of segments, invalid base64)
 *
 * Does NOT verify the signature — that is the server's job.
 */
export function getTokenPayload(): TokenPayload | null {
  const token = getToken()
  if (!token) return null

  // A JWT has three dotm-separated segments: header.payload.signature
  const parts = token.split('.')
  if (parts.length !== 3) return null

  try {
    // base64url → base64 → JSON
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    // atob pads automatically in modern browsers
    const json = atob(base64)
    return JSON.parse(json) as TokenPayload
  } catch {
    // Malformed payload — treat as missing token
    return null
  }
}
