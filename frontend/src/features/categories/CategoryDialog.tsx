import { useState, type FormEvent } from 'react'
import {
  createCategory,
  updateCategory,
  type CategoryResponse,
} from '../../api/categoriesApi.ts'
import { getErrorMessages } from '../../api/apiClient.ts'
import type { TransactionType } from '../../api/transactionsApi.ts'
import { FormErrors } from '../../components/Feedback.tsx'
import { ModalDialog } from '../../components/ModalDialog.tsx'

interface CategoryDialogProps {
  category?: CategoryResponse
  onClose: () => void
  onSaved: (message: string) => void
}

export function CategoryDialog({
  category,
  onClose,
  onSaved,
}: CategoryDialogProps) {
  const [name, setName] = useState(category?.name ?? '')
  const [type, setType] = useState<TransactionType>(category?.type ?? 'Expense')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const isEditing = category !== undefined

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (isSubmitting) {
      return
    }

    if (!name.trim()) {
      setErrors(['Name is required.'])
      return
    }

    setErrors([])
    setIsSubmitting(true)

    try {
      const request = { name: name.trim(), type }

      if (category) {
        await updateCategory(category.id, request)
      } else {
        await createCategory(request)
      }

      onSaved(isEditing ? 'Category updated.' : 'Category created.')
    } catch (error) {
      setErrors(
        getErrorMessages(error, 'Unable to save the category. Please try again.'),
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <ModalDialog
      title={isEditing ? 'Edit category' : 'Create category'}
      description="Categories keep your financial activity organized."
      isSubmitting={isSubmitting}
      onClose={onClose}
    >
      <FormErrors errors={errors} id="category-form-errors" />
      <form className="resource-form" onSubmit={handleSubmit} noValidate>
        <div className="form-field">
          <label htmlFor="category-name">Name</label>
          <input
            id="category-name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            aria-describedby={errors.length > 0 ? 'category-form-errors' : undefined}
            autoFocus
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="form-field">
          <label htmlFor="category-type">Type</label>
          <select
            id="category-type"
            value={type}
            onChange={(event) => setType(event.target.value as TransactionType)}
            disabled={isSubmitting}
          >
            <option value="Income">Income</option>
            <option value="Expense">Expense</option>
          </select>
        </div>
        <div className="dialog-actions">
          <button
            className="button button--secondary"
            type="button"
            disabled={isSubmitting}
            onClick={onClose}
          >
            Cancel
          </button>
          <button className="button button--primary" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : isEditing ? 'Save changes' : 'Create category'}
          </button>
        </div>
      </form>
    </ModalDialog>
  )
}
