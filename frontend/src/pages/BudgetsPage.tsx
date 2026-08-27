import { useEffect, useState } from 'react'
import { getErrorMessages } from '../api/apiClient.ts'
import {
  deleteBudget,
  getBudgets,
  type BudgetResponse,
} from '../api/budgetsApi.ts'
import { ConfirmDialog } from '../components/ConfirmDialog.tsx'
import { EmptyState, ErrorState, SuccessFeedback } from '../components/Feedback.tsx'
import { PageSkeleton } from '../components/LoadingSkeleton.tsx'
import { PageHeader } from '../components/PageHeader.tsx'
import { BudgetDialog } from '../features/budgets/BudgetDialog.tsx'
import { formatMoney, formatMonthYear } from '../utils/formatters.ts'

type BudgetDialogState = { mode: 'create' } | { mode: 'edit'; budget: BudgetResponse }

export function BudgetsPage() {
  const [budgets, setBudgets] = useState<BudgetResponse[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadKey, setLoadKey] = useState(0)
  const [dialog, setDialog] = useState<BudgetDialogState | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<BudgetResponse | null>(null)
  const [deleteErrors, setDeleteErrors] = useState<string[]>([])
  const [isDeleting, setIsDeleting] = useState(false)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function load() {
      try {
        const response = await getBudgets()
        if (!active) return
        setBudgets(response.toSorted((a, b) => b.year - a.year || b.month - a.month))
        setErrors([])
      } catch (error) {
        if (!active) return
        setErrors(getErrorMessages(error, 'Unable to load budgets. Please try again.'))
      } finally {
        if (active) setIsLoading(false)
      }
    }

    void load()
    return () => { active = false }
  }, [loadKey])

  function refresh(message?: string) {
    setDialog(null)
    setDeleteTarget(null)
    if (message) setSuccess(message)
    setIsLoading(true)
    setLoadKey((value) => value + 1)
  }

  async function confirmDelete() {
    if (!deleteTarget || isDeleting) return
    setDeleteErrors([])
    setIsDeleting(true)

    try {
      await deleteBudget(deleteTarget.id)
      refresh('Budget deleted.')
    } catch (error) {
      setDeleteErrors(
        getErrorMessages(error, 'Unable to delete the budget. Please try again.'),
      )
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <main className="page-shell page-enter">
      <PageHeader
        eyebrow="Monthly planning"
        title="Budgets"
        description="Set intentional monthly limits and adjust them as your plans change."
        actions={
          <button className="button button--primary" type="button" onClick={() => setDialog({ mode: 'create' })}>
            + New budget
          </button>
        }
      />
      <SuccessFeedback message={success} onDismiss={() => setSuccess(null)} />

      {isLoading && <PageSkeleton />}
      {!isLoading && errors.length > 0 && (
        <ErrorState messages={errors} onRetry={() => refresh()} />
      )}
      {!isLoading && errors.length === 0 && budgets.length === 0 && (
        <EmptyState
          title="Plan your first month"
          description="Create a budget to give your monthly spending a clear target."
          action={<button className="button button--primary" type="button" onClick={() => setDialog({ mode: 'create' })}>Create budget</button>}
        />
      )}
      {!isLoading && errors.length === 0 && budgets.length > 0 && (
        <section className="resource-grid content-reveal" aria-label="Budgets">
          {budgets.map((budget) => (
            <article className="resource-card budget-card" key={budget.id}>
              <div className="resource-card__header">
                <span className="period-chip">{budget.year}</span>
                <div className="row-actions">
                  <button className="text-button" type="button" onClick={() => setDialog({ mode: 'edit', budget })}>Edit</button>
                  <button className="text-button text-button--danger" type="button" onClick={() => { setDeleteErrors([]); setDeleteTarget(budget) }}>Delete</button>
                </div>
              </div>
              <p>{formatMonthYear(budget.month, budget.year)}</p>
              <h2>{formatMoney(budget.amount)}</h2>
            </article>
          ))}
        </section>
      )}

      {dialog && (
        <BudgetDialog
          key={dialog.mode === 'edit' ? dialog.budget.id : 'new-budget'}
          budget={dialog.mode === 'edit' ? dialog.budget : undefined}
          onClose={() => setDialog(null)}
          onSaved={refresh}
        />
      )}
      {deleteTarget && (
        <ConfirmDialog
          title="Delete budget?"
          description={`The ${formatMonthYear(deleteTarget.month, deleteTarget.year)} budget of ${formatMoney(deleteTarget.amount)} will be permanently deleted.`}
          isSubmitting={isDeleting}
          errors={deleteErrors}
          onClose={() => { if (!isDeleting) setDeleteTarget(null) }}
          onConfirm={() => void confirmDelete()}
        />
      )}
    </main>
  )
}
