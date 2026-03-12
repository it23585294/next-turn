/**
 * GlobalLoginPage — Consumer account sign-in.
 *
 * Route: /login  (no tenantId — end-users are not bound to any org)
 *
 * Calls POST /api/auth/login-global. Returns a JWT with tid = Guid.Empty.
 * The dashboard then supplies X-Tenant-Id per-request for org-specific APIs.
 *
 * After login, navigates to /dashboard/global  (or ?returnTo path if present).
 */
import { useForm, type SubmitHandler } from 'react-hook-form'
import { zodResolver }       from '@hookform/resolvers/zod'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { useState } from 'react'

import { loginSchema }        from '../../schemas/loginSchema'
import type { LoginFormValues } from '../../schemas/loginSchema'
import { loginGlobalUser }    from '../../api/auth'
import type { ApiError }      from '../../types/api'
import { saveToken }          from '../../utils/authToken'

import { FormField }    from '../../components/ui/FormField'
import { TextInput }    from '../../components/ui/TextInput'
import { PasswordInput } from '../../components/ui/PasswordInput'
import { Button }       from '../../components/ui/Button'

import logoImg from '../../assets/nextTurn-logo.png'
// Reuse existing LoginPage CSS module — same layout.
import styles from '../Login/LoginPage.module.css'

type BannerKind = 'error' | 'lockout' | 'ratelimit' | 'session'

export function GlobalLoginPage() {
  const navigate       = useNavigate()
  const [searchParams] = useSearchParams()

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
    setBanner(null)

    try {
      const result = await loginGlobalUser({
        email:    data.email,
        password: data.password,
      })
      saveToken(result.accessToken)

      const returnTo = searchParams.get('returnTo')
      if (returnTo) {
        navigate(decodeURIComponent(returnTo), { replace: true })
        return
      }

      // Global users land on a tenant-free dashboard.
      navigate('/dashboard', { replace: true })
    } catch (err) {
      const apiErr = err as ApiError

      if (apiErr.status === 429) {
        setBanner({ kind: 'ratelimit', message: 'Too many sign-in attempts. Please wait a moment before trying again.' })
      } else if (apiErr.status === 400) {
        const detail = apiErr.detail ?? ''
        if (detail.toLowerCase().includes('temporarily locked')) {
          setBanner({ kind: 'lockout', message: 'Account temporarily locked. Too many incorrect attempts — please try again later.' })
        } else {
          setBanner({ kind: 'error', message: 'Incorrect email or password. Please try again.' })
        }
      } else {
        setBanner({ kind: 'error', message: apiErr.detail ?? 'Something went wrong. Please try again later.' })
      }
    }
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
              Sign in to your NextTurn account.
            </p>
          </div>

          {/* Banner */}
          {banner && (
            <div
              className={
                banner.kind === 'lockout'    ? styles.bannerLockout
                : banner.kind === 'ratelimit'  ? styles.bannerRateLimit
                : banner.kind === 'session'    ? styles.bannerSession
                : styles.bannerError
              }
              role="alert"
            >
              {banner.kind === 'lockout'    ? <LockIcon />
                : banner.kind === 'ratelimit' ? <ClockIcon />
                : banner.kind === 'session'   ? <InfoIcon />
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
              <Button type="submit" variant="primary" size="lg" fullWidth loading={isSubmitting}>
                Sign In
              </Button>
            </div>
          </form>

          {/* Footer */}
          <p className={styles.formFooter}>
            Don't have an account?{' '}
            <Link to="/register" className={styles.link}>Create one</Link>
          </p>
        </div>
      </div>
    </div>
  )
}

/* ── Hero panel ─────────────────────────────────────────────────────────── */
function HeroPanel() {
  return (
    <div className={styles.hero} aria-hidden="true">
      <div className={styles.heroContent}>
        <div className={styles.logo}>
          <img src={logoImg} alt="NextTurn" className={styles.logoImg} />
        </div>
        <h2 className={styles.heroTagline}>
          Your time.<br />Your turn.
        </h2>
        <p className={styles.heroDesc}>
          Sign in to check your queue status, manage your appointments,
          and get notified the moment it's your turn.
        </p>
        <ul className={styles.features}>
          <FeatureItem icon={<QueueIcon />}    text="Real-time queue position updates" />
          <FeatureItem icon={<BellIcon />}     text="Never miss your turn again" />
          <FeatureItem icon={<ShieldIcon />}   text="Secure, private access" />
        </ul>
      </div>
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

/* ── SVG icons ──────────────────────────────────────────────────────────── */
function QueueIcon() {
  return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <line x1="8" y1="6" x2="21" y2="6"/> <line x1="8" y1="12" x2="21" y2="12"/>
    <line x1="8" y1="18" x2="21" y2="18"/> <line x1="3" y1="6" x2="3.01" y2="6"/>
    <line x1="3" y1="12" x2="3.01" y2="12"/> <line x1="3" y1="18" x2="3.01" y2="18"/>
  </svg>
}
function BellIcon() {
  return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 01-3.46 0"/>
  </svg>
}
function ShieldIcon() {
  return <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
  </svg>
}
function ErrorIcon() {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
    strokeLinecap="round" strokeLinejoin="round" style={{ flexShrink: 0 }}>
    <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
  </svg>
}
function LockIcon() {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
    strokeLinecap="round" strokeLinejoin="round" style={{ flexShrink: 0 }}>
    <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0110 0v4"/>
  </svg>
}
function InfoIcon() {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
    strokeLinecap="round" strokeLinejoin="round" style={{ flexShrink: 0 }}>
    <circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>
  </svg>
}
function ClockIcon() {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
    strokeLinecap="round" strokeLinejoin="round" style={{ flexShrink: 0 }}>
    <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
  </svg>
}
