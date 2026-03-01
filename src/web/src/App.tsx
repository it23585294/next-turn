/**
 * App — root router.
 *
 * Routes:
 *  /                    → WelcomePage (landing)
 *  /register/:tenantId  → RegisterPage
 *  *                    → redirect to /
 */
import { Routes, Route, Navigate } from 'react-router-dom'
import { WelcomePage } from './pages/Welcome'
import { RegisterPage } from './pages/Register'

function App() {
  return (
    <Routes>
      <Route path="/" element={<WelcomePage />} />
      <Route path="/register/:tenantId" element={<RegisterPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
