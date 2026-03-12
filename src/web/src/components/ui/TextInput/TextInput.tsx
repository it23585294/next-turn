/**
 * TextInput — Branded text input. Forwards ref for react-hook-form compatibility.
 */
import React, { forwardRef } from 'react'
import styles from './TextInput.module.css'

interface TextInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  id: string
  hasError?: boolean
}

export const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
  ({ id, hasError, className, ...rest }, ref) => {
    return (
      <input
        id={id}
        ref={ref}
        className={`${styles.input} ${hasError ? styles.error : ''} ${className ?? ''}`}
        aria-describedby={hasError ? `${id}-error` : undefined}
        {...rest}
      />
    )
  }
)
TextInput.displayName = 'TextInput'
