import { apiRequest } from './apiClient.ts'

export interface DashboardSummary {
  totalIncome: number
  totalExpenses: number
  balance: number
  transactionCount: number
}

export interface DashboardBudget {
  amount: number
  spent: number
  remaining: number
  percentageUsed: number
  isExceeded: boolean
}

export interface DashboardCategoryExpense {
  categoryId: string
  categoryName: string
  amount: number
  percentageOfTotalExpenses: number
}

export interface DashboardDailyFlow {
  date: string
  income: number
  expense: number
  net: number
}

export interface DashboardResponse {
  month: number
  year: number
  summary: DashboardSummary
  budget: DashboardBudget | null
  expensesByCategory: DashboardCategoryExpense[]
  dailyFlow: DashboardDailyFlow[]
}

export function getDashboard(month: number, year: number) {
  const query = new URLSearchParams({
    month: month.toString(),
    year: year.toString(),
  })

  return apiRequest<DashboardResponse>(`/api/dashboard?${query.toString()}`)
}
