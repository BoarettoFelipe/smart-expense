import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { getErrorMessages } from '../api/apiClient.ts'
import { getCategories, type CategoryResponse } from '../api/categoriesApi.ts'
import {
  deleteTransaction,
  getTransactions,
  type TransactionResponse,
} from '../api/transactionsApi.ts'
import { ConfirmDialog } from '../components/ConfirmDialog.tsx'
import { EmptyState, ErrorState, SuccessFeedback } from '../components/Feedback.tsx'
import { PageSkeleton } from '../components/LoadingSkeleton.tsx'
import { PageHeader } from '../components/PageHeader.tsx'
import { TypeBadge } from '../components/TypeBadge.tsx'
import { TransactionDialog } from '../features/transactions/TransactionDialog.tsx'
import { formatDate, formatMoney } from '../utils/formatters.ts'

type TransactionDialogState =
  | { mode: 'create' }
  | { mode: 'edit'; transaction: TransactionResponse }

export function TransactionsPage() {
  const [transactions, setTransactions] = useState<TransactionResponse[]>([])
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadKey, setLoadKey] = useState(0)
  const [dialog, setDialog] = useState<TransactionDialogState | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<TransactionResponse | null>(null)
  const [deleteErrors, setDeleteErrors] = useState<string[]>([])
  const [isDeleting, setIsDeleting] = useState(false)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function load() {
      try {
        const [transactionResponse, categoryResponse] = await Promise.all([
          getTransactions(),
          getCategories(),
        ])
        if (!active) return
        setTransactions(
          transactionResponse.toSorted(
            (a, b) => b.date.localeCompare(a.date) || b.createdAt.localeCompare(a.createdAt),
          ),
        )
        setCategories(categoryResponse.toSorted((a, b) => a.name.localeCompare(b.name)))
        setErrors([])
      } catch (error) {
        if (!active) return
        setErrors(
          getErrorMessages(error, 'Unable to load transactions. Please try again.'),
        )
      } finally {
        if (active) setIsLoading(false)
      }
    }

    void load()
    return () => { active = false }
  }, [loadKey])

  const categoryNames = useMemo(
    () => new Map(categories.map((category) => [category.id, category.name])),
    [categories],
  )

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
      await deleteTransaction(deleteTarget.id)
      refresh('Transaction deleted.')
    } catch (error) {
      setDeleteErrors(
        getErrorMessages(error, 'Unable to delete the transaction. Please try again.'),
      )
    } finally {
      setIsDeleting(false)
    }
  }

  const createAction = categories.length > 0 ? (
    <button className="button button--primary" type="button" onClick={() => setDialog({ mode: 'create' })}>
      + New transaction
    </button>
  ) : undefined

  return (
    <main className="page-shell page-enter">
      <PageHeader
        eyebrow="Financial activity"
        title="Transactions"
        description="Record and review the income and expenses that shape your month."
        actions={createAction}
      />
      <SuccessFeedback message={success} onDismiss={() => setSuccess(null)} />

      {isLoading && <PageSkeleton rows={6} />}
      {!isLoading && errors.length > 0 && (
        <ErrorState messages={errors} onRetry={() => refresh()} />
      )}
      {!isLoading && errors.length === 0 && categories.length === 0 && (
        <EmptyState
          title="Create a category first"
          description="Every transaction needs a category. Add one before recording financial activity."
          action={<Link className="button button--primary" to="/categories">Go to categories</Link>}
        />
      )}
      {!isLoading && errors.length === 0 && categories.length > 0 && transactions.length === 0 && (
        <EmptyState
          title="Your transactions will appear here"
          description="Add your first income or expense to begin building your financial history."
          action={<button className="button button--primary" type="button" onClick={() => setDialog({ mode: 'create' })}>Create transaction</button>}
        />
      )}
      {!isLoading && errors.length === 0 && transactions.length > 0 && (
        <TransactionList
          transactions={transactions}
          categoryNames={categoryNames}
          onEdit={(transaction) => setDialog({ mode: 'edit', transaction })}
          onDelete={(transaction) => { setDeleteErrors([]); setDeleteTarget(transaction) }}
        />
      )}

      {dialog && (
        <TransactionDialog
          key={dialog.mode === 'edit' ? dialog.transaction.id : 'new-transaction'}
          transaction={dialog.mode === 'edit' ? dialog.transaction : undefined}
          categories={categories}
          onClose={() => setDialog(null)}
          onSaved={refresh}
        />
      )}
      {deleteTarget && (
        <ConfirmDialog
          title="Delete transaction?"
          description={`“${deleteTarget.description}” for ${formatMoney(deleteTarget.amount)} will be permanently deleted.`}
          isSubmitting={isDeleting}
          errors={deleteErrors}
          onClose={() => { if (!isDeleting) setDeleteTarget(null) }}
          onConfirm={() => void confirmDelete()}
        />
      )}
    </main>
  )
}

function TransactionList({
  transactions,
  categoryNames,
  onEdit,
  onDelete,
}: {
  transactions: TransactionResponse[]
  categoryNames: Map<string, string>
  onEdit: (transaction: TransactionResponse) => void
  onDelete: (transaction: TransactionResponse) => void
}) {
  return (
    <section className="surface-card resource-list content-reveal" aria-label="Transactions">
      <div className="desktop-table-wrapper">
        <table className="data-table">
          <thead>
            <tr>
              <th>Description</th>
              <th>Date</th>
              <th>Category</th>
              <th>Type</th>
              <th className="align-right">Amount</th>
              <th><span className="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((transaction) => (
              <tr key={transaction.id}>
                <td><strong>{transaction.description}</strong></td>
                <td>{formatDate(transaction.date)}</td>
                <td>{categoryNames.get(transaction.categoryId) ?? 'Unavailable category'}</td>
                <td><TypeBadge type={transaction.type} /></td>
                <td className={`align-right amount-cell amount-cell--${transaction.type.toLowerCase()}`}>
                  {transaction.type === 'Expense' ? '−' : '+'}{formatMoney(transaction.amount)}
                </td>
                <td>
                  <div className="row-actions row-actions--end">
                    <button className="text-button" type="button" onClick={() => onEdit(transaction)}>Edit</button>
                    <button className="text-button text-button--danger" type="button" onClick={() => onDelete(transaction)}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mobile-card-list">
        {transactions.map((transaction) => (
          <article className="mobile-resource-card" key={transaction.id}>
            <div className="mobile-resource-card__topline">
              <TypeBadge type={transaction.type} />
              <time dateTime={transaction.date}>{formatDate(transaction.date)}</time>
            </div>
            <h2>{transaction.description}</h2>
            <p>{categoryNames.get(transaction.categoryId) ?? 'Unavailable category'}</p>
            <strong className={`mobile-amount amount-cell--${transaction.type.toLowerCase()}`}>
              {transaction.type === 'Expense' ? '−' : '+'}{formatMoney(transaction.amount)}
            </strong>
            <div className="row-actions">
              <button className="text-button" type="button" onClick={() => onEdit(transaction)}>Edit</button>
              <button className="text-button text-button--danger" type="button" onClick={() => onDelete(transaction)}>Delete</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}
