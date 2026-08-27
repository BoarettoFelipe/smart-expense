import { useState, type FormEvent } from 'react'
import type { CategoryResponse } from '../../api/categoriesApi.ts'
import { getErrorMessages } from '../../api/apiClient.ts'
import {
  createTransaction,
  updateTransaction,
  type TransactionResponse,
  type TransactionType,
} from '../../api/transactionsApi.ts'
import { FormErrors } from '../../components/Feedback.tsx'
import { ModalDialog } from '../../components/ModalDialog.tsx'
import { todayAsDateInput } from '../../utils/formatters.ts'

interface TransactionDialogProps {
  transaction?: TransactionResponse
  categories: CategoryResponse[]
  onClose: () => void
  onSaved: (message: string) => void
}

export function TransactionDialog({
  transaction,
  categories,
  onClose,
  onSaved,
}: TransactionDialogProps) {
  const initialType = transaction?.type ?? 'Expense'
  const [description, setDescription] = useState(transaction?.description ?? '')
  const [amount, setAmount] = useState(transaction?.amount.toString() ?? '')
  const [type, setType] = useState<TransactionType>(initialType)
  const [date, setDate] = useState(transaction?.date ?? todayAsDateInput())
  const [categoryId, setCategoryId] = useState(() => {
    if (transaction) {
      const transactionCategory = categories.find(
        (category) => category.id === transaction.categoryId,
      )

      return transactionCategory?.type === transaction.type
        ? transaction.categoryId
        : ''
    }

    return categories.find((category) => category.type === initialType)?.id ?? ''
  })
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const isEditing = transaction !== undefined
  const compatibleCategories = categories.filter(
    (category) => category.type === type,
  )
  const hasCompatibleCategories = compatibleCategories.length > 0
  const categoryGuidance = !hasCompatibleCategories
    ? `No ${type.toLowerCase()} categories are available. Create a matching category in Categories before saving.`
    : !categoryId
      ? `Select a ${type.toLowerCase()} category that matches this transaction.`
      : 'Only categories matching the transaction type are shown.'

  function handleTypeChange(nextType: TransactionType) {
    setType(nextType)

    const selectedCategory = categories.find(
      (category) => category.id === categoryId,
    )

    if (selectedCategory?.type !== nextType) {
      setCategoryId('')
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSubmitting) {
      return
    }

    const parsedAmount = Number(amount)
    const validationErrors: string[] = []

    if (!description.trim()) validationErrors.push('Description is required.')
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0)
      validationErrors.push('Amount must be greater than zero.')
    if (!date) validationErrors.push('Date is required.')
    const selectedCategory = categories.find(
      (category) => category.id === categoryId,
    )
    if (!selectedCategory) {
      validationErrors.push('A matching category is required.')
    } else if (selectedCategory.type !== type) {
      validationErrors.push(
        'Transaction type must match the selected category type.',
      )
    }

    if (validationErrors.length > 0) {
      setErrors(validationErrors)
      return
    }

    setErrors([])
    setIsSubmitting(true)

    try {
      const request = {
        description: description.trim(),
        amount: parsedAmount,
        type,
        date,
        categoryId,
      }

      if (transaction) {
        await updateTransaction(transaction.id, request)
      } else {
        await createTransaction(request)
      }

      onSaved(isEditing ? 'Transaction updated.' : 'Transaction created.')
    } catch (error) {
      setErrors(
        getErrorMessages(
          error,
          'Unable to save the transaction. Please try again.',
        ),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <ModalDialog
      title={isEditing ? 'Edit transaction' : 'Create transaction'}
      description="Record an income or expense for your financial history."
      isSubmitting={isSubmitting}
      onClose={onClose}
    >
      <FormErrors errors={errors} id="transaction-form-errors" />
      <form className="resource-form" onSubmit={handleSubmit} noValidate>
        <div className="form-field form-field--wide">
          <label htmlFor="transaction-description">Description</label>
          <input
            id="transaction-description"
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            aria-describedby={
              errors.length > 0 ? 'transaction-form-errors' : undefined
            }
            autoFocus
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="form-field">
          <label htmlFor="transaction-amount">Amount</label>
          <input
            id="transaction-amount"
            type="number"
            min="0.01"
            step="0.01"
            inputMode="decimal"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="form-field">
          <label htmlFor="transaction-type">Type</label>
          <select
            id="transaction-type"
            value={type}
            onChange={(event) =>
              handleTypeChange(event.target.value as TransactionType)
            }
            disabled={isSubmitting}
          >
            <option value="Expense">Expense</option>
            <option value="Income">Income</option>
          </select>
        </div>
        <div className="form-field">
          <label htmlFor="transaction-date">Date</label>
          <input
            id="transaction-date"
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="form-field">
          <label htmlFor="transaction-category">Category</label>
          <select
            id="transaction-category"
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            disabled={isSubmitting || !hasCompatibleCategories}
            aria-describedby="transaction-category-guidance"
            required
          >
            <option value="">Select a {type.toLowerCase()} category</option>
            {compatibleCategories.map((category) => (
              <option value={category.id} key={category.id}>
                {category.name}
              </option>
            ))}
          </select>
          <small id="transaction-category-guidance">{categoryGuidance}</small>
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
          <button
            className="button button--primary"
            disabled={isSubmitting || !hasCompatibleCategories}
          >
            {isSubmitting
              ? 'Saving…'
              : isEditing
                ? 'Save changes'
                : 'Create transaction'}
          </button>
        </div>
      </form>
    </ModalDialog>
  )
}
