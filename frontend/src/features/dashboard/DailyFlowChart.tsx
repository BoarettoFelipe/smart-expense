import type { DashboardDailyFlow } from '../../api/dashboardApi.ts'
import { formatMoney, formatShortDate } from '../../utils/formatters.ts'

export function DailyFlowChart({ flow }: { flow: DashboardDailyFlow[] }) {
  if (flow.length === 0) {
    return (
      <div className="chart-empty">
        <p>No daily activity in this period.</p>
        <span>Daily income and expenses will appear after transactions are recorded.</span>
      </div>
    )
  }

  const maximum = Math.max(
    1,
    ...flow.flatMap((day) => [day.income, day.expense]),
  )

  return (
    <div>
      <div className="chart-legend" aria-label="Chart legend">
        <span><i className="legend-swatch legend-swatch--income" />Income</span>
        <span><i className="legend-swatch legend-swatch--expense" />Expense</span>
      </div>
      <div className="daily-flow-scroll">
        <div className="daily-flow-chart" aria-label="Daily financial flow">
          {flow.map((day) => (
            <div
              role="img"
              className="daily-flow-day"
              key={day.date}
              aria-label={`${formatShortDate(day.date)}: income ${formatMoney(day.income)}, expense ${formatMoney(day.expense)}, net ${formatMoney(day.net)}`}
            >
              <div className="daily-flow-bars" aria-hidden="true">
                <span
                  className="daily-flow-bar daily-flow-bar--income"
                  style={{ height: `${barHeight(day.income, maximum)}%` }}
                />
                <span
                  className="daily-flow-bar daily-flow-bar--expense"
                  style={{ height: `${barHeight(day.expense, maximum)}%` }}
                />
              </div>
              <strong>{formatShortDate(day.date)}</strong>
              <small className={day.net < 0 ? 'money-negative' : 'money-positive'}>
                {formatMoney(day.net)}
              </small>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

function barHeight(value: number, maximum: number): number {
  return value === 0 ? 2 : Math.max(7, (value / maximum) * 100)
}
