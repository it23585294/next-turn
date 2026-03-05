/**
 * ProtectedRoute — wraps any route that requires authentication and optionally
 * a specific role.
 *
 * Usage in App.tsx:
 *   // Any authenticated user
 *   <ProtectedRoute><DashboardPage /></ProtectedRoute>
 *
 *   // Staff or higher only
 *   <ProtectedRoute allowedRoles={['Staff', 'OrgAdmin', 'SystemAdmin']}>
 *     <StaffPage />
 *   </ProtectedRoute>
 *
 * Guard logic (in order):
 *   1. Not authenticated (missing / expired token)
 *      → Navigate to /login/:tenantId
 *   2. Authenticated but role not in allowedRoles
 *      → Navigate to /access-denied
 *   3. Authenticated + role allowed (or no restriction)
 *      → Render children
 *
 * Why useParams works here:
 *   ProtectedRoute is the element of a Route that declares :tenantId, so it
 *   is rendered inside that route's context and useParams() can read the param.
 *
 * returnTo flow (NT-31+):
 *   When an unauthenticated user visits a protected route (e.g. a queue link shared by
 *   an org), ProtectedRoute redirects them to /login/:tenantId?returnTo=<original path>.
 *   LoginPage reads that param and navigates back after a successful login, so the user
 *   lands exactly where they intended rather than on the generic dashboard.
 */
import { Navigate, useParams, useLocation } from 'react-router-dom'
import type { ReactNode } from 'react'
import { isAuthenticated } from '../utils/authGuard'
import { getTokenPayload } from '../utils/authToken'

interface ProtectedRouteProps {
  children: ReactNode
  /**
   * When provided, the authenticated user's role must be one of these values.
   * If omitted, any authenticated user is allowed through.
   */
  allowedRoles?: string[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { tenantId } = useParams<{ tenantId: string }>()
  const location = useLocation()

  // Step 1 — authentication check
  if (!isAuthenticated()) {
    // Encode the full current path so LoginPage can redirect back after login.
    const returnTo = encodeURIComponent(location.pathname + location.search)
    const loginPath = tenantId
      ? `/login/${tenantId}?returnTo=${returnTo}`
      : '/'
    return <Navigate to={loginPath} replace />
  }

  // Step 2 — role check (only when allowedRoles is specified)
  if (allowedRoles && allowedRoles.length > 0) {
    const payload = getTokenPayload()
    const role = payload?.role ?? ''
    if (!allowedRoles.includes(role)) {
      return <Navigate to="/access-denied" replace />
    }
  }

  return <>{children}</>
}
