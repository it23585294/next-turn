/**
 * Auth API calls.
 * Tenant ID is passed as the X-Tenant-Id header (picked up by TenantMiddleware).
 */
import { apiClient, parseApiError } from './client'
import type { RegisterRequest } from '../schemas/registerSchema'
import type { LoginRequest } from '../schemas/loginSchema'
import type { ApiError } from '../types/api'
import { getToken } from '../utils/authToken'

export interface RegisterResult {
  ok: true
}

export interface CreateStaffUserRequest {
  name: string
  email: string
  phone?: string
  password: string
}

export interface InviteStaffUserRequest {
  name: string
  email: string
  phone?: string
}

export interface InviteStaffUserResult {
  userId: string
  invitePath: string
  expiresAt: string
}

export interface StaffUserSummary {
  userId: string
  name: string
  email: string
  phone?: string
  isActive: boolean
  createdAt: string
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

/**
 * POST /api/auth/register-global
 * Registers a consumer (end-user) account — no X-Tenant-Id required.
 * The created user can join queues from any organisation.
 */
export async function registerGlobalUser(
  body: RegisterRequest
): Promise<RegisterResult> {
  try {
    await apiClient.post('/auth/register-global', body)
    return { ok: true }
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/auth/login-global
 * Authenticates a consumer account — no X-Tenant-Id required.
 * Returns a JWT with tid = 00000000-0000-0000-0000-000000000000.
 * The frontend then supplies X-Tenant-Id per-request for org-specific APIs.
 */
export async function loginGlobalUser(
  body: LoginRequest
): Promise<LoginResult> {
  try {
    const { data } = await apiClient.post<LoginResult>('/auth/login-global', body)
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/auth/staff
 * Creates a staff account for the authenticated org admin's tenant.
 */
export async function createStaffUser(
  tenantId: string,
  body: CreateStaffUserRequest,
): Promise<RegisterResult> {
  try {
    const token = getToken()
    await apiClient.post('/auth/staff', body, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })

    return { ok: true }
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/auth/staff/invite
 * Invites a staff user to create their own password via secure invite link.
 */
export async function inviteStaffUser(
  tenantId: string,
  body: InviteStaffUserRequest,
): Promise<InviteStaffUserResult> {
  try {
    const token = getToken()
    const { data } = await apiClient.post<InviteStaffUserResult>('/auth/staff/invite', body, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })

    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/auth/staff/invite/accept
 * Public endpoint for staff to accept invite and set password.
 */
export async function acceptStaffInvite(
  token: string,
  password: string,
): Promise<void> {
  try {
    await apiClient.post('/auth/staff/invite/accept', {
      token,
      password,
    })
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * GET /api/auth/staff
 * Lists staff users in the current organisation.
 */
export async function listStaffUsers(tenantId: string): Promise<StaffUserSummary[]> {
  try {
    const token = getToken()
    const { data } = await apiClient.get<StaffUserSummary[]>('/auth/staff', {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
    return data
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/auth/staff/{userId}/deactivate
 */
export async function deactivateStaffUser(tenantId: string, userId: string): Promise<void> {
  try {
    const token = getToken()
    await apiClient.post(`/auth/staff/${userId}/deactivate`, null, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}

/**
 * POST /api/auth/staff/{userId}/reactivate
 */
export async function reactivateStaffUser(tenantId: string, userId: string): Promise<void> {
  try {
    const token = getToken()
    await apiClient.post(`/auth/staff/${userId}/reactivate`, null, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    })
  } catch (err) {
    const parsed: ApiError = parseApiError(err)
    throw parsed
  }
}
