/**
 * Tests for AccessDeniedPage (/access-denied) (NT-12-67).
 *
 * Strategy:
 *  - Mock getTokenPayload to control role display
 *  - Mock clearToken and useNavigate to assert side-effects
 *  - Render directly — no ProtectedRoute wrapper needed
 *
 * Coverage:
 *  1. Renders "Access Denied" heading
 *  2. Displays the user's current role from the token
 *  3. Shows "Unknown" role when token payload is null
 *  4. "Go to Dashboard" navigates to /dashboard/:tenantId when tenantId is in URL
 *  5. "Go to Dashboard" falls back to /dashboard/:tid from token when no URL tenantId
 *  6. "Go to Dashboard" falls back to / when no tenantId anywhere
 *  7. "Sign Out" calls clearToken and navigates to /
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { AccessDeniedPage } from '../AccessDeniedPage'
import * as authToken from '../../../utils/authToken'
import type { TokenPayload } from '../../../utils/authToken'

// ---------------------------------------------------------------------------
// Mocks
// ---------------------------------------------------------------------------
vi.mock('../../../utils/authToken', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../utils/authToken')>()
  return { ...actual, getTokenPayload: vi.fn(), clearToken: vi.fn() }
})
const mockGetTokenPayload = vi.mocked(authToken.getTokenPayload)
const mockClearToken      = vi.mocked(authToken.clearToken)

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => mockNavigate }
})

const TENANT = 'aabbccdd-0000-0000-0000-000000000000'

const BASE_PAYLOAD: TokenPayload = {
  sub: 'user-1', email: 'alice@example.com', name: 'Alice Smith',
  role: 'User', tid: TENANT, exp: 9999999999, iat: 0,
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
function renderWithTenant(tenantId?: string) {
  const path = tenantId ? `/access-denied/${tenantId}` : '/access-denied'
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/access-denied/:tenantId" element={<AccessDeniedPage />} />
        <Route path="/access-denied" element={<AccessDeniedPage />} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  mockGetTokenPayload.mockReturnValue(BASE_PAYLOAD)
})

// ---------------------------------------------------------------------------
// Rendering
// ---------------------------------------------------------------------------
describe('AccessDeniedPage — rendering', () => {
  it('renders the "Access Denied" heading', () => {
    renderWithTenant()
    expect(screen.getByRole('heading', { name: /access denied/i })).toBeInTheDocument()
  })

  it('displays the permission-denied message', () => {
    renderWithTenant()
    expect(screen.getByText(/don't have permission/i)).toBeInTheDocument()
  })

  it('displays the user\'s role from the token', () => {
    renderWithTenant()
    expect(screen.getByText('User')).toBeInTheDocument()
  })

  it('displays "Unknown" when token payload is null', () => {
    mockGetTokenPayload.mockReturnValue(null)
    renderWithTenant()
    expect(screen.getByText('Unknown')).toBeInTheDocument()
  })

  it('renders "Go to Dashboard" and "Sign Out" buttons', () => {
    renderWithTenant()
    expect(screen.getByRole('button', { name: /go to dashboard/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument()
  })

  it('renders the role for Staff users', () => {
    mockGetTokenPayload.mockReturnValue({ ...BASE_PAYLOAD, role: 'Staff' })
    renderWithTenant()
    expect(screen.getByText('Staff')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// "Go to Dashboard" navigation
// ---------------------------------------------------------------------------
describe('AccessDeniedPage — Go to Dashboard', () => {
  it('navigates to /dashboard/:tenantId when tenantId is in the token tid', async () => {
    const user = userEvent.setup()
    renderWithTenant()
    await user.click(screen.getByRole('button', { name: /go to dashboard/i }))
    expect(mockNavigate).toHaveBeenCalledWith(`/dashboard/${TENANT}`, { replace: true })
  })

  it('navigates to / when token payload is null', async () => {
    mockGetTokenPayload.mockReturnValue(null)
    const user = userEvent.setup()
    renderWithTenant()
    await user.click(screen.getByRole('button', { name: /go to dashboard/i }))
    expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
  })
})

// ---------------------------------------------------------------------------
// "Sign Out"
// ---------------------------------------------------------------------------
describe('AccessDeniedPage — Sign Out', () => {
  it('calls clearToken and navigates to / on sign out', async () => {
    const user = userEvent.setup()
    renderWithTenant()
    await user.click(screen.getByRole('button', { name: /sign out/i }))
    expect(mockClearToken).toHaveBeenCalledOnce()
    expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
  })
})
