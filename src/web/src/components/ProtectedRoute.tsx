/**
 * ProtectedRoute — wraps any route that requires authentication.
 *
 * Usage in App.tsx:
 *   <Route
 *     path="/dashboard/:tenantId"
 *     element={
 *       <ProtectedRoute>
 *         <DashboardPage />
 *       </ProtectedRoute>
 *     }
 *   />
 *
 * If the user is NOT authenticated:
 *   → Navigate to /login/:tenantId (preserving the tenantId from the URL so
 *     the user lands on the correct organisation's login page after logging in)
 *
 * Why useParams here works:
 *   ProtectedRoute is the *element* of a Route that declares :tenantId, so it
 *   is rendered inside that route's context and useParams() can see the param.
 *
 * Sprint 2 hardening (NT-XX):
 *   - Add `returnTo` state to the Navigate so the user is redirected back to
 *     their original destination after a successful login.
 *   - Move token storage from localStorage to in-memory + httpOnly cookie.
 */
import { Navigate, useParams } from 'react-router-dom'
import type { ReactNode } from 'react'
import { isAuthenticated } from '../utils/authGuard'

interface ProtectedRouteProps {
  children: ReactNode
}

export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { tenantId } = useParams<{ tenantId: string }>()

  if (!isAuthenticated()) {
    // Redirect to the correct tenant's login page.
    // If tenantId is somehow absent (mis-wired route), fall back to /.
    const loginPath = tenantId ? `/login/${tenantId}` : '/'
    return <Navigate to={loginPath} replace />
  }

  return <>{children}</>
}
