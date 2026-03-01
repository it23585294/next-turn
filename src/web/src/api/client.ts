/**
 * Axios instance — base URL from environment variable.
 * The Vite dev-server proxy rewrites /api → http://localhost:5000/api
 * so this works identically in dev and production.
 */
import axios from 'axios'
import type { AxiosError } from 'axios'
import type { ApiError, ProblemDetails } from '../types/api'

export const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15_000,
})

/**
 * Extracts a friendly ApiError from any Axios error.
 * Handles both problem+json (our API) and generic network/timeout errors.
 */
export function parseApiError(err: unknown): ApiError {
  const axiosErr = err as AxiosError<ProblemDetails>
  if (axiosErr.response) {
    const data = axiosErr.response.data ?? {}
    return {
      status: axiosErr.response.status,
      detail: data.detail ?? data.title,
      errors: data.errors,
      raw: data,
    }
  }
  // Network error / timeout
  return {
    status: 0,
    detail: 'Could not reach the server. Please check your connection.',
    raw: {},
  }
}
