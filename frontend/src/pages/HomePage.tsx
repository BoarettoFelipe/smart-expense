export function HomePage() {
  return (
    <main className="home-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Overview</p>
          <h1>Welcome to SmartExpense</h1>
          <p>Your financial overview will appear here.</p>
        </div>
        <span className="foundation-badge">Foundation ready</span>
      </header>

      <section className="placeholder-card" aria-labelledby="coming-next-heading">
        <div className="placeholder-card__icon" aria-hidden="true">
          <svg viewBox="0 0 24 24">
            <path d="M4 19h16v2H4v-2Zm1-3 4-5 3 3 5-7 2 1-6.7 9-3.1-3.1L7 17l-2-1Z" />
          </svg>
        </div>
        <div>
          <p className="eyebrow">Coming next</p>
          <h2 id="coming-next-heading">A clear view of your financial month</h2>
          <p>
            Your dashboard will bring income, expenses, budget progress, and
            daily activity together in one focused view.
          </p>
        </div>
      </section>

      <section className="foundation-grid" aria-label="SmartExpense features">
        <article>
          <span>01</span>
          <h3>Track activity</h3>
          <p>Keep income and expenses organized as your financial history grows.</p>
        </article>
        <article>
          <span>02</span>
          <h3>Set direction</h3>
          <p>Use monthly budgets to turn financial goals into practical limits.</p>
        </article>
        <article>
          <span>03</span>
          <h3>See patterns</h3>
          <p>Understand where your money goes without unnecessary complexity.</p>
        </article>
      </section>
    </main>
  )
}
