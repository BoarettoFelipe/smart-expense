import { useEffect, useId, useRef, type ReactNode } from 'react'

interface ModalDialogProps {
  title: string
  description?: string
  children: ReactNode
  isSubmitting?: boolean
  onClose: () => void
  size?: 'small' | 'medium'
}

export function ModalDialog({
  title,
  description,
  children,
  isSubmitting = false,
  onClose,
  size = 'medium',
}: ModalDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  const descriptionId = useId()

  useEffect(() => {
    const dialog = dialogRef.current

    if (!dialog) {
      return undefined
    }

    dialog.showModal()
    return () => {
      if (dialog.open) {
        dialog.close()
      }
    }
  }, [])

  return (
    <dialog
      className={`modal-dialog modal-dialog--${size}`}
      ref={dialogRef}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        if (isSubmitting) {
          event.preventDefault()
          return
        }

        onClose()
      }}
    >
      <div className="modal-dialog__header">
        <div>
          <h2 id={titleId}>{title}</h2>
          {description && <p id={descriptionId}>{description}</p>}
        </div>
        <button
          className="icon-button"
          type="button"
          aria-label="Close dialog"
          disabled={isSubmitting}
          onClick={onClose}
        >
          ×
        </button>
      </div>
      {children}
    </dialog>
  )
}
