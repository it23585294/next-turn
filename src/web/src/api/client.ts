/**
 * Axios instance — base URL from environment variable.
 * The Vite dev-server proxy rewrites /api → http://localhost:5000/api
 * so this works identically in dev and production.
 *
 * Global 401 interceptor (NT-12-68):
 *   A 401 from any API call means the token has expired or been tampered with.
 *   The interceptor clears the token and hard-navigates to the login page with
 *   ?reason=session_expired so LoginPage can show an informational banner.
 *   window.location.replace() is used intentionally — we're outside React's
 *   component tree here, and a full navigation ensures stale in-memory state
 *   is wiped before the login page mounts.
 */
import axios from 'axios'
import type { AxiosError } from 'axios'
import type { ApiError, ProblemDetails } from '../types/api'
import { clearToken, getTokenPayload } from '../utils/authToken'

// In production (Vercel), VITE_API_URL is set to the full Azure API base URL
// e.g. https://nextturn.azurewebsites.net/api
// In development, it is undefined and falls back to '/api' which the Vite
// dev-server proxy rewrites to http://localhost:5258/api.
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15_000,
})

// ── Global 401 interceptor ───────────────────────────────────────────────────
// Catches 401 responses from any endpoint, clears the stored token, and
// redirects to the appropriate login page with ?reason=session_expired.
// 403 responses are intentionally NOT handled here — they are caught by
// individual call sites (or propagate as ApiError to the ProtectedRoute guard).
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Try to get the tenant from the (now-invalid) token so we redirect to
      // the right organisation's login page. Fall back to '/' if not available.
      const payload = getTokenPayload()
      const tid = payload?.tid

      clearToken()

      const loginPath = tid
        ? `/login/${tid}?reason=session_expired`
        : `/?reason=session_expired`

      // Hard navigation — clears React state, ensures a clean login mount.
      window.location.replace(loginPath)
    }
    return Promise.reject(error)
  }
)

/**
 * Extracts a friendly ApiError from any Axios error.
 * Handles both problem+json (our API) and generic network/timeout errors.
 */
export function parseApiError(err: unknown): ApiError {
  const axiosErr = err as AxiosError<ProblemDetails>
  if (axiosErr.response) {
    const data = axiosErr.response.data ?? {}
    return {
      status: axiosErr.response.status,
      detail: data.detail ?? data.title,
      errors: data.errors,
      raw: data,
    }
  }
  // Network error / timeout
  return {
    status: 0,
    detail: 'Could not reach the server. Please check your connection.',
    raw: {},
  }
}
