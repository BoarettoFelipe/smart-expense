import { apiRequest } from './apiClient.ts'
import type { TransactionType } from './transactionsApi.ts'

export interface CategoryResponse {
  id: string
  name: string
  type: TransactionType
  createdAt: string
}

export interface CategoryRequest {
  name: string
  type: TransactionType
}

export function getCategories() {
  return apiRequest<CategoryResponse[]>('/api/categories')
}

export function getCategory(id: string) {
  return apiRequest<CategoryResponse>(`/api/categories/${id}`)
}

export function createCategory(request: CategoryRequest) {
  return apiRequest<CategoryResponse>('/api/categories', {
    method: 'POST',
    body: request,
  })
}

export function updateCategory(id: string, request: CategoryRequest) {
  return apiRequest<CategoryResponse>(`/api/categories/${id}`, {
    method: 'PUT',
    body: request,
  })
}

export function deleteCategory(id: string) {
  return apiRequest<void>(`/api/categories/${id}`, { method: 'DELETE' })
}
