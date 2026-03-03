/**
 * Zod validation schema for user registration.
 * Rules mirror RegisterUserValidator on the backend:
 *  - Name:     required, non-empty
 *  - Email:    valid email
 *  - Phone:    optional, digits/spaces/dashes/plus, 7-15 chars when supplied
 *  - Password: min 8 chars, ≥1 uppercase, ≥1 digit, ≥1 special character
 *  - Confirm:  must match password (frontend-only UX guard)
 */
import { z } from 'zod'

const SPECIAL_CHAR_REGEX = /[^A-Za-z0-9]/
const UPPERCASE_REGEX = /[A-Z]/
const DIGIT_REGEX = /[0-9]/
const PHONE_REGEX = /^\+?[\d\s\-().]{7,20}$/

export const registerSchema = z
  .object({
    name: z
      .string()
      .min(1, 'Name is required')
      .min(2, 'Name must be at least 2 characters')
      .max(100, 'Name is too long'),

    email: z
      .string()
      .min(1, 'Email is required')
      .email('Enter a valid email address'),

    phone: z
      .string()
      .refine(
        (val) => val === '' || PHONE_REGEX.test(val),
        'Enter a valid phone number'
      )
      .optional()
      .or(z.literal('')),

    password: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .refine((v) => UPPERCASE_REGEX.test(v), 'Must contain at least one uppercase letter')
      .refine((v) => DIGIT_REGEX.test(v), 'Must contain at least one number')
      .refine((v) => SPECIAL_CHAR_REGEX.test(v), 'Must contain at least one special character'),

    confirmPassword: z.string().min(1, 'Please confirm your password'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })

export type RegisterFormValues = z.infer<typeof registerSchema>

/** Shape sent to POST /api/auth/register */
export interface RegisterRequest {
  name: string
  email: string
  phone?: string | null
  password: string
}

/** Compute password strength (0–4) for visual feedback */
export function getPasswordStrength(password: string): number {
  if (!password) return 0
  let score = 0
  if (password.length >= 8)  score++
  if (UPPERCASE_REGEX.test(password)) score++
  if (DIGIT_REGEX.test(password))     score++
  if (SPECIAL_CHAR_REGEX.test(password)) score++
  return score
}
