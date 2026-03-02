/**
 * Tests for QueuePage (/queues/:tenantId/:queueId).
 *
 * Strategy:
 *  - Mock joinQueue so we control success / error outcomes without hitting the network
 *  - Render QueuePage inside MemoryRouter with the required route params
 *  - Use userEvent to click the join button and assert the resulting UI state
 *
 * Coverage:
 *  1. Idle — renders "Join Queue" heading and button
 *  2. Joining — shows loading indicator while the call is in flight
 *  3. Joined — success block: ticket number, position, estimated wait
 *  4. Joined — formatEta: seconds < 60 shown as "Xs", minutes shown as "X mins"
 *  5. Already in queue (409 without canBookAppointment) — info block shown
 *  6. Queue full (409 with canBookAppointment: true) — full block + appointment CTA shown
 *  7. Generic error — error block with the API detail message
 *  8. "Try Again" / "Back" buttons reset to idle state
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { QueuePage } from '../QueuePage'
import * as queuesApi from '../../../api/queues'
import type { JoinQueueResult } from '../../../api/queues'

// ---------------------------------------------------------------------------
// Module mocks
// ---------------------------------------------------------------------------
vi.mock('../../../api/queues', () => ({
  joinQueue: vi.fn(),
}))

const mockJoinQueue = vi.mocked(queuesApi.joinQueue)

// ---------------------------------------------------------------------------
// Constants / helpers
// ---------------------------------------------------------------------------
const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const QUEUE_ID  = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

const SAMPLE_RESULT: JoinQueueResult = {
  ticketNumber:         7,
  positionInQueue:      7,
  estimatedWaitSeconds: 2100, // 7 × 300s = 35 mins
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={[`/queues/${TENANT_ID}/${QUEUE_ID}`]}>
      <Routes>
        <Route path="/queues/:tenantId/:queueId" element={<QueuePage />} />
      </Routes>
    </MemoryRouter>
  )
}

function makeApiError(
  status: number,
  detail: string,
  extra?: Record<string, unknown>
) {
  return {
    status,
    detail,
    raw: { detail, ...extra },
  }
}

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
beforeEach(() => {
  mockJoinQueue.mockReset()
})

// ---------------------------------------------------------------------------
// 1. Idle state
// ---------------------------------------------------------------------------
describe('QueuePage — idle', () => {
  it('renders the page heading', () => {
    renderPage()
    expect(screen.getByRole('heading', { name: /join queue/i })).toBeInTheDocument()
  })

  it('renders a "Join Queue" button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /join queue/i })).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 2. Joining (loading) state
// ---------------------------------------------------------------------------
describe('QueuePage — loading', () => {
  it('shows a loading message while the API call is pending', async () => {
    // Never resolves during this test so we can inspect the loading state
    mockJoinQueue.mockReturnValue(new Promise(() => {}))

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    expect(await screen.findByText(/joining queue/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /join queue/i })).not.toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 3. Joined (success) state
// ---------------------------------------------------------------------------
describe('QueuePage — success', () => {
  beforeEach(() => {
    mockJoinQueue.mockResolvedValue(SAMPLE_RESULT)
  })

  it('shows the ticket number after a successful join', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await waitFor(() =>
      expect(screen.getByTestId('success-block')).toBeInTheDocument()
    )
    expect(screen.getByText(`#${SAMPLE_RESULT.ticketNumber}`)).toBeInTheDocument()
  })

  it('shows the position in queue', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await screen.findByTestId('success-block')
    expect(screen.getByText(String(SAMPLE_RESULT.positionInQueue))).toBeInTheDocument()
  })

  it('shows estimated wait in minutes (>= 60s)', async () => {
    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await screen.findByTestId('success-block')
    // 2100s = 35 mins
    expect(screen.getByText('35 mins')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 4. formatEta edge cases
// ---------------------------------------------------------------------------
describe('QueuePage — ETA formatting', () => {
  it('shows seconds for waits shorter than 60s', async () => {
    mockJoinQueue.mockResolvedValue({ ...SAMPLE_RESULT, estimatedWaitSeconds: 45 })

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await screen.findByTestId('success-block')
    expect(screen.getByText('45s')).toBeInTheDocument()
  })

  it('shows "1 min" for exactly 60 seconds', async () => {
    mockJoinQueue.mockResolvedValue({ ...SAMPLE_RESULT, estimatedWaitSeconds: 60 })

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await screen.findByTestId('success-block')
    expect(screen.getByText('1 min')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 5. Already in queue (409 without canBookAppointment)
// ---------------------------------------------------------------------------
describe('QueuePage — already in queue', () => {
  it('shows the already-in block on a 409 without canBookAppointment', async () => {
    mockJoinQueue.mockRejectedValue(
      makeApiError(409, 'Already in this queue.')
    )

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    expect(await screen.findByTestId('already-in-block')).toBeInTheDocument()
    expect(screen.getByText(/already have an active ticket/i)).toBeInTheDocument()
  })

  it('resets to idle when the Back button is clicked', async () => {
    mockJoinQueue.mockRejectedValue(
      makeApiError(409, 'Already in this queue.')
    )

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))
    await screen.findByTestId('already-in-block')

    await userEvent.click(screen.getByRole('button', { name: /back/i }))

    expect(screen.getByRole('button', { name: /join queue/i })).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 6. Queue full (409 with canBookAppointment: true)
// ---------------------------------------------------------------------------
describe('QueuePage — queue full', () => {
  it('shows the queue-full block on a 409 with canBookAppointment: true', async () => {
    mockJoinQueue.mockRejectedValue(
      makeApiError(409, 'The queue is currently full.', { canBookAppointment: true })
    )

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    expect(await screen.findByTestId('queue-full-block')).toBeInTheDocument()
    expect(screen.getByText(/queue is currently full/i)).toBeInTheDocument()
  })

  it('shows the Book an Appointment CTA', async () => {
    mockJoinQueue.mockRejectedValue(
      makeApiError(409, 'The queue is currently full.', { canBookAppointment: true })
    )

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await screen.findByTestId('queue-full-block')
    expect(screen.getByTestId('book-appointment-btn')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 7. Generic error
// ---------------------------------------------------------------------------
describe('QueuePage — generic error', () => {
  it('shows the error block with the API detail message', async () => {
    mockJoinQueue.mockRejectedValue(
      makeApiError(400, 'Queue not found.')
    )

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    expect(await screen.findByTestId('error-block')).toBeInTheDocument()
    expect(screen.getByText('Queue not found.')).toBeInTheDocument()
  })

  it('falls back to a generic message if no detail is provided', async () => {
    mockJoinQueue.mockRejectedValue({ status: 500, raw: {} })

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))

    await screen.findByTestId('error-block')
    expect(screen.getByText(/something went wrong/i)).toBeInTheDocument()
  })

  it('resets to idle when Try Again is clicked', async () => {
    mockJoinQueue.mockRejectedValue(makeApiError(400, 'Queue not found.'))

    renderPage()
    await userEvent.click(screen.getByRole('button', { name: /join queue/i }))
    await screen.findByTestId('error-block')

    await userEvent.click(screen.getByRole('button', { name: /try again/i }))

    expect(screen.getByRole('button', { name: /join queue/i })).toBeInTheDocument()
  })
})
