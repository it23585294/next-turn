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
 * Sprint 2 hardening (NT-XX):
 *   - Add `returnTo` state to the Navigate so the user is redirected back after login.
 *   - Move token storage from localStorage to in-memory + httpOnly cookie.
 */
import { Navigate, useParams } from 'react-router-dom'
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

  // Step 1 — authentication check
  if (!isAuthenticated()) {
    const loginPath = tenantId ? `/login/${tenantId}` : '/'
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
