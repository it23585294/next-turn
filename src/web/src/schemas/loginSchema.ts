/**
 * Zod validation schema for user login.
 *
 * Intentionally minimal on the client — the server enforces all business
 * rules (credential correctness, lockout). The schema's job is fast UX
 * feedback for structurally invalid input before the network round-trip.
 *
 * Rules:
 *  - Email: must be non-empty and a valid email format
 *  - Password: must be non-empty (no complexity rules here — those are on
 *               the RegisterPage; during login the user just types what they
 *               registered with)
 */
import { z } from 'zod'

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required')
    .email('Enter a valid email address'),

  password: z
    .string()
    .min(1, 'Password is required'),
})

export type LoginFormValues = z.infer<typeof loginSchema>

/** Shape sent to POST /api/auth/login */
export interface LoginRequest {
  email: string
  password: string
}
