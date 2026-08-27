import { apiRequest } from './apiClient.ts'

export type TransactionType = 'Income' | 'Expense'

export interface TransactionResponse {
  id: string
  description: string
  amount: number
  type: TransactionType
  date: string
  categoryId: string
  createdAt: string
  updatedAt: string | null
}

export interface TransactionRequest {
  description: string
  amount: number
  type: TransactionType
  date: string
  categoryId: string
}

export function getTransactions() {
  return apiRequest<TransactionResponse[]>('/api/transactions')
}

export function getTransaction(id: string) {
  return apiRequest<TransactionResponse>(`/api/transactions/${id}`)
}

export function createTransaction(request: TransactionRequest) {
  return apiRequest<TransactionResponse>('/api/transactions', {
    method: 'POST',
    body: request,
  })
}

export function updateTransaction(id: string, request: TransactionRequest) {
  return apiRequest<TransactionResponse>(`/api/transactions/${id}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteTransaction(id: string) {
  return apiRequest<void>(`/api/transactions/${id}`, { method: 'DELETE' })
}
