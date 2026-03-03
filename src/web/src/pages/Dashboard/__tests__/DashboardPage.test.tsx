/**
 * Tests for DashboardPage (/dashboard/:tenantId).
 *
 * Strategy:
 *  - Mock getTokenPayload so we can control what user info is shown
 *  - Mock clearToken and useNavigate to assert logout side-effects
 *  - Render DashboardPage directly (without ProtectedRoute) — the guard
 *    logic is tested separately in ProtectedRoute.test.tsx (NT-12+)
 *
 * Coverage:
 *  1. Renders welcome heading with user's first name
 *  2. Renders user's email in the welcome banner
 *  3. Renders the "User" role badge for a regular user
 *  4. Renders non-default role badge (Staff)
 *  5. Renders all four placeholder feature cards
 *  6. Renders the tenant ID chip
 *  7. Logout button calls clearToken and navigates to /
 *  8. Falls back gracefully when getTokenPayload returns null (clears token + navigates to /)
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { DashboardPage } from '../DashboardPage'
import * as authToken from '../../../utils/authToken'
import type { TokenPayload } from '../../../utils/authToken'

// ---------------------------------------------------------------------------
// Module mocks
// ---------------------------------------------------------------------------
vi.mock('../../../utils/authToken', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../utils/authToken')>()
  return {
    ...actual,
    getTokenPayload: vi.fn(),
    clearToken:      vi.fn(),
  }
})
const mockGetTokenPayload = vi.mocked(authToken.getTokenPayload)
const mockClearToken      = vi.mocked(authToken.clearToken)

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => mockNavigate }
})

// ---------------------------------------------------------------------------
// Constants / helpers
// ---------------------------------------------------------------------------
const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'

const SAMPLE_PAYLOAD: TokenPayload = {
  sub:   'c0ffee00-0000-0000-0000-000000000001',
  email: 'alice@example.com',
  name:  'Alice Smith',
  role:  'User',
  tid:   TENANT_ID,
  exp:   Math.floor(Date.now() / 1000) + 3600,
  iat:   Math.floor(Date.now() / 1000),
}

function renderPage(tenantId: string = TENANT_ID) {
  return render(
    <MemoryRouter initialEntries={[`/dashboard/${tenantId}`]}>
      <Routes>
        <Route path="/dashboard/:tenantId" element={<DashboardPage />} />
      </Routes>
    </MemoryRouter>
  )
}

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
beforeEach(() => {
  mockNavigate.mockReset()
  mockClearToken.mockReset()
  // Default: valid session token
  mockGetTokenPayload.mockReturnValue(SAMPLE_PAYLOAD)
})

// ---------------------------------------------------------------------------
// 1–2: Welcome banner
// ---------------------------------------------------------------------------
describe('DashboardPage — welcome banner', () => {
  it('shows "Welcome back, Alice!" using the first name from the token', () => {
    renderPage()
    expect(screen.getByText(/welcome back, alice!/i)).toBeInTheDocument()
  })

  it("renders the user's email address", () => {
    renderPage()
    expect(screen.getByText(SAMPLE_PAYLOAD.email)).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 3–4: Role badge
// ---------------------------------------------------------------------------
describe('DashboardPage — role badge', () => {
  it('renders "User" role badge for a standard user token', () => {
    renderPage()
    // 'User' appears in both the role badge span and the auth-confirmed card (<strong>).
    // getAllByText accepts multiple matches — we just verify at least one is present.
    expect(screen.getAllByText('User').length).toBeGreaterThan(0)
  })

  it('renders "Staff" role badge for a staff token', () => {
    mockGetTokenPayload.mockReturnValue({ ...SAMPLE_PAYLOAD, role: 'Staff' })
    renderPage()
    expect(screen.getAllByText('Staff').length).toBeGreaterThan(0)
  })
})

// ---------------------------------------------------------------------------
// 5: Placeholder cards
// ---------------------------------------------------------------------------
describe('DashboardPage — placeholder cards', () => {
  it('renders all four Sprint 2 placeholder feature cards', () => {
    renderPage()
    expect(screen.getByText('My Queue')).toBeInTheDocument()
    expect(screen.getByText('Appointments')).toBeInTheDocument()
    expect(screen.getByText('Notifications')).toBeInTheDocument()
    expect(screen.getByText('Activity')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 6: Tenant chip
// ---------------------------------------------------------------------------
describe('DashboardPage — tenant chip', () => {
  it('displays the tenantId from the URL in the welcome banner', () => {
    renderPage()
    expect(screen.getByText(TENANT_ID)).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 7: Logout
// ---------------------------------------------------------------------------
describe('DashboardPage — logout', () => {
  it('calls clearToken when the Sign out button is clicked', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /sign out/i }))
    expect(mockClearToken).toHaveBeenCalledOnce()
  })

  it('navigates to / when the Sign out button is clicked', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /sign out/i }))
    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
    })
  })
})

// ---------------------------------------------------------------------------
// 8: Null payload fallback
// ---------------------------------------------------------------------------
describe('DashboardPage — null token fallback', () => {
  it('clears the token and navigates to / when getTokenPayload returns null', async () => {
    mockGetTokenPayload.mockReturnValue(null)
    renderPage()
    await waitFor(() => {
      expect(mockClearToken).toHaveBeenCalledOnce()
      expect(mockNavigate).toHaveBeenCalledWith('/', { replace: true })
    })
  })
})
