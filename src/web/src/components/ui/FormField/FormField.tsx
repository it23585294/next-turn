/**
 * FormField — Accessible labeled input wrapper.
 * Renders: label + (optional hint) + input (via children) + error message.
 */
import React from 'react'
import styles from './FormField.module.css'

interface FormFieldProps {
  label: string
  htmlFor: string
  error?: string
  hint?: string
  required?: boolean
  children: React.ReactNode
}

export function FormField({ label, htmlFor, error, hint, required, children }: FormFieldProps) {
  return (
    <div className={`${styles.field} ${error ? styles.hasError : ''}`}>
      <label className={styles.label} htmlFor={htmlFor}>
        {label}
        {required && <span className={styles.required} aria-hidden="true"> *</span>}
      </label>
      {hint && <p className={styles.hint}>{hint}</p>}
      {children}
      {error && (
        <p className={styles.error} role="alert" id={`${htmlFor}-error`}>
          <span className={styles.errorIcon} aria-hidden="true">⚠</span>
          {error}
        </p>
      )}
    </div>
  )
}
