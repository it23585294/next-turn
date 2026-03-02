/**
 * Tests for AdminDashboardPage (/admin/:tenantId).
 *
 * Strategy:
 *  - Mock createQueue and getOrgQueues so we control API outcomes
 *  - Mock getTokenPayload so the component always sees a valid OrgAdmin payload
 *  - Render inside MemoryRouter so useNavigate/useParams work correctly
 *
 * Coverage:
 *  1.  Renders the create-queue form (fields + submit button)
 *  2.  Calls getOrgQueues on mount with the tenant ID from the route
 *  3.  Shows the queue list when getOrgQueues resolves with data
 *  4.  Shows empty-state text when getOrgQueues returns an empty array
 *  5.  Shows load error banner when getOrgQueues rejects
 *  6.  Validate: submitting with an empty name shows a field-level error
 *  7.  Happy path: submit valid form → shows new-link-banner with shareable link
 *  8.  Happy path: newly created queue appears at the top of the queue list
 *  9.  API error: createQueue 422 → shows the first validation message
 *  10. API error: createQueue generic failure → shows detail message
 */
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { AdminDashboardPage } from '../AdminDashboardPage'
import * as queuesApi from '../../../api/queues'
import type { CreateQueueResult, OrgQueueSummary } from '../../../api/queues'

// ---------------------------------------------------------------------------
// Module mocks
// ---------------------------------------------------------------------------

// Preserve type exports from queues module; stub only the network functions.
vi.mock('../../../api/queues', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api/queues')>()
  return { ...actual, createQueue: vi.fn(), getOrgQueues: vi.fn() }
})

// getTokenPayload must return a valid payload; otherwise the component redirects to '/'.
vi.mock('../../../utils/authToken', () => ({
  getTokenPayload: vi.fn(() => ({
    sub:   'admin-user-id',
    email: 'admin@nextturn.dev',
    name:  'Test Admin',
    role:  'OrgAdmin',
    tid:   TENANT_ID,
    exp:   Math.floor(Date.now() / 1000) + 3600,
    iat:   Math.floor(Date.now() / 1000),
  })),
  clearToken: vi.fn(),
  getToken:   vi.fn(() => 'test-jwt-token'),
}))

// Navigator clipboard is not implemented in jsdom — provide a stub.
// configurable: true is required so that userEvent.setup() can re-wrap the
// property internally without throwing "Cannot redefine property: clipboard".
beforeAll(() => {
  Object.defineProperty(navigator, 'clipboard', {
    value:        { writeText: vi.fn().mockResolvedValue(undefined) },
    writable:     true,
    configurable: true,
  })
})

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const QUEUE_ID  = 'cccccccc-cccc-cccc-cccc-cccccccccccc'

const SAMPLE_CREATE_RESULT: CreateQueueResult = {
  queueId:       QUEUE_ID,
  shareableLink: `/queues/${TENANT_ID}/${QUEUE_ID}`,
}

const SAMPLE_QUEUE: OrgQueueSummary = {
  queueId:                  QUEUE_ID,
  name:                     'Main Counter',
  maxCapacity:               50,
  averageServiceTimeSeconds: 300,
  status:                    'Active',
  shareableLink:             `/queues/${TENANT_ID}/${QUEUE_ID}`,
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const mockCreateQueue  = vi.mocked(queuesApi.createQueue)
const mockGetOrgQueues = vi.mocked(queuesApi.getOrgQueues)

function makeApiError(
  status: number,
  extra: Record<string, unknown> = {}
) {
  return { status, detail: extra.detail as string | undefined, errors: extra.errors, raw: extra }
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={[`/admin/${TENANT_ID}`]}>
      <Routes>
        <Route path="/admin/:tenantId" element={<AdminDashboardPage />} />
      </Routes>
    </MemoryRouter>
  )
}

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------

beforeEach(() => {
  mockCreateQueue.mockReset()
  mockGetOrgQueues.mockReset()
  // Default: empty queue list; most tests override as needed.
  mockGetOrgQueues.mockResolvedValue([])
})

// ---------------------------------------------------------------------------
// 1. Form presence
// ---------------------------------------------------------------------------

describe('AdminDashboardPage — form', () => {
  it('renders the Queue Name field', () => {
    renderPage()
    expect(screen.getByLabelText(/queue name/i)).toBeInTheDocument()
  })

  it('renders the Max Capacity field', () => {
    renderPage()
    expect(screen.getByLabelText(/max capacity/i)).toBeInTheDocument()
  })

  it('renders the Avg. Service Time field', () => {
    renderPage()
    expect(screen.getByLabelText(/avg\. service time/i)).toBeInTheDocument()
  })

  it('renders the Create Queue submit button', () => {
    renderPage()
    expect(screen.getByTestId('create-queue-btn')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 2–5. Queue list loading
// ---------------------------------------------------------------------------

describe('AdminDashboardPage — loading queues on mount', () => {
  it('calls getOrgQueues with the tenantId from the route', async () => {
    renderPage()
    await waitFor(() =>
      expect(mockGetOrgQueues).toHaveBeenCalledWith(TENANT_ID)
    )
  })

  it('shows empty-state text when no queues exist', async () => {
    mockGetOrgQueues.mockResolvedValue([])

    renderPage()

    expect(
      await screen.findByText(/no queues yet/i)
    ).toBeInTheDocument()
  })

  it('shows the queue list when queues are returned', async () => {
    mockGetOrgQueues.mockResolvedValue([SAMPLE_QUEUE])

    renderPage()

    expect(await screen.findByTestId('queue-list')).toBeInTheDocument()
    expect(screen.getByText('Main Counter')).toBeInTheDocument()
  })

  it('shows a queue card for each returned queue', async () => {
    const second: OrgQueueSummary = { ...SAMPLE_QUEUE, queueId: 'dddd-dddd', name: 'VIP Lane' }
    mockGetOrgQueues.mockResolvedValue([SAMPLE_QUEUE, second])

    renderPage()

    const cards = await screen.findAllByTestId('queue-card')
    expect(cards).toHaveLength(2)
  })

  it('shows a load error banner when getOrgQueues rejects', async () => {
    mockGetOrgQueues.mockRejectedValue(new Error('network error'))

    renderPage()

    expect(
      await screen.findByText(/could not load queues/i)
    ).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 6. Client-side validation
// ---------------------------------------------------------------------------

describe('AdminDashboardPage — form validation', () => {
  it('shows a field error when name is empty on submit', async () => {
    const user = userEvent.setup()
    renderPage()

    // Clear the name field (it starts empty) and submit
    const nameInput = screen.getByLabelText(/queue name/i)
    await user.clear(nameInput)
    await user.click(screen.getByTestId('create-queue-btn'))

    expect(await screen.findByText(/queue name is required/i)).toBeInTheDocument()
  })

  it('does not call createQueue when validation fails', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.clear(screen.getByLabelText(/queue name/i))
    await user.click(screen.getByTestId('create-queue-btn'))

    await screen.findByText(/queue name is required/i)
    expect(mockCreateQueue).not.toHaveBeenCalled()
  })
})

// ---------------------------------------------------------------------------
// 7–8. Happy path: create queue
// ---------------------------------------------------------------------------

describe('AdminDashboardPage — successful queue creation', () => {
  beforeEach(() => {
    mockCreateQueue.mockResolvedValue(SAMPLE_CREATE_RESULT)
  })

  it('shows the shareable link banner after successful creation', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/queue name/i), 'Main Counter')
    await user.click(screen.getByTestId('create-queue-btn'))

    expect(await screen.findByTestId('new-link-banner')).toBeInTheDocument()
  })

  it('displays the shareable link in the success banner', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/queue name/i), 'Main Counter')
    await user.click(screen.getByTestId('create-queue-btn'))

    const banner = await screen.findByTestId('new-link-banner')
    // window.location.origin in jsdom is 'http://localhost'
    expect(within(banner).getByText(
      new RegExp(SAMPLE_CREATE_RESULT.shareableLink.replace('/', '\\/'))
    )).toBeInTheDocument()
  })

  it('adds the new queue to the queue list after creation', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.type(screen.getByLabelText(/queue name/i), 'Main Counter')
    await user.click(screen.getByTestId('create-queue-btn'))

    await screen.findByTestId('new-link-banner')

    // The queue list should now contain the newly created queue.
    expect(await screen.findByTestId('queue-list')).toBeInTheDocument()
    expect(screen.getByText('Main Counter')).toBeInTheDocument()
  })

  it('resets the form name field after successful creation', async () => {
    const user = userEvent.setup()
    renderPage()

    const nameInput = screen.getByLabelText(/queue name/i) as HTMLInputElement
    await user.type(nameInput, 'Main Counter')
    await user.click(screen.getByTestId('create-queue-btn'))

    await screen.findByTestId('new-link-banner')
    expect(nameInput.value).toBe('')
  })

  it('calls createQueue with the correct tenant ID and body', async () => {
    const user = userEvent.setup()
    renderPage()

    // Clear capacity and set known values for precise assertion
    const capacityInput = screen.getByLabelText(/max capacity/i)
    await user.clear(capacityInput)
    await user.type(capacityInput, '20')

    const avgTimeInput = screen.getByLabelText(/avg\. service time/i)
    await user.clear(avgTimeInput)
    await user.type(avgTimeInput, '120')

    await user.type(screen.getByLabelText(/queue name/i), 'Triage')
    await user.click(screen.getByTestId('create-queue-btn'))

    await screen.findByTestId('new-link-banner')

    expect(mockCreateQueue).toHaveBeenCalledWith(TENANT_ID, {
      name:                      'Triage',
      maxCapacity:               20,
      averageServiceTimeSeconds: 120,
    })
  })
})

// ---------------------------------------------------------------------------
// 9–10. API error paths
// ---------------------------------------------------------------------------

describe('AdminDashboardPage — create queue errors', () => {
  it('shows the first validation error message on 422', async () => {
    const user = userEvent.setup()
    mockCreateQueue.mockRejectedValue(
      makeApiError(422, { errors: { Name: ['Queue name must not exceed 200 characters.'] } })
    )

    renderPage()

    await user.type(screen.getByLabelText(/queue name/i), 'Counter')
    await user.click(screen.getByTestId('create-queue-btn'))

    expect(
      await screen.findByText(/queue name must not exceed 200 characters/i)
    ).toBeInTheDocument()
  })

  it('shows the API detail message on a generic error', async () => {
    const user = userEvent.setup()
    mockCreateQueue.mockRejectedValue(
      makeApiError(400, { detail: 'Organisation not found.' })
    )

    renderPage()

    await user.type(screen.getByLabelText(/queue name/i), 'Counter')
    await user.click(screen.getByTestId('create-queue-btn'))

    expect(await screen.findByTestId('create-error')).toBeInTheDocument()
    expect(screen.getByText(/organisation not found/i)).toBeInTheDocument()
  })
})
