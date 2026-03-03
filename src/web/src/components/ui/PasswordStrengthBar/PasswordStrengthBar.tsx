/**
 * PasswordStrengthBar — Visual indicator for password complexity.
 * Score 0–4 mapped to: none / weak / fair / good / strong.
 */
import styles from './PasswordStrengthBar.module.css'

interface PasswordStrengthBarProps {
  score: number // 0–4
}

const LEVELS = [
  { label: '',       className: '' },
  { label: 'Weak',   className: styles.weak   },
  { label: 'Fair',   className: styles.fair   },
  { label: 'Good',   className: styles.good   },
  { label: 'Strong', className: styles.strong },
]

export function PasswordStrengthBar({ score }: PasswordStrengthBarProps) {
  if (score === 0) return null

  const level = LEVELS[score] ?? LEVELS[4]

  return (
    <div className={styles.container} role="status" aria-label={`Password strength: ${level.label}`}>
      <div className={styles.bars}>
        {[1, 2, 3, 4].map((i) => (
          <div
            key={i}
            className={`${styles.bar} ${i <= score ? level.className : styles.inactive}`}
          />
        ))}
      </div>
      <span className={`${styles.label} ${level.className}`}>{level.label}</span>
    </div>
  )
}
