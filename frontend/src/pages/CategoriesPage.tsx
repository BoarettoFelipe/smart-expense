import { useEffect, useState } from 'react'
import { getErrorMessages } from '../api/apiClient.ts'
import {
  deleteCategory,
  getCategories,
  type CategoryResponse,
} from '../api/categoriesApi.ts'
import { ConfirmDialog } from '../components/ConfirmDialog.tsx'
import { EmptyState, ErrorState, SuccessFeedback } from '../components/Feedback.tsx'
import { PageSkeleton } from '../components/LoadingSkeleton.tsx'
import { PageHeader } from '../components/PageHeader.tsx'
import { TypeBadge } from '../components/TypeBadge.tsx'
import { CategoryDialog } from '../features/categories/CategoryDialog.tsx'
import { formatTimestamp } from '../utils/formatters.ts'

type CategoryDialogState = { mode: 'create' } | { mode: 'edit'; category: CategoryResponse }

export function CategoriesPage() {
  const [categories, setCategories] = useState<CategoryResponse[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadKey, setLoadKey] = useState(0)
  const [dialog, setDialog] = useState<CategoryDialogState | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<CategoryResponse | null>(null)
  const [deleteErrors, setDeleteErrors] = useState<string[]>([])
  const [isDeleting, setIsDeleting] = useState(false)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    async function load() {
      try {
        const response = await getCategories()
        if (!active) return
        setCategories(response.toSorted((a, b) => a.name.localeCompare(b.name)))
        setErrors([])
      } catch (error) {
        if (!active) return
        setErrors(getErrorMessages(error, 'Unable to load categories. Please try again.'))
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
      await deleteCategory(deleteTarget.id)
      refresh('Category deleted.')
    } catch (error) {
      setDeleteErrors(
        getErrorMessages(error, 'Unable to delete the category. Please try again.'),
      )
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <main className="page-shell page-enter">
      <PageHeader
        eyebrow="Organization"
        title="Categories"
        description="Create a clear structure for income and expense activity."
        actions={
          <button className="button button--primary" type="button" onClick={() => setDialog({ mode: 'create' })}>
            + New category
          </button>
        }
      />
      <SuccessFeedback message={success} onDismiss={() => setSuccess(null)} />

      {isLoading && <PageSkeleton />}
      {!isLoading && errors.length > 0 && (
        <ErrorState messages={errors} onRetry={() => refresh()} />
      )}
      {!isLoading && errors.length === 0 && categories.length === 0 && (
        <EmptyState
          title="Create your first category"
          description="Categories are required before you can record transactions."
          action={<button className="button button--primary" type="button" onClick={() => setDialog({ mode: 'create' })}>Create category</button>}
        />
      )}
      {!isLoading && errors.length === 0 && categories.length > 0 && (
        <section className="resource-grid content-reveal" aria-label="Categories">
          {categories.map((category) => (
            <article className="resource-card" key={category.id}>
              <div className="resource-card__header">
                <TypeBadge type={category.type} />
                <div className="row-actions">
                  <button className="text-button" type="button" onClick={() => setDialog({ mode: 'edit', category })}>Edit</button>
                  <button className="text-button text-button--danger" type="button" onClick={() => { setDeleteErrors([]); setDeleteTarget(category) }}>Delete</button>
                </div>
              </div>
              <h2>{category.name}</h2>
              <p>Created {formatTimestamp(category.createdAt)}</p>
            </article>
          ))}
        </section>
      )}

      {dialog && (
        <CategoryDialog
          key={dialog.mode === 'edit' ? dialog.category.id : 'new-category'}
          category={dialog.mode === 'edit' ? dialog.category : undefined}
          onClose={() => setDialog(null)}
          onSaved={refresh}
        />
      )}
      {deleteTarget && (
        <ConfirmDialog
          title="Delete category?"
          description={`“${deleteTarget.name}” will be permanently deleted. Categories used by transactions cannot be deleted.`}
          isSubmitting={isDeleting}
          errors={deleteErrors}
          onClose={() => { if (!isDeleting) setDeleteTarget(null) }}
          onConfirm={() => void confirmDelete()}
        />
      )}
    </main>
  )
}
