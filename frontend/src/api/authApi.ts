import { apiRequest } from './apiClient.ts'

export interface RegisterRequest {
  email: string
  password: string
}

export interface RegisterResponse {
  userId: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresAt: string
}

export function register(request: RegisterRequest) {
  return apiRequest<RegisterResponse>('/api/auth/register', {
    method: 'POST',
    body: request,
  })
}

export function login(request: LoginRequest) {
  return apiRequest<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: request,
  })
}
