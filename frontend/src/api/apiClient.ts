import { getStoredAuthSession } from '../auth/authStorage.ts'

const connectionErrorMessage =
  'Unable to connect to SmartExpense. Please try again.'

type UnauthorizedHandler = () => void

let unauthorizedHandler: UnauthorizedHandler | null = null

export class ApiError extends Error {
  readonly status: number
  readonly errors: string[]

  constructor(message: string, status: number, errors: string[] = []) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
}

interface ParsedBackendError {
  message?: unknown
  errors?: unknown
}

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null) {
  unauthorizedHandler = handler
}

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const { body, headers: requestHeaders, ...requestOptions } = options
  const headers = new Headers(requestHeaders)
  const session = getStoredAuthSession()

  headers.set('Accept', 'application/json')

  if (body !== undefined) {
    headers.set('Content-Type', 'application/json')
  }

  if (session) {
    headers.set('Authorization', `Bearer ${session.accessToken}`)
  }

  let response: Response

  try {
    response = await fetch(path, {
      ...requestOptions,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(connectionErrorMessage, 0)
  }

  const responseBody = await parseResponseBody(response)

  if (!response.ok) {
    if (response.status === 401 && session) {
      unauthorizedHandler?.()
    }

    const parsedError = parseBackendError(responseBody)
    throw new ApiError(
      parsedError.message ?? defaultErrorMessage(response.status),
      response.status,
      parsedError.errors,
    )
  }

  return responseBody as T
}

export function getErrorMessages(error: unknown, fallback: string): string[] {
  if (!(error instanceof ApiError)) {
    return [fallback]
  }

  return [error.message, ...error.errors].filter(
    (message, index, messages) => messages.indexOf(message) === index,
  )
}

async function parseResponseBody(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined
  }

  const text = await response.text()

  if (!text) {
    return undefined
  }

  try {
    return JSON.parse(text) as unknown
  } catch {
    return undefined
  }
}

function parseBackendError(value: unknown): {
  message: string | null
  errors: string[]
} {
  if (!isRecord(value)) {
    return { message: null, errors: [] }
  }

  const backendError = value as ParsedBackendError
  const message =
    typeof backendError.message === 'string' ? backendError.message : null
  const errors = Array.isArray(backendError.errors)
    ? backendError.errors.flatMap(readErrorDescription)
    : []

  return { message, errors }
}

function readErrorDescription(value: unknown): string[] {
  if (typeof value === 'string') {
    return [value]
  }

  if (!isRecord(value)) {
    return []
  }

  if (typeof value.description === 'string') {
    return [value.description]
  }

  if (typeof value.message === 'string') {
    return [value.message]
  }

  return []
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function defaultErrorMessage(status: number): string {
  if (status === 401) {
    return 'Your session is no longer valid. Please sign in again.'
  }

  return 'SmartExpense could not complete the request. Please try again.'
}
