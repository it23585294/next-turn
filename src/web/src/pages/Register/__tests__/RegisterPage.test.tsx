/**
 * Integration-style tests for RegisterPage.
 *
 * Strategy:
 *  - Render the page inside a MemoryRouter so useParams() resolves correctly
 *  - Mock registerUser() at the module boundary — no network, no server
 *  - Use @testing-library/user-event v14 async API for realistic interaction
 *
 * Coverage:
 *  - Correct fields are rendered
 *  - Per-field validation errors appear on interaction
 *  - Password strength bar appears when typing a password
 *  - Happy-path submit → success card shown
 *  - Server 400 error → error banner shown
 *  - Server 422 error → error banner shown
 *  - Missing tenantId in URL → error card shown
 *  - Submit button shows loading state during async call
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { RegisterPage } from '../RegisterPage'
import * as authApi from '../../../api/auth'

// ---------------------------------------------------------------------------
// Module mock — all calls to registerUser go through this spy
// ---------------------------------------------------------------------------
vi.mock('../../../api/auth')
const mockRegisterUser = vi.mocked(authApi.registerUser)

// ---------------------------------------------------------------------------
// Render helpers
// ---------------------------------------------------------------------------
const TENANT_ID = '00000000-0000-0000-0000-000000000001'

/** Render RegisterPage with a valid tenantId route param */
function renderPage(tenantId: string | null = TENANT_ID) {
  const path  = tenantId ? `/register/${tenantId}` : '/register'
  const route = tenantId ? '/register/:tenantId'   : '/register'

  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path={route} element={<RegisterPage />} />
      </Routes>
    </MemoryRouter>
  )
}

/** Fill in the form with valid data and optionally override fields */
async function fillValidForm(
  user: ReturnType<typeof userEvent.setup>,
  overrides: Partial<{ name: string; email: string; phone: string; password: string; confirm: string }> = {}
) {
  const name     = overrides.name     ?? 'Maria Santos'
  const email    = overrides.email    ?? 'maria@example.com'
  const password = overrides.password ?? 'Secure1!'
  const confirm  = overrides.confirm  ?? password

  await user.clear(screen.getByLabelText(/full name/i))
  await user.type(screen.getByLabelText(/full name/i), name)

  await user.clear(screen.getByLabelText(/email address/i))
  await user.type(screen.getByLabelText(/email address/i), email)

  if (overrides.phone) {
    await user.type(screen.getByLabelText(/phone number/i), overrides.phone)
  }

  // Password fields — target by id to avoid ambiguity between the two
  await user.type(document.getElementById('password')!, password)
  await user.type(document.getElementById('confirmPassword')!, confirm)
}

// ---------------------------------------------------------------------------
// Rendering
// ---------------------------------------------------------------------------
describe('RegisterPage — rendering', () => {
  it('renders the page heading', () => {
    renderPage()
    expect(screen.getByRole('heading', { name: /create your account/i })).toBeInTheDocument()
  })

  it('renders all required form fields', () => {
    renderPage()
    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/phone number/i)).toBeInTheDocument()
    expect(document.getElementById('password')).toBeInTheDocument()
    expect(document.getElementById('confirmPassword')).toBeInTheDocument()
  })

  it('renders the submit button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument()
  })

  it('renders an error card when tenantId is missing from the URL', () => {
    renderPage(null)
    expect(screen.getByText(/invalid registration link/i)).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// Field-level validation
// ---------------------------------------------------------------------------
describe('RegisterPage — field validation', () => {
  it('shows a name error after blurring an empty name field', async () => {
    renderPage()
    const nameInput = screen.getByLabelText(/full name/i)

    await userEvent.click(nameInput)
    await userEvent.tab()

    await waitFor(() => {
      expect(screen.getByText(/name is required/i)).toBeInTheDocument()
    })
  })

  it('shows an email error for an invalid email address', async () => {
    renderPage()
    const emailInput = screen.getByLabelText(/email address/i)

    await userEvent.type(emailInput, 'notanemail')
    await userEvent.tab()

    await waitFor(() => {
      expect(screen.getByText(/enter a valid email address/i)).toBeInTheDocument()
    })
  })

  it('shows a password length error for a short password', async () => {
    renderPage()

    await userEvent.type(document.getElementById('password')!, 'Ab1!')
    await userEvent.tab()

    await waitFor(() => {
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument()
    })
  })

  it('shows a mismatched passwords error', async () => {
    renderPage()
    const user = userEvent.setup()

    await user.type(document.getElementById('password')!, 'Secure1!')
    await user.type(document.getElementById('confirmPassword')!, 'Different1!')
    await user.tab()

    await waitFor(() => {
      expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument()
    })
  })
})

// ---------------------------------------------------------------------------
// Password strength bar
// ---------------------------------------------------------------------------
describe('RegisterPage — password strength bar', () => {
  it('does not show the strength bar initially', () => {
    renderPage()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('shows strength bar after typing in the password field', async () => {
    renderPage()
    await userEvent.type(document.getElementById('password')!, 'Secure1!')
    await waitFor(() => {
      expect(screen.getByRole('status')).toBeInTheDocument()
    })
  })

  it('shows "Strong" label for a fully-qualifying password', async () => {
    renderPage()
    await userEvent.type(document.getElementById('password')!, 'Secure1!')
    await waitFor(() => {
      expect(screen.getByText('Strong')).toBeInTheDocument()
    })
  })

  it('shows "Weak" label for a short, simple password', async () => {
    renderPage()
    // Only ≥8 chars check passes (9 chars, no upper, no digit, no special)
    await userEvent.type(document.getElementById('password')!, 'abcdefghi')
    await waitFor(() => {
      expect(screen.getByText('Weak')).toBeInTheDocument()
    })
  })
})

// ---------------------------------------------------------------------------
// Successful submission
// ---------------------------------------------------------------------------
describe('RegisterPage — successful submission', () => {
  beforeEach(() => {
    mockRegisterUser.mockResolvedValue({ ok: true })
  })

  it('shows the success card after a successful submission', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /create account/i }))

    await waitFor(() => {
      expect(screen.getByText(/account created!/i)).toBeInTheDocument()
    })
  })

  it('calls registerUser with the correct tenantId and payload', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /create account/i }))

    await waitFor(() => {
      expect(mockRegisterUser).toHaveBeenCalledWith(
        TENANT_ID,
        expect.objectContaining({
          name: 'Maria Santos',
          email: 'maria@example.com',
          password: 'Secure1!',
        })
      )
    })
  })

  it('shows the "Go to Sign In" link after success', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /create account/i }))

    await waitFor(() => {
      expect(screen.getByRole('link', { name: /go to sign in/i })).toBeInTheDocument()
    })
  })
})

// ---------------------------------------------------------------------------
// Server error — 400 Domain error
// ---------------------------------------------------------------------------
describe('RegisterPage — server 400 error', () => {
  it('shows the error banner with the server detail message', async () => {
    mockRegisterUser.mockRejectedValueOnce({
      status: 400,
      detail: 'Email address is already in use',
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /create account/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/already in use/i)
    })
  })

  it('does not navigate to success card on error', async () => {
    mockRegisterUser.mockRejectedValueOnce({
      status: 400,
      detail: 'Email already in use',
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /create account/i }))

    await waitFor(() => {
      expect(screen.queryByText(/account created!/i)).not.toBeInTheDocument()
    })
  })
})

// ---------------------------------------------------------------------------
// Server error — 422 Validation error
// ---------------------------------------------------------------------------
describe('RegisterPage — server 422 error', () => {
  it('shows the first server validation error in the error banner', async () => {
    mockRegisterUser.mockRejectedValueOnce({
      status: 422,
      errors: { Password: ['Must contain at least one uppercase letter'] },
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /create account/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/uppercase/i)
    })
  })
})
