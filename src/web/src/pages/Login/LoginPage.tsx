/**
 * LoginPage — Full-page split-screen sign-in UI.
 *
 * Route: /login/:tenantId
 *
 * Flow:
 *  idle → submitting → success (saveToken + navigate /dashboard/:tenantId)
 *                    → error (generic banner)
 *                    → lockout (distinct locked banner)
 *                    → rate limit (429 banner)
 *
 * Server error classification:
 *  400 + "temporarily locked" → lockout banner
 *  400 other                  → generic "Invalid credentials" banner
 *  429                        → rate-limit banner
 *  else                       → generic "Something went wrong" banner
 */
import { useForm, type SubmitHandler } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useParams, useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useState } from 'react'

import { loginSchema } from '../../schemas/loginSchema'
import type { LoginFormValues } from '../../schemas/loginSchema'
import { loginUser } from '../../api/auth'
import type { ApiError } from '../../types/api'
import { saveToken } from '../../utils/authToken'

import { FormField } from '../../components/ui/FormField'
import { TextInput } from '../../components/ui/TextInput'
import { PasswordInput } from '../../components/ui/PasswordInput'
import { Button } from '../../components/ui/Button'

import logoImg from '../../assets/nextTurn-logo.png'
import styles from './LoginPage.module.css'

type BannerKind = 'error' | 'lockout' | 'ratelimit' | 'session'

export function LoginPage() {
  const { tenantId } = useParams<{ tenantId: string }>()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  // Show a soft info banner when the user was redirected here because their
  // session expired (401 interceptor in client.ts sets ?reason=session_expired).
  const sessionExpired = searchParams.get('reason') === 'session_expired'
  const [banner, setBanner] = useState<{ kind: BannerKind; message: string } | null>(
    sessionExpired
      ? { kind: 'session', message: 'Your session has expired. Please sign in again.' }
      : null
  )

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    mode: 'onTouched',
  })

  const onSubmit: SubmitHandler<LoginFormValues> = async (data) => {
    if (!tenantId) {
      setBanner({ kind: 'error', message: 'This link is invalid. Please contact your organization.' })
      return
    }

    setBanner(null)

    try {
      const result = await loginUser(tenantId, {
        email: data.email,
        password: data.password,
      })
      saveToken(result.accessToken)

      // If the user arrived here via a protected route (e.g. a shared queue link),
      // ProtectedRoute appended ?returnTo=<path>.  Send them back there; otherwise
      // fall through to the role-based default destination.
      const returnTo = searchParams.get('returnTo')
      if (returnTo) {
        navigate(decodeURIComponent(returnTo), { replace: true })
        return
      }

      const destination =
        result.role === 'SystemAdmin' || result.role === 'OrgAdmin'
          ? `/admin/${tenantId}`
          : result.role === 'Staff'
            ? `/staff/${tenantId}`
            : `/dashboard/${tenantId}`

      navigate(destination, { replace: true })
    } catch (err) {
      const apiErr = err as ApiError

      if (apiErr.status === 429) {
        setBanner({
          kind: 'ratelimit',
          message: 'Too many sign-in attempts. Please wait a moment before trying again.',
        })
      } else if (apiErr.status === 400) {
        const detail = apiErr.detail ?? ''
        if (detail.toLowerCase().includes('temporarily locked')) {
          setBanner({ kind: 'lockout', message: 'Account temporarily locked. Too many incorrect attempts — please try again later.' })
        } else {
          setBanner({ kind: 'error', message: 'Incorrect email or password. Please try again.' })
        }
      } else {
        setBanner({
          kind: 'error',
          message: apiErr.detail ?? 'Something went wrong. Please try again later.',
        })
      }
    }
  }

  if (!tenantId) {
    return (
      <div className={styles.page}>
        <HeroPanel />
        <div className={styles.formPanel}>
          <div className={styles.errorCard}>
            <span className={styles.errorCardIcon}>🔗</span>
            <h2 className={styles.errorCardTitle}>Invalid Sign-In Link</h2>
            <p className={styles.errorCardBody}>
              This link is missing an organization identifier.
              Please ask your organization for the correct sign-in URL.
            </p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className={styles.page}>
      <HeroPanel />

      <div className={styles.formPanel}>
        <div className={styles.formContainer}>
          {/* Header */}
          <div className={styles.formHeader}>
            <h1 className={styles.formTitle}>Welcome back</h1>
            <p className={styles.formSubtitle}>
              Sign in to your organization's NextTurn account.
            </p>
          </div>

          {/* Banner — error / lockout / rate-limit / session-expired */}
          {banner && (
            <div
              className={
                banner.kind === 'lockout'   ? styles.bannerLockout
                : banner.kind === 'ratelimit' ? styles.bannerRateLimit
                : banner.kind === 'session'   ? styles.bannerSession
                : styles.bannerError
              }
              role="alert"
            >
              {banner.kind === 'lockout'
                ? <LockIcon />
                : banner.kind === 'ratelimit'
                  ? <ClockIcon />
                  : banner.kind === 'session'
                    ? <InfoIcon />
                    : <ErrorIcon />}
              <span>{banner.message}</span>
            </div>
          )}

          <form className={styles.form} onSubmit={handleSubmit(onSubmit)} noValidate>
            {/* Email */}
            <FormField label="Email Address" htmlFor="email" error={errors.email?.message} required>
              <TextInput
                id="email"
                type="email"
                placeholder="you@example.com"
                hasError={!!errors.email}
                autoComplete="email"
                autoFocus
                {...register('email')}
              />
            </FormField>

            {/* Password */}
            <FormField label="Password" htmlFor="password" error={errors.password?.message} required>
              <PasswordInput
                id="password"
                placeholder="Your password"
                hasError={!!errors.password}
                autoComplete="current-password"
                {...register('password')}
              />
            </FormField>

            {/* Submit */}
            <div className={styles.actions}>
              <Button
                type="submit"
                variant="primary"
                size="lg"
                fullWidth
                loading={isSubmitting}
              >
                Sign In
              </Button>
            </div>
          </form>

          {/* Footer */}
          <p className={styles.formFooter}>
            Don't have an account?{' '}
            <Link to={`/register/${tenantId}`} className={styles.link}>
              Create one
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}

/* ------------------------------------------------------------------ */
/* Hero panel (left side)                                               */
/* ------------------------------------------------------------------ */
function HeroPanel() {
  return (
    <div className={styles.hero} aria-hidden="true">
      <div className={styles.heroContent}>
        {/* Logo */}
        <div className={styles.logo}>
          <img src={logoImg} alt="NextTurn" className={styles.logoImg} />
        </div>

        {/* Tagline */}
        <h2 className={styles.heroTagline}>
          Your time.<br />Your turn.
        </h2>
        <p className={styles.heroDesc}>
          Sign in to check your queue status, manage your appointments,
          and get notified the moment it's your turn.
        </p>

        {/* Feature highlights */}
        <ul className={styles.features}>
          <FeatureItem icon={<QueueIcon />}    text="Real-time queue position updates" />
          <FeatureItem icon={<CalendarIcon />} text="View and manage your appointments" />
          <FeatureItem icon={<BellIcon />}     text="Never miss your turn again" />
          <FeatureItem icon={<ShieldIcon />}   text="Secure, tenant-isolated access" />
        </ul>
      </div>

      {/* Decorative blobs */}
      <div className={styles.blobTop}    aria-hidden="true" />
      <div className={styles.blobBottom} aria-hidden="true" />
    </div>
  )
}

function FeatureItem({ icon, text }: { icon: React.ReactNode; text: string }) {
  return (
    <li className={styles.featureItem}>
      <span className={styles.featureIcon}>{icon}</span>
      <span>{text}</span>
    </li>
  )
}

/* ------------------------------------------------------------------ */
/* SVG icons                                                            */
/* ------------------------------------------------------------------ */
function QueueIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <line x1="8" y1="6" x2="21" y2="6"/>
      <line x1="8" y1="12" x2="21" y2="12"/>
      <line x1="8" y1="18" x2="21" y2="18"/>
      <line x1="3" y1="6" x2="3.01" y2="6"/>
      <line x1="3" y1="12" x2="3.01" y2="12"/>
      <line x1="3" y1="18" x2="3.01" y2="18"/>
    </svg>
  )
}

function CalendarIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
      <line x1="16" y1="2" x2="16" y2="6"/>
      <line x1="8" y1="2" x2="8" y2="6"/>
      <line x1="3" y1="10" x2="21" y2="10"/>
    </svg>
  )
}

function BellIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 01-3.46 0"/>
    </svg>
  )
}

function ShieldIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
    </svg>
  )
}

function ErrorIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0 }}>
      <circle cx="12" cy="12" r="10"/>
      <line x1="12" y1="8" x2="12" y2="12"/>
      <line x1="12" y1="16" x2="12.01" y2="16"/>
    </svg>
  )
}

function LockIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0 }}>
      <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
      <path d="M7 11V7a5 5 0 0110 0v4"/>
    </svg>
  )
}

function InfoIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0 }}>
      <circle cx="12" cy="12" r="10"/>
      <line x1="12" y1="16" x2="12" y2="12"/>
      <line x1="12" y1="8" x2="12.01" y2="8"/>
    </svg>
  )
}

function ClockIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      style={{ flexShrink: 0 }}>
      <circle cx="12" cy="12" r="10"/>
      <polyline points="12 6 12 12 16 14"/>
    </svg>
  )
}
