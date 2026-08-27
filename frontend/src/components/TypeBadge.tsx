import type { TransactionType } from '../api/transactionsApi.ts'
import { transactionTypeLabel } from '../utils/formatters.ts'

export function TypeBadge({ type }: { type: TransactionType }) {
  return (
    <span className={`type-badge type-badge--${type.toLowerCase()}`}>
      {transactionTypeLabel(type)}
    </span>
  )
}
