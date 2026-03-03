/**
 * PasswordInput — Input with show/hide toggle for password fields.
 * Uses forwardRef so it works directly with react-hook-form register().
 */
import React, { forwardRef, useState } from 'react'
import styles from './PasswordInput.module.css'

interface PasswordInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  id: string
  hasError?: boolean
}

export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  ({ id, hasError, className, ...rest }, ref) => {
    const [visible, setVisible] = useState(false)

    return (
      <div className={styles.wrapper}>
        <input
          id={id}
          ref={ref}
          type={visible ? 'text' : 'password'}
          className={`${styles.input} ${hasError ? styles.error : ''} ${className ?? ''}`}
          aria-describedby={hasError ? `${id}-error` : undefined}
          {...rest}
        />
        <button
          type="button"
          className={styles.toggle}
          onClick={() => setVisible((v) => !v)}
          aria-label={visible ? 'Hide password' : 'Show password'}
          tabIndex={-1}
        >
          {visible ? <EyeOffIcon /> : <EyeIcon />}
        </button>
      </div>
    )
  }
)
PasswordInput.displayName = 'PasswordInput'

function EyeIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      aria-hidden="true">
      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
      <circle cx="12" cy="12" r="3"/>
    </svg>
  )
}

function EyeOffIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
      aria-hidden="true">
      <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/>
      <line x1="1" y1="1" x2="23" y2="23"/>
    </svg>
  )
}
