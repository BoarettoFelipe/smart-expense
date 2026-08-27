import type { DashboardCategoryExpense } from '../../api/dashboardApi.ts'
import { formatMoney, formatPercentage } from '../../utils/formatters.ts'

export function ExpenseDistribution({
  expenses,
}: {
  expenses: DashboardCategoryExpense[]
}) {
  if (expenses.length === 0) {
    return (
      <div className="chart-empty">
        <p>No expense activity in this period.</p>
        <span>Category distribution will appear after expenses are recorded.</span>
      </div>
    )
  }

  return (
    <div className="distribution-chart" aria-label="Expense distribution by category">
      {expenses.map((expense) => (
        <div className="distribution-row" key={expense.categoryId}>
          <div className="distribution-row__label">
            <strong>{expense.categoryName}</strong>
            <span>
              {formatMoney(expense.amount)} ·{' '}
              {formatPercentage(expense.percentageOfTotalExpenses)}
            </span>
          </div>
          <div
            className="distribution-track"
            role="img"
            aria-label={`${expense.categoryName}: ${formatPercentage(expense.percentageOfTotalExpenses)} of expenses`}
          >
            <span
              style={{
                width: `${Math.max(2, Math.min(100, expense.percentageOfTotalExpenses))}%`,
              }}
            />
          </div>
        </div>
      ))}
    </div>
  )
}
