/**
 * Integration-style tests for OrgRegistrationPage.
 *
 * Strategy:
 *  - Render inside MemoryRouter at /register-org (no route param — org doesn't exist yet)
 *  - Mock registerOrganisation() at the module boundary — no network, no server
 *  - Use @testing-library/user-event v14 async API for realistic interaction
 *
 * Coverage:
 *  - All form fields and submit button are rendered
 *  - Empty submission triggers Zod inline errors for all required fields
 *  - Invalid email format shows email field error
 *  - Happy path → API called with correct payload → success card shown
 *  - Success card contains "pending approval" copy
 *  - API 400 → error banner shown, form stays visible
 *  - API 409 → conflict-specific error banner shown
 *  - Generic API error → fallback error banner shown
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { OrgRegistrationPage } from '../OrgRegistrationPage'
import * as orgsApi from '../../../api/organisations'

// ── Module mock ───────────────────────────────────────────────────────────────
// Preserve the real toOrgRegistrationPayload; only stub registerOrganisation.

vi.mock('../../../api/organisations', async (importOriginal) => {
  const actual = await importOriginal<typeof orgsApi>()
  return { ...actual, registerOrganisation: vi.fn() }
})
const mockRegisterOrganisation = vi.mocked(orgsApi.registerOrganisation)

// ── Render helper ─────────────────────────────────────────────────────────────

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/register-org']}>
      <Routes>
        <Route path="/register-org" element={<OrgRegistrationPage />} />
      </Routes>
    </MemoryRouter>
  )
}

// ── Form fill helper ──────────────────────────────────────────────────────────

async function fillValidForm(
  user: ReturnType<typeof userEvent.setup>,
  overrides: Partial<{
    orgName: string
    orgType: string
    addressLine1: string
    city: string
    postalCode: string
    country: string
    adminName: string
    adminEmail: string
  }> = {}
) {
  const {
    orgName      = 'Acme Corp',
    orgType      = 'Healthcare',
    addressLine1 = '123 Main Street',
    city         = 'London',
    postalCode   = 'SW1A 1AA',
    country      = 'United Kingdom',
    adminName    = 'Jane Smith',
    adminEmail   = 'admin@acme.com',
  } = overrides

  await user.type(screen.getByLabelText(/organisation name/i),  orgName)
  await user.selectOptions(screen.getByLabelText(/organisation type/i), orgType)
  await user.type(screen.getByLabelText(/address line 1/i),     addressLine1)
  await user.type(screen.getByLabelText(/^city/i),              city)
  await user.type(screen.getByLabelText(/postal code/i),        postalCode)
  await user.type(screen.getByLabelText(/^country/i),           country)
  await user.type(screen.getByLabelText(/admin full name/i),    adminName)
  await user.type(screen.getByLabelText(/admin email/i),        adminEmail)
}

// ── Rendering ─────────────────────────────────────────────────────────────────

describe('OrgRegistrationPage — rendering', () => {
  it('renders the page heading', () => {
    renderPage()
    expect(
      screen.getByRole('heading', { name: /register your organisation/i })
    ).toBeInTheDocument()
  })

  it('renders all eight form fields', () => {
    renderPage()
    expect(screen.getByLabelText(/organisation name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/organisation type/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/address line 1/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^city/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/postal code/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^country/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/admin full name/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/admin email/i)).toBeInTheDocument()
  })

  it('renders the submit button', () => {
    renderPage()
    expect(
      screen.getByRole('button', { name: /register organisation/i })
    ).toBeInTheDocument()
  })

  it('renders the org type select with all five options', () => {
    renderPage()
    const select = screen.getByLabelText(/organisation type/i)
    const opts = Array.from((select as HTMLSelectElement).options)
      .filter((o) => o.value !== '')
      .map((o) => o.value)

    expect(opts).toEqual(['Healthcare', 'Retail', 'Government', 'Education', 'Other'])
  })
})

// ── Field-level validation (empty submit) ─────────────────────────────────────

describe('OrgRegistrationPage — field validation on empty submit', () => {
  it('shows organisation name required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/organisation name is required/i)).toBeInTheDocument()
    })
  })

  it('shows address line 1 required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/address line 1 is required/i)).toBeInTheDocument()
    })
  })

  it('shows city required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/city is required/i)).toBeInTheDocument()
    })
  })

  it('shows postal code required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/postal code is required/i)).toBeInTheDocument()
    })
  })

  it('shows country required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/country is required/i)).toBeInTheDocument()
    })
  })

  it('shows admin name required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/admin name is required/i)).toBeInTheDocument()
    })
  })

  it('shows admin email required error', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/admin email is required/i)).toBeInTheDocument()
    })
  })

  it('does not call the API when fields are invalid', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/organisation name is required/i)).toBeInTheDocument()
    })
    expect(mockRegisterOrganisation).not.toHaveBeenCalled()
  })
})

// ── Email format validation ───────────────────────────────────────────────────

describe('OrgRegistrationPage — email format validation', () => {
  it('shows email format error after blurring an invalid email', async () => {
    const user = userEvent.setup()
    renderPage()

    const emailInput = screen.getByLabelText(/admin email/i)
    await user.type(emailInput, 'not-an-email')
    await user.tab()

    await waitFor(() => {
      expect(screen.getByText(/valid email/i)).toBeInTheDocument()
    })
  })

  it('does not show email error for a valid email', async () => {
    const user = userEvent.setup()
    renderPage()

    const emailInput = screen.getByLabelText(/admin email/i)
    await user.type(emailInput, 'admin@example.com')
    await user.tab()

    await waitFor(() => {
      expect(screen.queryByText(/valid email/i)).not.toBeInTheDocument()
    })
  })
})

// ── Happy path ────────────────────────────────────────────────────────────────

describe('OrgRegistrationPage — successful submission', () => {
  beforeEach(() => {
    mockRegisterOrganisation.mockResolvedValue({
      organisationId: '11111111-1111-1111-1111-111111111111',
      adminUserId:    '22222222-2222-2222-2222-222222222222',
    })
  })

  it('calls registerOrganisation with the correct payload', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(mockRegisterOrganisation).toHaveBeenCalledWith(
        expect.objectContaining({
          orgName:      'Acme Corp',
          orgType:      'Healthcare',
          addressLine1: '123 Main Street',
          city:         'London',
          postalCode:   'SW1A 1AA',
          country:      'United Kingdom',
          adminName:    'Jane Smith',
          adminEmail:   'admin@acme.com',
        })
      )
    })
  })

  it('shows the success card after successful submission', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/registration submitted/i)).toBeInTheDocument()
    })
  })

  it('success card contains "pending approval" text', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByText(/pending approval/i)).toBeInTheDocument()
    })
  })

  it('hides the form after successful submission', async () => {
    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /register organisation/i })).not.toBeInTheDocument()
    })
  })
})

// ── API 400 — domain error ────────────────────────────────────────────────────

describe('OrgRegistrationPage — API 400 error', () => {
  beforeEach(() => {
    mockRegisterOrganisation.mockResolvedValue({
      organisationId: '11111111-1111-1111-1111-111111111111',
      adminUserId:    '22222222-2222-2222-2222-222222222222',
    })
  })

  it('shows the server detail message in the error banner', async () => {
    mockRegisterOrganisation.mockRejectedValueOnce({
      status: 400,
      detail: 'Registration failed due to a domain rule.',
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/domain rule/i)
    })
  })

  it('keeps the form visible after a 400 error', async () => {
    mockRegisterOrganisation.mockRejectedValueOnce({
      status: 400,
      detail: 'Some domain error.',
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
    expect(screen.queryByText(/registration submitted/i)).not.toBeInTheDocument()
  })
})

// ── API 409 — conflict ────────────────────────────────────────────────────────

describe('OrgRegistrationPage — API 409 conflict', () => {
  beforeEach(() => {
    mockRegisterOrganisation.mockResolvedValue({
      organisationId: '11111111-1111-1111-1111-111111111111',
      adminUserId:    '22222222-2222-2222-2222-222222222222',
    })
  })

  it('shows a conflict-specific error banner', async () => {
    mockRegisterOrganisation.mockRejectedValueOnce({
      status: 409,
      detail: "An organisation named 'Acme Corp' is already registered.",
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/already registered/i)
    })
  })

  it('falls back to the default conflict message when detail is absent', async () => {
    mockRegisterOrganisation.mockRejectedValueOnce({
      status: 409,
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/already registered/i)
    })
  })
})

// ── Generic / network error ───────────────────────────────────────────────────

describe('OrgRegistrationPage — generic/network error', () => {
  beforeEach(() => {
    mockRegisterOrganisation.mockResolvedValue({
      organisationId: '11111111-1111-1111-1111-111111111111',
      adminUserId:    '22222222-2222-2222-2222-222222222222',
    })
  })

  it('shows a generic error banner for an unexpected error status', async () => {
    mockRegisterOrganisation.mockRejectedValueOnce({
      status: 503,
      detail: 'Service temporarily unavailable.',
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/temporarily unavailable/i)
    })
  })

  it('shows a fallback message when detail is absent', async () => {
    mockRegisterOrganisation.mockRejectedValueOnce({
      status: 500,
      raw: {},
    })

    const user = userEvent.setup()
    renderPage()

    await fillValidForm(user)
    await user.click(screen.getByRole('button', { name: /register organisation/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/something went wrong/i)
    })
  })
})
