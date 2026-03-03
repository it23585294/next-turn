/**
 * Auth API calls.
 * Tenant ID is passed as the X-Tenant-Id header (picked up by TenantMiddleware).
 */
import { apiClient, parseApiError } from './client'
import type { RegisterRequest } from '../schemas/registerSchema'
import type { LoginRequest } from '../schemas/loginSchema'
import type { ApiError } from '../types/api'

export interface RegisterResult {
  ok: true
}

/**
 * Shape returned by POST /api/auth/login on HTTP 200.
 * Mirrors NextTurn.API.Models.Auth.LoginResponse on the backend.
 */
export interface LoginResult {
  accessToken: string
  userId: string
  name: string
  email: string
  /** UserRole enum string: "User" | "Staff" | "OrgAdmin" | "SystemAdmin" */
  role: string
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

/**
 * POST /api/auth/login
 * @param tenantId  Organization GUID — passed as X-Tenant-Id header
 * @param body      Login payload (email + password)
 * @returns         JWT access token + user metadata on success
 * @throws          ApiError on 400 (invalid credentials / locked) or 429 (rate limit)
 */
export async function loginUser(
  tenantId: string,
  body: LoginRequest
): Promise<LoginResult> {
  try {
    const { data } = await apiClient.post<LoginResult>('/auth/login', body, {
      headers: { 'X-Tenant-Id': tenantId },
    })
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}
