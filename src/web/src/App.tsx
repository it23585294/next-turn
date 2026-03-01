/**
 * App — root router.
 *
 * Routes:
 *  /                    → WelcomePage (landing)
 *  /register/:tenantId  → RegisterPage
 *  /login/:tenantId     → LoginPage       (NT-59)
 *  /dashboard/:tenantId → ProtectedRoute → DashboardPage  (NT-60)
 *  *                    → redirect to /
 */
import { Routes, Route, Navigate } from 'react-router-dom'
import { WelcomePage } from './pages/Welcome'
import { RegisterPage } from './pages/Register'
import { LoginPage } from './pages/Login'
import { DashboardPage } from './pages/Dashboard'
import { ProtectedRoute } from './components/ProtectedRoute'

function App() {
  return (
    <Routes>
      <Route path="/" element={<WelcomePage />} />
      <Route path="/register/:tenantId" element={<RegisterPage />} />
      <Route path="/login/:tenantId" element={<LoginPage />} />
      <Route
        path="/dashboard/:tenantId"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
