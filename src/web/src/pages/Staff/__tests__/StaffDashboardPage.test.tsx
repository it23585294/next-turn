import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'

import { StaffDashboardPage } from '../StaffDashboardPage'
import * as queuesApi from '../../../api/queues'

const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const QUEUE_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

vi.mock('../../../api/queues', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../../api/queues')>()
  return {
    ...actual,
    getAvailableQueues: vi.fn(),
    getQueueDashboard: vi.fn(),
    callNext: vi.fn(),
    markServed: vi.fn(),
    markNoShow: vi.fn(),
  }
})

vi.mock('../../../utils/authToken', () => ({
  getTokenPayload: vi.fn(() => ({
    sub: 'staff-user-id',
    email: 'staff@nextturn.dev',
    name: 'Test Staff',
    role: 'Staff',
    tid: TENANT_ID,
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  })),
  clearToken: vi.fn(),
  getToken: vi.fn(() => 'test-jwt-token'),
}))

const mockGetAvailableQueues = vi.mocked(queuesApi.getAvailableQueues)
const mockGetQueueDashboard = vi.mocked(queuesApi.getQueueDashboard)
const mockCallNext = vi.mocked(queuesApi.callNext)
const mockMarkServed = vi.mocked(queuesApi.markServed)

function renderPage() {
  return render(
    <MemoryRouter initialEntries={[`/staff/${TENANT_ID}`]}>
      <Routes>
        <Route path="/staff/:tenantId" element={<StaffDashboardPage />} />
      </Routes>
    </MemoryRouter>
  )
}

beforeEach(() => {
  mockGetAvailableQueues.mockReset()
  mockGetQueueDashboard.mockReset()
  mockCallNext.mockReset()
  mockMarkServed.mockReset()

  mockGetAvailableQueues.mockResolvedValue([
    {
      queueId: QUEUE_ID,
      name: 'Main Counter',
      maxCapacity: 50,
      averageServiceTimeSeconds: 180,
      status: 'Active',
      shareableLink: `/queues/${TENANT_ID}/${QUEUE_ID}`,
    },
  ])

  mockGetQueueDashboard.mockResolvedValue({
    queueId: QUEUE_ID,
    queueName: 'Main Counter',
    queueStatus: 'Active',
    waitingCount: 1,
    currentlyServing: null,
    waitingEntries: [
      {
        entryId: 'entry-1',
        ticketNumber: 1,
        joinedAt: '2026-03-01T08:00:00Z',
      },
    ],
  })

  mockCallNext.mockResolvedValue({
    entryId: 'entry-1',
    ticketNumber: 1,
    status: 'Serving',
  })

  mockMarkServed.mockResolvedValue({
    entryId: 'entry-1',
    ticketNumber: 1,
    status: 'Served',
  })
})

describe('StaffDashboardPage', () => {
  it('loads queues and dashboard for selected queue', async () => {
    renderPage()

    await waitFor(() => expect(mockGetAvailableQueues).toHaveBeenCalledWith(TENANT_ID))
    await waitFor(() => expect(mockGetQueueDashboard).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID))

    expect(await screen.findByText('Main Counter')).toBeInTheDocument()
    expect(screen.getByTestId('queue-selector')).toBeInTheDocument()
    expect(screen.getByTestId('waiting-list')).toBeInTheDocument()
  })

  it('calls next ticket and refreshes dashboard', async () => {
    mockGetQueueDashboard
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 1,
        currentlyServing: null,
        waitingEntries: [{ entryId: 'entry-1', ticketNumber: 1, joinedAt: '2026-03-01T08:00:00Z' }],
      })
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 0,
        currentlyServing: { entryId: 'entry-1', ticketNumber: 1, joinedAt: '2026-03-01T08:00:00Z' },
        waitingEntries: [],
      })

    const user = userEvent.setup()
    renderPage()

    const btn = await screen.findByTestId('call-next-btn')
    await user.click(btn)

    expect(mockCallNext).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID)
    await waitFor(() => expect(screen.getByTestId('current-ticket')).toBeInTheDocument())
  })

  it('disables served/no-show actions when nothing is currently serving', async () => {
    renderPage()

    const servedBtn = await screen.findByTestId('mark-served-btn')
    const noShowBtn = await screen.findByTestId('mark-noshow-btn')

    expect(servedBtn).toBeDisabled()
    expect(noShowBtn).toBeDisabled()
  })

  it('marks current ticket as served when serving entry exists', async () => {
    mockGetQueueDashboard
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 0,
        currentlyServing: { entryId: 'entry-1', ticketNumber: 1, joinedAt: '2026-03-01T08:00:00Z' },
        waitingEntries: [],
      })
      .mockResolvedValueOnce({
        queueId: QUEUE_ID,
        queueName: 'Main Counter',
        queueStatus: 'Active',
        waitingCount: 0,
        currentlyServing: null,
        waitingEntries: [],
      })

    const user = userEvent.setup()
    renderPage()

    const servedBtn = await screen.findByTestId('mark-served-btn')
    await user.click(servedBtn)

    expect(mockMarkServed).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID)
    await waitFor(() => expect(screen.queryByTestId('current-ticket')).not.toBeInTheDocument())
  })
})
