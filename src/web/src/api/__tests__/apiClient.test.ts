/**
 * Tests for the 401 interceptor in client.ts (NT-12-68).
 *
 * Strategy:
 *  - Import the real apiClient so the interceptor is live
 *  - Replace the axios adapter with a per-test fake that returns controlled responses
 *  - Spy on window.location.replace to assert redirects
 *  - Mock clearToken and getTokenPayload from authToken
 *
 * Coverage:
 *  1. 401 response → clearToken() is called
 *  2. 401 with tid in token → redirects to /login/:tid?reason=session_expired
 *  3. 401 with no token payload → redirects to /?reason=session_expired
 *  4. 403 response → NOT redirected, error propagates
 *  5. 200 response → passes through, no redirect
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import type { InternalAxiosRequestConfig, AxiosResponse } from 'axios'

// ---------------------------------------------------------------------------
// Mock authToken BEFORE importing client so the interceptor captures the mocks
// ---------------------------------------------------------------------------
vi.mock('../../utils/authToken', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../../utils/authToken')>()
  return { ...actual, clearToken: vi.fn(), getTokenPayload: vi.fn() }
})

import * as authToken from '../../utils/authToken'
import { apiClient } from '../client'
import type { TokenPayload } from '../../utils/authToken'

const mockClearToken      = vi.mocked(authToken.clearToken)
const mockGetTokenPayload = vi.mocked(authToken.getTokenPayload)

const TENANT = 'aabbccdd-0000-0000-0000-000000000001'

const BASE_PAYLOAD: TokenPayload = {
  sub: 'user-1', email: 'alice@example.com', name: 'Alice Smith',
  role: 'User', tid: TENANT, exp: 9999999999, iat: 0,
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Replace the apiClient adapter with one that resolves or rejects synchronously. */
function fakeAdapter(status: number) {
  apiClient.defaults.adapter = async (
    config: InternalAxiosRequestConfig
  ): Promise<AxiosResponse> => {
    if (status >= 200 && status < 300) {
      return {
        data: { ok: true },
        status,
        statusText: 'OK',
        headers: {},
        config,
      }
    }
    // Simulate an AxiosError for non-2xx
    const err = Object.assign(new Error(`Request failed with status ${status}`), {
      isAxiosError: true,
      response: {
        data: {},
        status,
        statusText: status === 401 ? 'Unauthorized' : status === 403 ? 'Forbidden' : 'Error',
        headers: {},
        config,
      },
      config,
    })
    return Promise.reject(err)
  }
}

// ---------------------------------------------------------------------------
// window.location.replace spy
// ---------------------------------------------------------------------------
// jsdom does not allow direct assignment of window.location, so we use
// Object.defineProperty to replace `replace` with a spy.
let replaceSpy: ReturnType<typeof vi.fn>

beforeEach(() => {
  replaceSpy = vi.fn()
  Object.defineProperty(window, 'location', {
    writable: true,
    value: { ...window.location, replace: replaceSpy },
  })
  mockGetTokenPayload.mockReturnValue(BASE_PAYLOAD)
})

afterEach(() => {
  // Restore the real adapter
  apiClient.defaults.adapter = undefined
})

// ---------------------------------------------------------------------------
// 401 — intercept and redirect
// ---------------------------------------------------------------------------
describe('apiClient — 401 interceptor', () => {
  it('calls clearToken when a 401 response is received', async () => {
    fakeAdapter(401)
    await apiClient.get('/test').catch(() => {})
    expect(mockClearToken).toHaveBeenCalledOnce()
  })

  it('redirects to /login/:tid?reason=session_expired when tid is in the token', async () => {
    fakeAdapter(401)
    await apiClient.get('/test').catch(() => {})
    expect(replaceSpy).toHaveBeenCalledWith(`/login/${TENANT}?reason=session_expired`)
  })

  it('redirects to /?reason=session_expired when token payload is null', async () => {
    mockGetTokenPayload.mockReturnValue(null)
    fakeAdapter(401)
    await apiClient.get('/test').catch(() => {})
    expect(replaceSpy).toHaveBeenCalledWith('/?reason=session_expired')
  })

  it('still rejects the promise after redirecting', async () => {
    fakeAdapter(401)
    await expect(apiClient.get('/test')).rejects.toThrow()
  })
})

// ---------------------------------------------------------------------------
// Non-401 — pass through
// ---------------------------------------------------------------------------
describe('apiClient — non-401 errors', () => {
  it('does NOT call clearToken on a 403 response', async () => {
    fakeAdapter(403)
    await apiClient.get('/test').catch(() => {})
    expect(mockClearToken).not.toHaveBeenCalled()
  })

  it('does NOT redirect on a 403 response', async () => {
    fakeAdapter(403)
    await apiClient.get('/test').catch(() => {})
    expect(replaceSpy).not.toHaveBeenCalled()
  })

  it('propagates the 403 error to the caller', async () => {
    fakeAdapter(403)
    await expect(apiClient.get('/test')).rejects.toMatchObject({
      response: { status: 403 },
    })
  })
})

// ---------------------------------------------------------------------------
// 200 — happy path
// ---------------------------------------------------------------------------
describe('apiClient — 2xx responses', () => {
  it('returns the response on a 200 response without redirecting', async () => {
    fakeAdapter(200)
    const result = await apiClient.get('/test')
    expect(result.status).toBe(200)
    expect(replaceSpy).not.toHaveBeenCalled()
    expect(mockClearToken).not.toHaveBeenCalled()
  })
})
