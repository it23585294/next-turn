/**
 * Auth API calls.
 * Tenant ID is passed as the X-Tenant-Id header (picked up by TenantMiddleware).
 */
import { apiClient, parseApiError } from './client'
import type { RegisterRequest } from '../schemas/registerSchema'
import type { ApiError } from '../types/api'

export interface RegisterResult {
  ok: true
}

/**
 * POST /api/auth/register
 * @param tenantId  Organization GUID — passed as X-Tenant-Id header
 * @param body      Registration payload
 */
export async function registerUser(
  tenantId: string,
  body: RegisterRequest
): Promise<RegisterResult> {
  try {
    await apiClient.post('/auth/register', body, {
      headers: { 'X-Tenant-Id': tenantId },
    })
    return { ok: true }
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}
