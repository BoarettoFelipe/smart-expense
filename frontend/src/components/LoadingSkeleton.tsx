export function PageSkeleton({ rows = 5 }: { rows?: number }) {
  return (
    <div className="skeleton-page" role="status" aria-busy="true">
      <span className="sr-only">Loading content…</span>
      <div className="skeleton-cards">
        {Array.from({ length: 4 }, (_, index) => (
          <div className="skeleton skeleton--card" key={index} />
        ))}
      </div>
      <div className="surface-card skeleton-list">
        {Array.from({ length: rows }, (_, index) => (
          <div className="skeleton skeleton--row" key={index} />
        ))}
      </div>
    </div>
  )
}
