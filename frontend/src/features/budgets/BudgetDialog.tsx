import { useState, type FormEvent } from 'react'
import { getErrorMessages } from '../../api/apiClient.ts'
import {
  createBudget,
  updateBudget,
  type BudgetResponse,
} from '../../api/budgetsApi.ts'
import { FormErrors } from '../../components/Feedback.tsx'
import { ModalDialog } from '../../components/ModalDialog.tsx'
import { currentPeriod, monthOptions } from '../../utils/formatters.ts'

interface BudgetDialogProps {
  budget?: BudgetResponse
  onClose: () => void
  onSaved: (message: string) => void
}

export function BudgetDialog({ budget, onClose, onSaved }: BudgetDialogProps) {
  const defaultPeriod = currentPeriod()
  const [month, setMonth] = useState(budget?.month ?? defaultPeriod.month)
  const [year, setYear] = useState(budget?.year.toString() ?? defaultPeriod.year.toString())
  const [amount, setAmount] = useState(budget?.amount.toString() ?? '')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const isEditing = budget !== undefined

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSubmitting) {
      return
    }

    const parsedYear = Number(year)
    const parsedAmount = Number(amount)
    const validationErrors: string[] = []

    if (!Number.isInteger(parsedYear) || parsedYear <= 0)
      validationErrors.push('Year must be greater than zero.')
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0)
      validationErrors.push('Amount must be greater than zero.')

    if (validationErrors.length > 0) {
      setErrors(validationErrors)
      return
    }

    setErrors([])
    setIsSubmitting(true)

    try {
      const request = { month, year: parsedYear, amount: parsedAmount }

      if (budget) {
        await updateBudget(budget.id, request)
      } else {
        await createBudget(request)
      }

      onSaved(isEditing ? 'Budget updated.' : 'Budget created.')
    } catch (error) {
      setErrors(
        getErrorMessages(error, 'Unable to save the budget. Please try again.'),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <ModalDialog
      title={isEditing ? 'Edit budget' : 'Create budget'}
      description="Set a spending target for one calendar month."
      isSubmitting={isSubmitting}
      onClose={onClose}
    >
      <FormErrors errors={errors} id="budget-form-errors" />
      <form className="resource-form" onSubmit={handleSubmit} noValidate>
        <div className="form-field">
          <label htmlFor="budget-month">Month</label>
          <select
            id="budget-month"
            value={month}
            onChange={(event) => setMonth(Number(event.target.value))}
            disabled={isSubmitting}
            autoFocus
          >
            {monthOptions.map((option) => (
              <option value={option.value} key={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="form-field">
          <label htmlFor="budget-year">Year</label>
          <input
            id="budget-year"
            type="number"
            min="1"
            step="1"
            value={year}
            onChange={(event) => setYear(event.target.value)}
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="form-field form-field--wide">
          <label htmlFor="budget-amount">Budget amount</label>
          <input
            id="budget-amount"
            type="number"
            min="0.01"
            step="0.01"
            inputMode="decimal"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            aria-describedby={errors.length > 0 ? 'budget-form-errors' : undefined}
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="dialog-actions form-field--wide">
          <button
            className="button button--secondary"
            type="button"
            disabled={isSubmitting}
            onClick={onClose}
          >
            Cancel
          </button>
          <button className="button button--primary" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : isEditing ? 'Save changes' : 'Create budget'}
          </button>
        </div>
      </form>
    </ModalDialog>
  )
}
