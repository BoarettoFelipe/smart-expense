import { FormErrors } from './Feedback.tsx'
import { ModalDialog } from './ModalDialog.tsx'

interface ConfirmDialogProps {
  title: string
  description: string
  confirmLabel?: string
  isSubmitting: boolean
  errors: string[]
  onConfirm: () => void
  onClose: () => void
}

export function ConfirmDialog({
  title,
  description,
  confirmLabel = 'Delete',
  isSubmitting,
  errors,
  onConfirm,
  onClose,
}: ConfirmDialogProps) {
  return (
    <ModalDialog
      title={title}
      description={description}
      isSubmitting={isSubmitting}
      onClose={onClose}
      size="small"
    >
      <FormErrors errors={errors} id="confirmation-errors" />
      <div className="dialog-actions">
        <button
          className="button button--secondary"
          type="button"
          disabled={isSubmitting}
          onClick={onClose}
        >
          Cancel
        </button>
        <button
          className="button button--danger"
          type="button"
          disabled={isSubmitting}
          onClick={onConfirm}
        >
          {isSubmitting ? 'Deleting…' : confirmLabel}
        </button>
      </div>
    </ModalDialog>
  )
}
