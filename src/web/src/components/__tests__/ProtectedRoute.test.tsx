/**
 * Tests for ProtectedRoute (NT-12-66).
 *
 * Strategy:
 *  - Mock isAuthenticated() and getTokenPayload() to control gate conditions
 *  - Render inside MemoryRouter with a route that declares :tenantId so
 *    useParams() resolves correctly inside the component
 *  - Assert navigation destinations via MemoryRouter route matching
 *
 * Coverage:
 *  1. Unauthenticated (no token) → redirects to /login/:tenantId
 *  2. Unauthenticated (no tenantId in URL) → redirects to /
 *  3. Authenticated, no allowedRoles → renders children
 *  4. Authenticated, role in allowedRoles → renders children
 *  5. Authenticated, role NOT in allowedRoles → redirects to /access-denied
 *  6. Authenticated, empty allowedRoles array → renders children (no restriction)
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { ProtectedRoute } from '../ProtectedRoute'
import * as authGuard from '../../utils/authGuard'
import * as authToken from '../../utils/authToken'

// ---------------------------------------------------------------------------
// Module mocks
// ---------------------------------------------------------------------------
vi.mock('../../utils/authGuard', () => ({ isAuthenticated: vi.fn() }))
vi.mock('../../utils/authToken', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../utils/authToken')>()
  return { ...actual, getTokenPayload: vi.fn() }
})

const mockIsAuthenticated = vi.mocked(authGuard.isAuthenticated)
const mockGetTokenPayload = vi.mocked(authToken.getTokenPayload)

const TENANT = 'aabbccdd-0000-0000-0000-000000000000'

// ---------------------------------------------------------------------------
// Helper
// ---------------------------------------------------------------------------
function renderGuard(
  initialPath: string,
  allowedRoles?: string[]
) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        {/* Route that ProtectedRoute will be nested inside */}
        <Route
          path="/dashboard/:tenantId"
          element={
            <ProtectedRoute allowedRoles={allowedRoles}>
              <div>Protected Content</div>
            </ProtectedRoute>
          }
        />
        {/* Staff route to test role restriction */}
        <Route
          path="/staff/:tenantId"
          element={
            <ProtectedRoute allowedRoles={['Staff', 'OrgAdmin', 'SystemAdmin']}>
              <div>Staff Content</div>
            </ProtectedRoute>
          }
        />
        {/* Destinations — render text so we can assert navigation happened */}
        <Route path="/login/:tenantId" element={<div>Login Page</div>} />
        <Route path="/" element={<div>Welcome Page</div>} />
        <Route path="/access-denied" element={<div>Access Denied Page</div>} />
      </Routes>
    </MemoryRouter>
  )
}

// ---------------------------------------------------------------------------
// 1. Unauthenticated → redirects to /login/:tenantId
// ---------------------------------------------------------------------------
describe('ProtectedRoute — unauthenticated', () => {
  beforeEach(() => {
    mockIsAuthenticated.mockReturnValue(false)
    mockGetTokenPayload.mockReturnValue(null)
  })

  it('redirects to /login/:tenantId when no token is present', () => {
    renderGuard(`/dashboard/${TENANT}`)
    expect(screen.getByText('Login Page')).toBeInTheDocument()
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
  })

  it('redirects to / when tenantId is absent from the URL', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          {/* Route without :tenantId — useParams returns undefined */}
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<div>Welcome Page</div>} />
        </Routes>
      </MemoryRouter>
    )
    expect(screen.getByText('Welcome Page')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 2. Authenticated, no role restriction → renders children
// ---------------------------------------------------------------------------
describe('ProtectedRoute — authenticated, no allowedRoles', () => {
  beforeEach(() => {
    mockIsAuthenticated.mockReturnValue(true)
    mockGetTokenPayload.mockReturnValue({
      sub: '1', email: 'a@b.com', name: 'Alice', role: 'User',
      tid: TENANT, exp: 9999999999, iat: 0,
    })
  })

  it('renders children when no allowedRoles restriction is set', () => {
    renderGuard(`/dashboard/${TENANT}`)
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })

  it('renders children when allowedRoles is an empty array', () => {
    renderGuard(`/dashboard/${TENANT}`, [])
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 3. Authenticated, role IN allowedRoles → renders children
// ---------------------------------------------------------------------------
describe('ProtectedRoute — authenticated, role allowed', () => {
  it.each([
    ['Staff',       ['Staff', 'OrgAdmin', 'SystemAdmin']],
    ['OrgAdmin',    ['Staff', 'OrgAdmin', 'SystemAdmin']],
    ['SystemAdmin', ['Staff', 'OrgAdmin', 'SystemAdmin']],
  ])('renders children for role %s', (role, allowedRoles) => {
    mockIsAuthenticated.mockReturnValue(true)
    mockGetTokenPayload.mockReturnValue({
      sub: '1', email: 'a@b.com', name: 'A', role,
      tid: TENANT, exp: 9999999999, iat: 0,
    })
    renderGuard(`/dashboard/${TENANT}`, allowedRoles)
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 4. Authenticated, role NOT in allowedRoles → redirects to /access-denied
// ---------------------------------------------------------------------------
describe('ProtectedRoute — authenticated, role denied', () => {
  it('redirects User to /access-denied when Staff+ is required', () => {
    mockIsAuthenticated.mockReturnValue(true)
    mockGetTokenPayload.mockReturnValue({
      sub: '1', email: 'a@b.com', name: 'Alice', role: 'User',
      tid: TENANT, exp: 9999999999, iat: 0,
    })

    render(
      <MemoryRouter initialEntries={[`/staff/${TENANT}`]}>
        <Routes>
          <Route
            path="/staff/:tenantId"
            element={
              <ProtectedRoute allowedRoles={['Staff', 'OrgAdmin', 'SystemAdmin']}>
                <div>Staff Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/access-denied" element={<div>Access Denied Page</div>} />
        </Routes>
      </MemoryRouter>
    )

    expect(screen.getByText('Access Denied Page')).toBeInTheDocument()
    expect(screen.queryByText('Staff Content')).not.toBeInTheDocument()
  })

  it.each([
    ['User',  ['OrgAdmin', 'SystemAdmin']],
    ['Staff', ['OrgAdmin', 'SystemAdmin']],
    ['User',  ['SystemAdmin']],
    ['Staff', ['SystemAdmin']],
    ['OrgAdmin', ['SystemAdmin']],
  ])('redirects role %s to /access-denied for restricted route', (role, allowedRoles) => {
    mockIsAuthenticated.mockReturnValue(true)
    mockGetTokenPayload.mockReturnValue({
      sub: '1', email: 'a@b.com', name: 'A', role,
      tid: TENANT, exp: 9999999999, iat: 0,
    })
    renderGuard(`/dashboard/${TENANT}`, allowedRoles)
    expect(screen.getByText('Access Denied Page')).toBeInTheDocument()
    expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
  })
})
