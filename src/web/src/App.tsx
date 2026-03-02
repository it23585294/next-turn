/**
 * App — root router.
 *
 * Routes:
 *  /                       → WelcomePage (landing)
 *  /register/:tenantId     → RegisterPage
 *  /login/:tenantId        → LoginPage
 *  /dashboard/:tenantId    → ProtectedRoute (any role)  → DashboardPage
 *  /staff/:tenantId        → ProtectedRoute (Staff+)    → stub
 *  /admin/:tenantId        → ProtectedRoute (OrgAdmin+) → stub
 *  /system/:tenantId       → ProtectedRoute (SystemAdmin) → stub
 *  /access-denied          → AccessDeniedPage
 *  *                       → redirect to /
 *
 * The /staff, /admin, /system routes are stubs (NT-12-66) — they exist solely
 * to prove the role guards work in tests and during the sprint demo.
 * Real feature pages will replace the stubs in Sprint 2+.
 */
import { Routes, Route, Navigate } from 'react-router-dom'
import { WelcomePage } from './pages/Welcome'
import { RegisterPage } from './pages/Register'
import { LoginPage } from './pages/Login'
import { DashboardPage } from './pages/Dashboard'
import { AccessDeniedPage } from './pages/AccessDenied'
import { OrgRegistrationPage } from './pages/OrgRegistration'
import { ProtectedRoute } from './components/ProtectedRoute'
import { QueuePage } from './pages/Queue'
import { AdminDashboardPage } from './pages/Admin'

// ── Role-restricted stub pages ────────────────────────────────────────────────
// Temporary placeholders so the route guards have real targets during NT-12
// testing and the sprint demo. Replace with real feature pages in Sprint 2+.
const StaffStub     = () => <main style={{ padding: '2rem' }}><h1>Staff Area — Sprint 2</h1></main>
const SystemAdminStub = () => <main style={{ padding: '2rem' }}><h1>System Area — Sprint 2</h1></main>

function App() {
  return (
    <Routes>
      <Route path="/" element={<WelcomePage />} />
      <Route path="/register/:tenantId" element={<RegisterPage />} />
      <Route path="/login/:tenantId" element={<LoginPage />} />

      {/* Any authenticated user */}
      <Route
        path="/dashboard/:tenantId"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />

      {/* Staff and above */}
      <Route
        path="/staff/:tenantId"
        element={
          <ProtectedRoute allowedRoles={['Staff', 'OrgAdmin', 'SystemAdmin']}>
            <StaffStub />
          </ProtectedRoute>
        }
      />

      {/* OrgAdmin and above */}
      <Route
        path="/admin/:tenantId"
        element={
          <ProtectedRoute allowedRoles={['OrgAdmin', 'SystemAdmin']}>
            <AdminDashboardPage />
          </ProtectedRoute>
        }
      />

      {/* SystemAdmin only */}
      <Route
        path="/system/:tenantId"
        element={
          <ProtectedRoute allowedRoles={['SystemAdmin']}>
            <SystemAdminStub />
          </ProtectedRoute>
        }
      />

      {/* Any authenticated user — join a specific queue */}
      <Route
        path="/queues/:tenantId/:queueId"
        element={
          <ProtectedRoute>
            <QueuePage />
          </ProtectedRoute>
        }
      />

      {/* Public — no tenant context required; org doesn't exist yet */}
      <Route path="/register-org" element={<OrgRegistrationPage />} />

      <Route path="/access-denied" element={<AccessDeniedPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
