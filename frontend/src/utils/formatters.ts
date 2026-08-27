import type { TransactionType } from '../api/transactionsApi.ts'

const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

const percentageFormatter = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 1,
})

const monthFormatter = new Intl.DateTimeFormat('en', { month: 'long' })
const dateFormatter = new Intl.DateTimeFormat('en', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})
const shortDateFormatter = new Intl.DateTimeFormat('en', {
  day: '2-digit',
  month: 'short',
})

export const monthOptions = Array.from({ length: 12 }, (_, index) => ({
  value: index + 1,
  label: monthFormatter.format(new Date(2026, index, 1)),
}))

export function formatMoney(value: number): string {
  return currencyFormatter.format(value)
}

export function formatPercentage(value: number): string {
  return `${percentageFormatter.format(value)}%`
}

export function formatDate(value: string): string {
  return dateFormatter.format(parseDateOnly(value))
}

export function formatShortDate(value: string): string {
  return shortDateFormatter.format(parseDateOnly(value))
}

export function formatTimestamp(value: string): string {
  return dateFormatter.format(new Date(value))
}

export function formatMonthYear(month: number, year: number): string {
  const monthName = monthOptions.find((option) => option.value === month)?.label
  return `${monthName ?? month} ${year}`
}

export function transactionTypeLabel(type: TransactionType): string {
  return type === 'Income' ? 'Income' : 'Expense'
}

export function currentPeriod(): { month: number; year: number } {
  const today = new Date()
  return { month: today.getMonth() + 1, year: today.getFullYear() }
}

export function todayAsDateInput(): string {
  const today = new Date()
  const month = String(today.getMonth() + 1).padStart(2, '0')
  const day = String(today.getDate()).padStart(2, '0')
  return `${today.getFullYear()}-${month}-${day}`
}

function parseDateOnly(value: string): Date {
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year, month - 1, day)
}
