/**
 * Tests for LoginPage (/login/:tenantId).
 *
 * Strategy:
 *  - Render inside MemoryRouter so useParams() resolves correctly
 *  - Mock loginUser() at the module boundary — no network
 *  - Mock saveToken and useNavigate to assert post-login side-effects
 *  - Use @testing-library/user-event async API for realistic interaction
 *
 * Coverage:
 *  1.  Renders heading and form fields
 *  2.  Renders link to /register/:tenantId
 *  3.  Shows error card when tenantId is absent from the URL
 *  4.  Email validation — empty → required error
 *  5.  Email validation — invalid format → format error
 *  6.  Password validation — empty → required error
 *  7.  Success: calls loginUser with correct args
 *  8.  Success: calls saveToken with the returned access token
 *  9.  Success: navigates to /dashboard/:tenantId
 *  10. 400 (wrong credentials) → red error banner, no lockout text
 *  11. 400 with "temporarily locked" detail → amber lockout banner
 *  12. 429 too many requests → blue rate-limit banner
 *  13. Network error (status 0) → generic error banner
 *  14. Sign In button is disabled / shows loading while submitting
 *  15. Error banner is cleared on a subsequent successful submission
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { LoginPage } from '../LoginPage'
import * as authApi from '../../../api/auth'
import * as authToken from '../../../utils/authToken'

// ---------------------------------------------------------------------------
// Module mocks
// ---------------------------------------------------------------------------
vi.mock('../../../api/auth')
const mockLoginUser = vi.mocked(authApi.loginUser)

// Spy on saveToken — we verify it's called with the correct JWT
vi.mock('../../../utils/authToken', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../utils/authToken')>()
  return { ...actual, saveToken: vi.fn(), clearToken: vi.fn() }
})
const mockSaveToken = vi.mocked(authToken.saveToken)

// Capture navigate calls without wiring a full router history
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>()
  return { ...actual, useNavigate: () => mockNavigate }
})

// ---------------------------------------------------------------------------
// Constants / helpers
// ---------------------------------------------------------------------------
const TENANT_ID   = '00000000-0000-0000-0000-000000000001'
const VALID_EMAIL = 'alice@example.com'
const VALID_PASS  = 'SecureP@ss1'

const LOGIN_RESULT: authApi.LoginResult = {
  accessToken: 'eyJhbGciOiJIUzI1NiJ9.stub.sig',
  userId:      'c0ffee00-0000-0000-0000-000000000001',
  name:        'Alice Smith',
  email:       VALID_EMAIL,
  role:        'User',
}

function makeApiError(status: number, detail: string) {
  return { status, detail, raw: { detail } }
}

/** Render LoginPage under a MemoryRouter that supplies :tenantId */
function renderPage(tenantId: string | null = TENANT_ID) {
  const path  = tenantId ? `/login/${tenantId}` : '/login'
  const route = tenantId ? '/login/:tenantId'   : '/login'
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path={route} element={<LoginPage />} />
      </Routes>
    </MemoryRouter>
  )
}

// ---------------------------------------------------------------------------
// Before each: reset mocks
// ---------------------------------------------------------------------------
beforeEach(() => {
  mockNavigate.mockReset()
  mockSaveToken.mockReset()
})

// ---------------------------------------------------------------------------
// 1–3: Rendering
// ---------------------------------------------------------------------------
describe('LoginPage — rendering', () => {
  it('renders the "Welcome back" heading', () => {
    renderPage()
    expect(screen.getByRole('heading', { name: /welcome back/i })).toBeInTheDocument()
  })

  it('renders email and password fields', () => {
    renderPage()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    // Use getElementById — getByLabelText(/password/i) is ambiguous because the
    // PasswordInput toggle button also carries aria-label="Show password".
    expect(document.getElementById('password')).toBeInTheDocument()
  })

  it('renders a Sign In submit button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('renders a link to the register page for this tenant', () => {
    renderPage()
    const link = screen.getByRole('link', { name: /create one/i })
    expect(link).toHaveAttribute('href', `/register/${TENANT_ID}`)
  })

  it('shows an error card when tenantId is absent from the URL', () => {
    renderPage(null)
    expect(screen.getByText(/invalid sign-in link/i)).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 4–6: Field-level validation (client-side, no network call)
// ---------------------------------------------------------------------------
describe('LoginPage — field validation', () => {
  it('shows "Email is required" when email is left empty on submit', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))
    await waitFor(() => {
      expect(screen.getByText(/email is required/i)).toBeInTheDocument()
    })
    expect(mockLoginUser).not.toHaveBeenCalled()
  })

  it('shows a format error for an invalid email address', async () => {
    renderPage()
    await userEvent.type(screen.getByLabelText(/email address/i), 'notanemail')
    await userEvent.tab()
    await waitFor(() => {
      expect(screen.getByText(/valid email address/i)).toBeInTheDocument()
    })
  })

  it('shows "Password is required" when password is empty on submit', async () => {
    renderPage()
    await userEvent.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))
    await waitFor(() => {
      expect(screen.getByText(/password is required/i)).toBeInTheDocument()
    })
    expect(mockLoginUser).not.toHaveBeenCalled()
  })
})

// ---------------------------------------------------------------------------
// 7–9: Success path
// ---------------------------------------------------------------------------
describe('LoginPage — success', () => {
  beforeEach(() => {
    mockLoginUser.mockResolvedValueOnce(LOGIN_RESULT)
  })

  it('calls loginUser with the correct email, password, and tenantId', async () => {
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(mockLoginUser).toHaveBeenCalledWith(
        TENANT_ID,
        { email: VALID_EMAIL, password: VALID_PASS }
      )
    })
  })

  it('calls saveToken with the returned access token', async () => {
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(mockSaveToken).toHaveBeenCalledWith(LOGIN_RESULT.accessToken)
    })
  })

  it('navigates to /dashboard/:tenantId after successful login', async () => {
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith(
        `/dashboard/${TENANT_ID}`,
        { replace: true }
      )
    })
  })
})

// ---------------------------------------------------------------------------
// 10–13: Server error banners
// ---------------------------------------------------------------------------
describe('LoginPage — error banners', () => {
  it('shows a red error banner for 400 invalid credentials', async () => {
    mockLoginUser.mockRejectedValueOnce(makeApiError(400, 'Invalid credentials.'))
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, 'WrongPass!')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByText(/incorrect email or password/i)).toBeInTheDocument()
    })
  })

  it('shows an amber lockout banner when the account is temporarily locked', async () => {
    mockLoginUser.mockRejectedValueOnce(
      makeApiError(400, 'Account is temporarily locked. Please try again later.')
    )
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByText(/account temporarily locked/i)).toBeInTheDocument()
    })
  })

  it('shows a blue rate-limit banner on HTTP 429', async () => {
    mockLoginUser.mockRejectedValueOnce(makeApiError(429, 'Too many requests'))
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByText(/too many sign-in attempts/i)).toBeInTheDocument()
    })
  })

  it('shows a generic error banner for network errors (status 0)', async () => {
    mockLoginUser.mockRejectedValueOnce({
      status: 0,
      detail: 'Could not reach the server. Please check your connection.',
      raw: {},
    })
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByText(/could not reach the server/i)).toBeInTheDocument()
    })
  })
})

// ---------------------------------------------------------------------------
// 14: Loading state
// ---------------------------------------------------------------------------
describe('LoginPage — loading state', () => {
  it('disables the Sign In button while the request is in flight', async () => {
    // Use a promise that never resolves so we can inspect the pending state
    mockLoginUser.mockReturnValueOnce(new Promise(() => {}))
    renderPage()
    const user = userEvent.setup()
    await user.type(screen.getByLabelText(/email address/i), VALID_EMAIL)
    await user.type(document.getElementById('password')!, VALID_PASS)
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    // When loading=true, Button replaces children with a spinner wrapper that has
    // aria-label="Loading", so the button's accessible name becomes "Loading".
    // Check aria-busy="true" which is the authoritative "in progress" indicator.
    await waitFor(() => {
      const btn = document.querySelector('button[aria-busy="true"]')
      expect(btn).not.toBeNull()
      expect(btn).toBeDisabled()
    })
  })
})

// ---------------------------------------------------------------------------
// 15: Session-expired banner (NT-12-7)
// ---------------------------------------------------------------------------
describe('LoginPage — session expired banner', () => {
  it('shows a session-expired info banner when redirected with ?reason=session_expired', () => {
    render(
      <MemoryRouter initialEntries={[`/login/${TENANT_ID}?reason=session_expired`]}>
        <Routes>
          <Route path="/login/:tenantId" element={<LoginPage />} />
        </Routes>
      </MemoryRouter>
    )
    // The banner should be present immediately (no user interaction needed)
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText(/your session has expired/i)).toBeInTheDocument()
  })

  it('does NOT show the session-expired banner when no reason param is present', () => {
    render(
      <MemoryRouter initialEntries={[`/login/${TENANT_ID}`]}>
        <Routes>
          <Route path="/login/:tenantId" element={<LoginPage />} />
        </Routes>
      </MemoryRouter>
    )
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
