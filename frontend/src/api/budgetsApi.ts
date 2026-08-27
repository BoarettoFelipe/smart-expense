import { apiRequest } from './apiClient.ts'

export interface BudgetResponse {
  id: string
  month: number
  year: number
  amount: number
  createdAt: string
}

export interface BudgetRequest {
  month: number
  year: number
  amount: number
}

export function getBudgets() {
  return apiRequest<BudgetResponse[]>('/api/budgets')
}

export function getBudget(id: string) {
  return apiRequest<BudgetResponse>(`/api/budgets/${id}`)
}

export function createBudget(request: BudgetRequest) {
  return apiRequest<BudgetResponse>('/api/budgets', {
    method: 'POST',
    body: request,
  })
}

export function updateBudget(id: string, request: BudgetRequest) {
  return apiRequest<BudgetResponse>(`/api/budgets/${id}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteBudget(id: string) {
  return apiRequest<void>(`/api/budgets/${id}`, { method: 'DELETE' })
}
