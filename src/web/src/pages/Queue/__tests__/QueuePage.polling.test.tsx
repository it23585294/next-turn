/**
 * Polling-specific tests for QueuePage (/queues/:tenantId/:queueId).
 *
 * Kept separate from QueuePage.test.tsx because these tests run under
 * vi.useFakeTimers(), which requires a different interaction strategy.
 *
 * Strategy:
 *  - vi.useFakeTimers() per test so setInterval is fully controlled
 *  - fireEvent.click (sync) wrapped in await act(async()=>{}) to flush
 *    the resolved mock Promise and the resulting React state update —
 *    this avoids userEvent's async delay machinery hanging under fake timers
 *  - await act(async () => { vi.advanceTimersByTime(N) }) to fire each
 *    poll tick deterministically, then flush any resulting state updates
 *
 * Coverage:
 *  1.  30-second poll fires after join — getQueueStatus called once
 *  2.  60-second mark — getQueueStatus called twice (two poll ticks)
 *  3.  Does NOT poll before 30 seconds have elapsed
 *  4.  Poll updates the displayed position from 3 → 2 on next tick
 *  5.  positionInQueue === 1 + queueStatus === Active → banner immediate
 *  6.  positionInQueue === 1 reached via a poll update (not just initial join)
 *  7.  queueStatus 'Paused' after a poll tick → paused-banner visible
 *  8.  queueStatus 'Closed' after a poll tick → closed-banner visible
 *  9.  Polling stops after unmount — no calls after clearInterval fires
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act, fireEvent } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'

import { QueuePage } from '../QueuePage'
import * as queuesApi from '../../../api/queues'
import type { JoinQueueResult, QueueStatusResult } from '../../../api/queues'

// ---------------------------------------------------------------------------
// Module mocks — both joinQueue and getQueueStatus are needed here
// ---------------------------------------------------------------------------
vi.mock('../../../api/queues', () => ({
  joinQueue:        vi.fn(),
  getQueueStatus:   vi.fn(),
}))

const mockJoinQueue      = vi.mocked(queuesApi.joinQueue)
const mockGetQueueStatus = vi.mocked(queuesApi.getQueueStatus)

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------
const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const QUEUE_ID  = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

/** Join result where the user is at position 3. */
const JOIN_RESULT_POSITION3: JoinQueueResult = {
  ticketNumber:         3,
  positionInQueue:      3,
  estimatedWaitSeconds: 900,
}

/** Join result where the user is first in line. */
const JOIN_RESULT_POSITION1: JoinQueueResult = {
  ticketNumber:         1,
  positionInQueue:      1,
  estimatedWaitSeconds: 300,
}

/** Status poll response — position has moved to 2. */
const STATUS_POSITION2: QueueStatusResult = {
  ticketNumber:         3,
  positionInQueue:      2,
  estimatedWaitSeconds: 600,
  queueStatus:          'Active',
}

/** Status poll response — queue is now paused. */
const STATUS_PAUSED: QueueStatusResult = {
  ticketNumber:         3,
  positionInQueue:      3,
  estimatedWaitSeconds: 900,
  queueStatus:          'Paused',
}

/** Status poll response — queue is now closed. */
const STATUS_CLOSED: QueueStatusResult = {
  ticketNumber:         3,
  positionInQueue:      3,
  estimatedWaitSeconds: 900,
  queueStatus:          'Closed',
}

/** Status poll response — user has been promoted to position 1. */
const STATUS_POSITION1: QueueStatusResult = {
  ticketNumber:         3,
  positionInQueue:      1,
  estimatedWaitSeconds: 300,
  queueStatus:          'Active',
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
function renderPage() {
  return render(
    <MemoryRouter initialEntries={[`/queues/${TENANT_ID}/${QUEUE_ID}`]}>
      <Routes>
        <Route path="/queues/:tenantId/:queueId" element={<QueuePage />} />
      </Routes>
    </MemoryRouter>
  )
}

/**
 * Clicks the Join Queue button and waits for React to flush all state updates
 * and the resolved mock Promise from joinQueue.
 *
 * Uses fireEvent (synchronous) instead of userEvent.click to avoid userEvent's
 * internal delay machinery hanging under vi.useFakeTimers().
 * Wraps in act() to flush microtasks + pending React state updates.
 */
async function clickJoin() {
  await act(async () => {
    fireEvent.click(screen.getByRole('button', { name: /join queue/i }))
  })
}

// ---------------------------------------------------------------------------
// Setup — fake timers per test
// ---------------------------------------------------------------------------
beforeEach(() => {
  vi.useFakeTimers()
  mockJoinQueue.mockReset()
  mockGetQueueStatus.mockReset()
})

afterEach(() => {
  vi.useRealTimers()
})

// ---------------------------------------------------------------------------
// 1–3. Poll fires at 30s and 60s, not before
// ---------------------------------------------------------------------------
describe('QueuePage — polling cadence', () => {
  it('calls getQueueStatus once after 30 seconds', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION2)

    renderPage()
    await clickJoin()
    expect(screen.getByTestId('success-block')).toBeInTheDocument()

    // Advance exactly one poll interval.
    await act(async () => { vi.advanceTimersByTime(30_000) })

    expect(mockGetQueueStatus).toHaveBeenCalledTimes(1)
    expect(mockGetQueueStatus).toHaveBeenCalledWith(QUEUE_ID, TENANT_ID)
  })

  it('calls getQueueStatus twice after 60 seconds', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION2)

    renderPage()
    await clickJoin()

    await act(async () => { vi.advanceTimersByTime(60_000) })

    expect(mockGetQueueStatus).toHaveBeenCalledTimes(2)
  })

  it('does NOT poll before 30 seconds have elapsed', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION2)

    renderPage()
    await clickJoin()

    // Advance to just under the threshold.
    await act(async () => { vi.advanceTimersByTime(29_999) })

    expect(mockGetQueueStatus).not.toHaveBeenCalled()
  })
})

// ---------------------------------------------------------------------------
// 4. Position updates on poll tick
// ---------------------------------------------------------------------------
describe('QueuePage — position update after poll', () => {
  it('updates displayed position from 3 to 2 after one poll tick', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION2)

    renderPage()
    await clickJoin()

    // Verify initial position is 3.
    expect(screen.getByText('3')).toBeInTheDocument()

    // Trigger poll tick.
    await act(async () => { vi.advanceTimersByTime(30_000) })

    // Position should now be 2.
    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('does not crash when poll resolves after join succeeds', async () => {
    // Smoke test: verify the component remains stable with a poll update.
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION2)

    renderPage()
    await clickJoin()

    await act(async () => { vi.advanceTimersByTime(30_000) })

    // Component is still stable — no throw, still shows success block.
    expect(screen.getByTestId('success-block')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 5–6. "You're next!" banner
// ---------------------------------------------------------------------------
describe("QueuePage — \"You're next!\" banner", () => {
  it('shows the banner immediately when joining at position 1', async () => {
    // After join, state is seeded with { ...joinResult, queueStatus: 'Active' }.
    // If positionInQueue is 1 and queueStatus is Active, the banner renders at once.
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION1)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION1)

    renderPage()
    await clickJoin()

    expect(screen.getByTestId('youre-next-banner')).toBeInTheDocument()
  })

  it('shows the banner after a poll promotes the user to position 1', async () => {
    // User joins at position 3; a poll tick returns position 1.
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION1)

    renderPage()
    await clickJoin()

    // Banner not visible yet.
    expect(screen.queryByTestId('youre-next-banner')).not.toBeInTheDocument()

    // Poll tick fires.
    await act(async () => { vi.advanceTimersByTime(30_000) })

    expect(screen.getByTestId('youre-next-banner')).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 7–8. Queue status banners (Paused / Closed)
// ---------------------------------------------------------------------------
describe('QueuePage — queue status banners', () => {
  it('shows the paused-banner after a poll returns queueStatus Paused', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_PAUSED)

    renderPage()
    await clickJoin()

    // Banner not visible on initial join (state has queueStatus: 'Active').
    expect(screen.queryByTestId('paused-banner')).not.toBeInTheDocument()

    await act(async () => { vi.advanceTimersByTime(30_000) })

    expect(screen.getByTestId('paused-banner')).toBeInTheDocument()
    expect(screen.getByText(/queue is currently paused/i)).toBeInTheDocument()
  })

  it('shows the closed-banner after a poll returns queueStatus Closed', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_CLOSED)

    renderPage()
    await clickJoin()

    await act(async () => { vi.advanceTimersByTime(30_000) })

    expect(screen.getByTestId('closed-banner')).toBeInTheDocument()
    expect(screen.getByText(/queue has closed/i)).toBeInTheDocument()
  })
})

// ---------------------------------------------------------------------------
// 9. Polling clears on unmount (no memory leak)
// ---------------------------------------------------------------------------
describe('QueuePage — polling cleanup on unmount', () => {
  it('stops calling getQueueStatus after the component is unmounted', async () => {
    mockJoinQueue.mockResolvedValue(JOIN_RESULT_POSITION3)
    mockGetQueueStatus.mockResolvedValue(STATUS_POSITION2)

    const { unmount } = renderPage()
    await clickJoin()

    // Fire the first tick to confirm polling is active.
    await act(async () => { vi.advanceTimersByTime(30_000) })
    expect(mockGetQueueStatus).toHaveBeenCalledTimes(1)

    // Unmount triggers the useEffect cleanup → clearInterval.
    unmount()

    // Advance another full interval — should produce zero additional calls.
    await act(async () => { vi.advanceTimersByTime(30_000) })
    expect(mockGetQueueStatus).toHaveBeenCalledTimes(1) // still 1, not 2
  })
})
