import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { getErrorMessages } from '../api/apiClient.ts'
import {
  getDashboard,
  type DashboardResponse,
} from '../api/dashboardApi.ts'
import { EmptyState, ErrorState } from '../components/Feedback.tsx'
import { PageSkeleton } from '../components/LoadingSkeleton.tsx'
import { PageHeader } from '../components/PageHeader.tsx'
import { DailyFlowChart } from '../features/dashboard/DailyFlowChart.tsx'
import { ExpenseDistribution } from '../features/dashboard/ExpenseDistribution.tsx'
import {
  currentPeriod,
  formatMoney,
  formatMonthYear,
  formatPercentage,
  monthOptions,
} from '../utils/formatters.ts'

export function DashboardPage() {
  const initialPeriod = currentPeriod()
  const [month, setMonth] = useState(initialPeriod.month)
  const [year, setYear] = useState(initialPeriod.year)
  const [draftMonth, setDraftMonth] = useState(initialPeriod.month)
  const [draftYear, setDraftYear] = useState(initialPeriod.year.toString())
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null)
  const [errors, setErrors] = useState<string[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true

    async function loadDashboard() {
      try {
        const response = await getDashboard(month, year)
        if (!active) return
        setDashboard(response)
        setErrors([])
      } catch (error) {
        if (!active) return
        setErrors(
          getErrorMessages(error, 'Unable to load your dashboard. Please try again.'),
        )
      } finally {
        if (active) setIsLoading(false)
      }
    }

    void loadDashboard()
    return () => {
      active = false
    }
  }, [month, reloadKey, year])

  function selectPeriod(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const parsedYear = Number(draftYear)

    if (!Number.isInteger(parsedYear) || parsedYear <= 0) {
      setErrors(['Dashboard period is invalid.'])
      return
    }

    setIsLoading(true)
    setErrors([])
    setMonth(draftMonth)
    setYear(parsedYear)

    if (draftMonth === month && parsedYear === year) {
      setReloadKey((value) => value + 1)
    }
  }

  function retry() {
    setIsLoading(true)
    setReloadKey((value) => value + 1)
  }

  return (
    <main className="page-shell page-enter">
      <PageHeader
        eyebrow="Financial overview"
        title="Dashboard"
        description="Understand your month at a glance and keep your plans on course."
        actions={
          <form className="period-selector" onSubmit={selectPeriod}>
            <label className="sr-only" htmlFor="dashboard-month">Month</label>
            <select
              id="dashboard-month"
              value={draftMonth}
              onChange={(event) => setDraftMonth(Number(event.target.value))}
            >
              {monthOptions.map((option) => (
                <option value={option.value} key={option.value}>{option.label}</option>
              ))}
            </select>
            <label className="sr-only" htmlFor="dashboard-year">Year</label>
            <input
              id="dashboard-year"
              type="number"
              min="1"
              step="1"
              value={draftYear}
              onChange={(event) => setDraftYear(event.target.value)}
            />
            <button className="button button--secondary" type="submit">View</button>
          </form>
        }
      />

      {isLoading && <PageSkeleton rows={4} />}

      {!isLoading && errors.length > 0 && (
        <ErrorState messages={errors} onRetry={retry} />
      )}

      {!isLoading && errors.length === 0 && dashboard && (
        <DashboardContent dashboard={dashboard} />
      )}
    </main>
  )
}

function DashboardContent({ dashboard }: { dashboard: DashboardResponse }) {
  const { summary, budget } = dashboard
  const hasTransactions = summary.transactionCount > 0

  return (
    <div className="dashboard-content content-reveal">
      <div className="section-heading section-heading--compact">
        <div>
          <p className="eyebrow">Selected period</p>
          <h2>{formatMonthYear(dashboard.month, dashboard.year)}</h2>
        </div>
      </div>

      <section className="summary-grid" aria-label="Monthly financial summary">
        <SummaryCard label="Total income" value={formatMoney(summary.totalIncome)} tone="income" />
        <SummaryCard label="Total expenses" value={formatMoney(summary.totalExpenses)} tone="expense" />
        <SummaryCard
          label="Balance"
          value={formatMoney(summary.balance)}
          tone={summary.balance < 0 ? 'expense' : 'balance'}
        />
        <SummaryCard label="Transactions" value={summary.transactionCount.toString()} tone="neutral" />
      </section>

      {!hasTransactions && (
        <div className="dashboard-empty-note">
          <span aria-hidden="true">○</span>
          <div>
            <strong>This month is ready for your first entry.</strong>
            <p>Zero values are expected until income or expenses are recorded.</p>
          </div>
          <Link className="button button--secondary" to="/transactions">Add transaction</Link>
        </div>
      )}

      <section className="dashboard-section" aria-labelledby="budget-progress-title">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Monthly plan</p>
            <h2 id="budget-progress-title">Budget progress</h2>
          </div>
          <Link to="/budgets">Manage budgets</Link>
        </div>
        {budget ? <BudgetProgress budget={budget} /> : (
          <EmptyState
            compact
            title="No budget for this month"
            description="Create a monthly budget to compare your spending with a clear target."
            action={<Link className="button button--primary" to="/budgets">Create budget</Link>}
          />
        )}
      </section>

      <div className="dashboard-chart-grid">
        <section className="surface-card dashboard-section" aria-labelledby="expense-chart-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Spending mix</p>
              <h2 id="expense-chart-title">Expenses by category</h2>
            </div>
          </div>
          <ExpenseDistribution expenses={dashboard.expensesByCategory} />
        </section>

        <section className="surface-card dashboard-section" aria-labelledby="flow-chart-title">
          <div className="section-heading">
            <div>
              <p className="eyebrow">Daily movement</p>
              <h2 id="flow-chart-title">Financial flow</h2>
            </div>
          </div>
          <DailyFlowChart flow={dashboard.dailyFlow} />
        </section>
      </div>
    </div>
  )
}

function SummaryCard({
  label,
  value,
  tone,
}: {
  label: string
  value: string
  tone: 'income' | 'expense' | 'balance' | 'neutral'
}) {
  return (
    <article className={`summary-card summary-card--${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  )
}

function BudgetProgress({ budget }: { budget: NonNullable<DashboardResponse['budget']> }) {
  const progressWidth = Math.max(0, Math.min(100, budget.percentageUsed))

  return (
    <div className={`budget-progress${budget.isExceeded ? ' budget-progress--exceeded' : ''}`}>
      <div className="budget-progress__metrics">
        <div><span>Budget</span><strong>{formatMoney(budget.amount)}</strong></div>
        <div><span>Spent</span><strong>{formatMoney(budget.spent)}</strong></div>
        <div>
          <span>Remaining</span>
          <strong className={budget.remaining < 0 ? 'money-negative' : ''}>
            {formatMoney(budget.remaining)}
          </strong>
        </div>
      </div>
      <div className="budget-progress__label">
        <span>{budget.isExceeded ? 'Budget exceeded' : 'Budget used'}</span>
        <strong>{formatPercentage(budget.percentageUsed)}</strong>
      </div>
      <div
        className="budget-progress__track"
        role="progressbar"
        aria-label="Budget used"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(progressWidth)}
      >
        <span style={{ width: `${progressWidth}%` }} />
      </div>
    </div>
  )
}
