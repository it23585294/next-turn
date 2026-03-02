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
    get:  vi.fn(),
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

import {
  joinQueue,
  createQueue,
  getQueueStatus,
  type JoinQueueResult,
  type CreateQueueResult,
  type QueueStatusResult,
} from '../../api/queues'
import { apiClient } from '../../api/client'

const mockPost = vi.mocked(apiClient.post)
const mockGet  = vi.mocked(apiClient.get)

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
  mockGet.mockReset()
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

// ---------------------------------------------------------------------------
// createQueue
// ---------------------------------------------------------------------------
const SAMPLE_CREATE_RESULT: CreateQueueResult = {
  queueId:       'cccccccc-cccc-cccc-cccc-cccccccccccc',
  shareableLink: `/queues/${TENANT_ID}/cccccccc-cccc-cccc-cccc-cccccccccccc`,
}

const CREATE_BODY = {
  name:                      'Main Counter',
  maxCapacity:               50,
  averageServiceTimeSeconds: 300,
}

describe('createQueue — success', () => {
  beforeEach(() => {
    mockPost.mockResolvedValueOnce({ status: 201, data: SAMPLE_CREATE_RESULT } as AxiosResponse)
  })

  it('returns the CreateQueueResult on 201', async () => {
    const result = await createQueue(TENANT_ID, CREATE_BODY)
    expect(result).toEqual(SAMPLE_CREATE_RESULT)
  })

  it('calls POST /queues with the request body', async () => {
    await createQueue(TENANT_ID, CREATE_BODY)
    expect(mockPost).toHaveBeenCalledWith('/queues', CREATE_BODY, expect.any(Object))
  })

  it('sends Authorization: Bearer header', async () => {
    await createQueue(TENANT_ID, CREATE_BODY)
    expect(mockPost).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(Object),
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer test-jwt-token' }),
      })
    )
  })

  it('sends X-Tenant-Id header', async () => {
    await createQueue(TENANT_ID, CREATE_BODY)
    expect(mockPost).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(Object),
      expect.objectContaining({
        headers: expect.objectContaining({ 'X-Tenant-Id': TENANT_ID }),
      })
    )
  })
})

describe('createQueue — errors', () => {
  it('throws ApiError on 403 Forbidden', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(403, { detail: 'Forbidden.' })
    )
    await expect(createQueue(TENANT_ID, CREATE_BODY)).rejects.toMatchObject({
      status: 403,
    })
  })

  it('throws ApiError with validation errors on 422', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(422, {
        errors: { Name: ['Queue name is required.'] },
      })
    )
    await expect(createQueue(TENANT_ID, CREATE_BODY)).rejects.toMatchObject({
      status: 422,
      errors: { Name: ['Queue name is required.'] },
    })
  })
})

// ---------------------------------------------------------------------------
// getQueueStatus
// ---------------------------------------------------------------------------
const SAMPLE_STATUS_RESULT: QueueStatusResult = {
  ticketNumber:         7,
  positionInQueue:      3,
  estimatedWaitSeconds: 900,
  queueStatus:          'Active',
}

describe('getQueueStatus — success', () => {
  beforeEach(() => {
    mockGet.mockResolvedValueOnce({ status: 200, data: SAMPLE_STATUS_RESULT } as AxiosResponse)
  })

  it('returns the QueueStatusResult on 200', async () => {
    const result = await getQueueStatus(QUEUE_ID, TENANT_ID)
    expect(result).toEqual(SAMPLE_STATUS_RESULT)
  })

  it('calls GET /queues/{queueId}/status', async () => {
    await getQueueStatus(QUEUE_ID, TENANT_ID)
    expect(mockGet).toHaveBeenCalledWith(
      `/queues/${QUEUE_ID}/status`,
      expect.any(Object)
    )
  })

  it('sends Authorization: Bearer header', async () => {
    await getQueueStatus(QUEUE_ID, TENANT_ID)
    expect(mockGet).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer test-jwt-token' }),
      })
    )
  })

  it('sends X-Tenant-Id header', async () => {
    await getQueueStatus(QUEUE_ID, TENANT_ID)
    expect(mockGet).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        headers: expect.objectContaining({ 'X-Tenant-Id': TENANT_ID }),
      })
    )
  })
})

describe('getQueueStatus — errors', () => {
  it('throws ApiError on 400 (no active entry)', async () => {
    mockGet.mockRejectedValueOnce(
      makeAxiosError(400, { detail: 'No active queue entry found.' })
    )
    await expect(getQueueStatus(QUEUE_ID, TENANT_ID)).rejects.toMatchObject({
      status: 400,
      detail: 'No active queue entry found.',
    })
  })

  it('throws ApiError on 401 (missing JWT)', async () => {
    mockGet.mockRejectedValueOnce(
      makeAxiosError(401, { title: 'Unauthorized' })
    )
    await expect(getQueueStatus(QUEUE_ID, TENANT_ID)).rejects.toMatchObject({
      status: 401,
    })
  })
})
