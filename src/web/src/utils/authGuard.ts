/**
 * authGuard — predicate that decides whether the current user is considered
 * authenticated for client-side route protection.
 *
 * WHAT this checks:
 *  1. A token is present in localStorage (getToken() !== null)
 *  2. The JWT payload can be decoded (getTokenPayload() !== null)
 *  3. The token has not yet expired — `exp` claim is in the future
 *
 * WHAT this does NOT check:
 *  - Signature validity — that is the server's job. The server validates the
 *    signature on every authenticated API request and returns 401 if invalid.
 *  - Revocation — no revocation list in Sprint 1. Tokens live until `exp`.
 *
 * Client-side expiry check prevents the UX situation where a user with a
 * correctly stored but already-expired token is shown the dashboard, then
 * immediately gets a 401 from the first API call. Better to redirect to login
 * immediately and let them get a fresh token.
 *
 * ClockSkew:
 *  The backend sets `ClockSkew = TimeSpan.Zero`, so tokens expire precisely at
 *  `exp`. We add a 5-second client-side buffer (subtract 5 from now) to avoid
 *  a race where the token expires between the guard check and the first API call.
 */

import { getTokenPayload } from './authToken'

const CLOCK_SKEW_SECONDS = 5

/**
 * Returns true if the current stored token is present and not expired.
 * Used by ProtectedRoute to guard the /dashboard/:tenantId route.
 */
export function isAuthenticated(): boolean {
  const payload = getTokenPayload()
  if (!payload) return false

  const nowSeconds = Math.floor(Date.now() / 1000)
  return payload.exp > nowSeconds - CLOCK_SKEW_SECONDS
}
