/**
 * Tests for the Auth API layer — auth.ts / client.ts.
 *
 * Strategy: vi.mock the axios apiClient so we never touch the network.
 * We verify that:
 *  - registerUser() sends the correct HTTP method, path, body, and headers
 *  - It returns { ok: true } on a successful response
 *  - It throws a typed ApiError (not a raw AxiosError) on non-2xx responses
 *  - It wraps network errors (status 0) correctly
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import type { AxiosResponse } from 'axios'

// ------- mock apiClient before importing the modules under test -------
vi.mock('../../api/client', () => ({
  apiClient: {
    post: vi.fn(),
  },
  parseApiError: vi.fn((err: unknown) => {
    // Re-export the real implementation so auth.ts uses it correctly
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

import { registerUser } from '../../api/auth'
import { apiClient } from '../../api/client'

const mockPost = vi.mocked(apiClient.post)

const TENANT_ID = 'aabbccdd-0000-0000-0000-000000000001'
const BODY = {
  name: 'Maria Santos',
  email: 'maria@example.com',
  phone: null,
  password: 'Secure1!',
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
function makeAxiosError(status: number, data: Record<string, unknown>) {
  const err = new Error('Request failed') as Error & {
    response: { status: number; data: Record<string, unknown> }
  }
  err.response = { status, data }
  return err
}

// ---------------------------------------------------------------------------
// Success path
// ---------------------------------------------------------------------------
describe('registerUser — success', () => {
  beforeEach(() => {
    mockPost.mockResolvedValueOnce({ status: 201, data: {} } as AxiosResponse)
  })

  it('returns { ok: true } on 201 Created', async () => {
    const result = await registerUser(TENANT_ID, BODY)
    expect(result).toEqual({ ok: true })
  })

  it('calls POST /auth/register with the correct body', async () => {
    await registerUser(TENANT_ID, BODY)
    expect(mockPost).toHaveBeenCalledWith('/auth/register', BODY, expect.any(Object))
  })

  it('sends X-Tenant-Id header matching the tenantId parameter', async () => {
    await registerUser(TENANT_ID, BODY)
    expect(mockPost).toHaveBeenCalledWith(
      '/auth/register',
      BODY,
      expect.objectContaining({
        headers: expect.objectContaining({ 'X-Tenant-Id': TENANT_ID }),
      })
    )
  })
})

// ---------------------------------------------------------------------------
// Domain error (400)
// ---------------------------------------------------------------------------
describe('registerUser — 400 domain error', () => {
  it('throws an ApiError with status 400 and the detail message', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(400, { detail: 'Email address is already in use' })
    )

    await expect(registerUser(TENANT_ID, BODY)).rejects.toMatchObject({
      status: 400,
      detail: 'Email address is already in use',
    })
  })
})

// ---------------------------------------------------------------------------
// Validation error (422)
// ---------------------------------------------------------------------------
describe('registerUser — 422 validation error', () => {
  it('throws an ApiError with status 422 and field errors map', async () => {
    mockPost.mockRejectedValueOnce(
      makeAxiosError(422, {
        errors: { Password: ['Must contain at least one uppercase letter'] },
      })
    )

    await expect(registerUser(TENANT_ID, BODY)).rejects.toMatchObject({
      status: 422,
      errors: { Password: expect.arrayContaining(['Must contain at least one uppercase letter']) },
    })
  })
})

// ---------------------------------------------------------------------------
// Network / timeout error (status 0)
// ---------------------------------------------------------------------------
describe('registerUser — network error', () => {
  it('throws an ApiError with status 0 when there is no response', async () => {
    // No .response property — simulates timeout/network failure
    mockPost.mockRejectedValueOnce(new Error('Network Error'))

    await expect(registerUser(TENANT_ID, BODY)).rejects.toMatchObject({
      status: 0,
      detail: 'Could not reach the server. Please check your connection.',
    })
  })
})
