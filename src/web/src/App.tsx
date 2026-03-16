/**
 * App — root router.
 *
 * Routes:
 *  /                       → WelcomePage (landing)
 *  /register               → GlobalRegisterPage (consumer — no tenantId)
 *  /login                  → GlobalLoginPage    (consumer — no tenantId)
 *  /register/:tenantId     → RegisterPage (org-member)
 *  /login/:tenantId        → LoginPage    (org-member)
 *  /dashboard              → ProtectedRoute → DashboardPage (consumer user, no tenantId)
 *  /dashboard/:tenantId    → ProtectedRoute → DashboardPage
 *  /staff/:tenantId        → ProtectedRoute (Staff+)    → StaffDashboardPage
 *  /admin/:tenantId        → ProtectedRoute (OrgAdmin+) → AdminDashboardPage
 *  /system/:tenantId       → ProtectedRoute (SystemAdmin) → stub
 *  /queues/:tenantId/:queueId → ProtectedRoute → QueuePage
 *  /register-org           → OrgRegistrationPage
 *  /access-denied          → AccessDeniedPage
 *  *                       → redirect to /
 */
import { Routes, Route, Navigate } from 'react-router-dom'
import { WelcomePage }          from './pages/Welcome'
import { RegisterPage }         from './pages/Register'
import { LoginPage }            from './pages/Login'
import { GlobalRegisterPage }   from './pages/GlobalRegister'
import { GlobalLoginPage }      from './pages/GlobalLogin'
import { DashboardPage }        from './pages/Dashboard'
import { AccessDeniedPage }     from './pages/AccessDenied'
import { OrgRegistrationPage }  from './pages/OrgRegistration'
import { ProtectedRoute }       from './components/ProtectedRoute'
import { QueuePage }            from './pages/Queue'
import { AppointmentPage }      from './pages/Appointment'
import { AdminDashboardPage }   from './pages/Admin'
import { StaffDashboardPage }   from './pages/Staff'
import { StaffInviteAcceptPage } from './pages/StaffInviteAccept'
import { OrgLoginLookupPage }   from './pages/OrgLoginLookup'
import { TermsPage, PrivacyPage } from './pages/Legal'

// ── Role-restricted stub pages ────────────────────────────────────────────────
// System admin area is still a placeholder until the corresponding story lands.
const SystemAdminStub = () => <main style={{ padding: '2rem' }}><h1>System Area — Sprint 2</h1></main>

function App() {
  return (
    <Routes>
      <Route path="/" element={<WelcomePage />} />

      {/* Consumer (end-user) auth — no tenantId */}
      <Route path="/register" element={<GlobalRegisterPage />} />
      <Route path="/login"    element={<GlobalLoginPage />} />

      {/* Org-member auth — scoped to a specific org */}
      <Route path="/register/:tenantId" element={<RegisterPage />} />
      <Route path="/login/:tenantId"    element={<LoginPage />} />
      <Route path="/login/o/:orgSlug"   element={<LoginPage />} />

      {/* Any authenticated user */}
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
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
            <StaffDashboardPage />
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

      <Route
        path="/appointments"
        element={
          <ProtectedRoute>
            <AppointmentPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/appointments/:tenantId"
        element={
          <ProtectedRoute>
            <AppointmentPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/appointments/:tenantId/:appointmentProfileId"
        element={
          <ProtectedRoute>
            <AppointmentPage />
          </ProtectedRoute>
        }
      />

      <Route path="/staff/invite/accept" element={<StaffInviteAcceptPage />} />

      {/* Public — no tenant context required; org doesn't exist yet */}
      <Route path="/register-org" element={<OrgRegistrationPage />} />
      <Route path="/find-org-login" element={<OrgLoginLookupPage />} />

      {/* Legal */}
      <Route path="/terms"   element={<TermsPage />} />
      <Route path="/privacy" element={<PrivacyPage />} />

      <Route path="/access-denied" element={<AccessDeniedPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
