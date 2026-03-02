/**
 * Tests for the Queue API layer — queues.ts.
 *
 * Strategy: vi.mock the axios apiClient so we never touch the network.
 * We verify that:
 *  - joinQueue() calls POST /queues/{queueId}/join
 *  - It sends Authorization and X-Tenant-Id headers
 *  - It returns the JoinQueueResult on success
 *  - It throws a typed ApiError on non-2xx responses
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import type { AxiosResponse } from 'axios'

// Mock apiClient + parseApiError before importing the module under test.
vi.mock('../../api/client', () => ({
  apiClient: {
    post: vi.fn(),
  },
  parseApiError: vi.fn((err: unknown) => {
    const axiosErr = err as { response?: { status: number; data: Record<string, unknown> } }
    if (axiosErr.response) {
      return {
        status: axiosErr.response.status,
        detail: (axiosErr.response.data?.detail ?? axiosErr.response.data?.title) as string | undefined,
        errors: axiosErr.response.data?.errors as Record<string, string[]> | undefined,
        raw: axiosErr.response.data,
      }
    }
    return { status: 0, detail: 'Could not reach the server. Please check your connection.', raw: {} }
  }),
}))

// Mock getToken so tests don't rely on localStorage.
vi.mock('../../utils/authToken', () => ({
  getToken: vi.fn(() => 'test-jwt-token'),
}))

import { joinQueue, type JoinQueueResult } from '../../api/queues'
import { apiClient } from '../../api/client'

const mockPost = vi.mocked(apiClient.post)

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------
const TENANT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
const QUEUE_ID  = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'

const SAMPLE_RESULT: JoinQueueResult = {
  ticketNumber:         1,
  positionInQueue:      1,
  estimatedWaitSeconds: 300,
}

function makeAxiosError(status: number, data: Record<string, unknown>) {
  const err = new Error('Request failed') as Error & {
    response: { status: number; data: Record<string, unknown> }
  }
  err.response = { status, data }
  return err
}

// ---------------------------------------------------------------------------
// Setup
// ---------------------------------------------------------------------------
beforeEach(() => {
  mockPost.mockReset()
})

// ---------------------------------------------------------------------------
// Success path
// ---------------------------------------------------------------------------
describe('joinQueue — success', () => {
  beforeEach(() => {
    mockPost.mockResolvedValueOnce({ status: 200, data: SAMPLE_RESULT } as AxiosResponse)
  })

  it('returns the JoinQueueResult on 200', async () => {
    const result = await joinQueue(QUEUE_ID, TENANT_ID)
    expect(result).toEqual(SAMPLE_RESULT)
  })

  it('calls POST /queues/{queueId}/join', async () => {
    await joinQueue(QUEUE_ID, TENANT_ID)
    expect(mockPost).toHaveBeenCalledWith(
      `/queues/${QUEUE_ID}/join`,
      null,
      expect.any(Object)
    )
  })

  it('sends Authorization: Bearer header with the stored token', async () => {
    await joinQueue(QUEUE_ID, TENANT_ID)
    expect(mockPost).toHaveBeenCalledWith(
      expect.any(String),
      null,
      expect.objectContaining({
        headers: expect.objectContaining({
          Authorization: 'Bearer test-jwt-token',
        }),
      })
    )
  })

  it('sends X-Tenant-Id header', async () => {
    await joinQueue(QUEUE_ID, TENANT_ID)
    expect(mockPost).toHaveBeenCalledWith(
      expect.any(String),
      null,
      expect.objectContaining({
        headers: expect.objectContaining({
          'X-Tenant-Id': TENANT_ID,
        }),
      })
    )
  })
})

// ---------------------------------------------------------------------------
// Error paths
// ---------------------------------------------------------------------------
describe('joinQueue — errors', () => {
  it('throws a typed ApiError on 400', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(400, { detail: 'Queue not found.' })
    )

    await expect(joinQueue(QUEUE_ID, TENANT_ID)).rejects.toMatchObject({
      status: 400,
      detail: 'Queue not found.',
    })
  })

  it('throws a typed ApiError on 409 (already in queue)', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(409, { detail: 'Already in this queue.' })
    )

    await expect(joinQueue(QUEUE_ID, TENANT_ID)).rejects.toMatchObject({
      status: 409,
      detail: 'Already in this queue.',
    })
  })

  it('throws a typed ApiError with canBookAppointment on 409 (queue full)', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(409, { detail: 'The queue is currently full.', canBookAppointment: true })
    )

    await expect(joinQueue(QUEUE_ID, TENANT_ID)).rejects.toMatchObject({
      status: 409,
      raw: expect.objectContaining({ canBookAppointment: true }),
    })
  })

  it('throws a network ApiError (status 0) on timeout', async () => {
    mockPost.mockRejectedValueOnce(new Error('Network Error'))

    await expect(joinQueue(QUEUE_ID, TENANT_ID)).rejects.toMatchObject({
      status: 0,
      detail: 'Could not reach the server. Please check your connection.',
    })
  })
})
