export type LoginResponse = {
  id: string
  username: string
  primaryPersonaId?: string | null
  accessToken: string
  expiresAt: string
  refreshToken: string
}

const API_BASE = import.meta.env.VITE_API_BASE_URL || ''

export class ApiError extends Error {
  status?: number
  url: string
  bodyText?: string
  isNetworkError?: boolean
  constructor(message: string, url: string, status?: number, bodyText?: string, isNetworkError?: boolean) {
    super(message)
    this.name = 'ApiError'
    this.url = url
    this.status = status
    this.bodyText = bodyText
    this.isNetworkError = isNetworkError
  }
}

function joinUrl(base: string, path: string) {
  if (!base) return path
  if (base.endsWith('/') && path.startsWith('/')) return base.slice(0, -1) + path
  if (!base.endsWith('/') && !path.startsWith('/')) return base + '/' + path
  return base + path
}

async function request<T>(path: string, options: RequestInit = {}, accessToken?: string): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> | undefined)
  }
  if (accessToken) headers['Authorization'] = `Bearer ${accessToken}`

  const url = joinUrl(API_BASE, path)
  let res: Response
  try {
    res = await fetch(url, { ...options, headers })
  } catch (e: any) {
    const hint = navigator.onLine ? '' : ' (offline)'
    throw new ApiError(`Network error contacting API${hint}`, url, undefined, undefined, true)
  }
  if (!res.ok) {
    let text = ''
    try { text = await res.text() } catch {}
    const msg = text || `HTTP ${res.status}`
    throw new ApiError(msg, url, res.status, text)
  }
  const ct = res.headers.get('content-type') || ''
  if (ct.includes('application/json')) return (await res.json()) as T
  // @ts-ignore allow void
  return undefined
}

export const api = {
  register: (username: string, password: string, email?: string) =>
    request<{ id: string; username: string; primaryPersonaId?: string | null }>(
      '/api/users/register',
      { method: 'POST', body: JSON.stringify({ Username: username, Password: password, Email: email }) }
    ),
  login: (username: string, password: string) =>
    request<LoginResponse>('/api/users/login', {
      method: 'POST',
      body: JSON.stringify({ Username: username, Password: password })
    }),

  refresh: (refreshToken: string) =>
    request<{ accessToken: string; expiresAt: string; refreshToken: string }>(
      '/api/users/refresh',
      { method: 'POST', body: JSON.stringify(refreshToken) }
    ),

  getUser: (id: string, accessToken?: string) =>
    request<{ id: string; username: string; email?: string; primaryPersonaId?: string | null }>(
      `/api/users/${id}`,
      {},
      accessToken
    ),

  me: (accessToken: string) =>
    request<{ id: string; username: string; email?: string; primaryPersonaId?: string | null }>(
      '/api/users/me',
      {},
      accessToken
    ),

  updateUser: (id: string, data: { email?: string | null; username?: string }, accessToken: string) =>
    request<void>(
      `/api/users/${id}`,
      { method: 'PATCH', body: JSON.stringify({ Email: data.email ?? null, Username: data.username }) },
      accessToken
    ),

  changePassword: (id: string, currentPassword: string, newPassword: string, accessToken: string) =>
    request<void>(
      `/api/users/${id}/password`,
      {
        method: 'PATCH',
        body: JSON.stringify({ CurrentPassword: currentPassword, NewPassword: newPassword })
      },
      accessToken
    ),

  // type comes from backend enum: 0 = User, 1 = Assistant (or string if enum serialized as string)
  listPersonas: (accessToken?: string) => request<Array<{ id: string; name: string; type?: number | 'User' | 'Assistant' }>>('/api/personas', {}, accessToken),
  getPersona: (id: string, accessToken?: string) => request<any>(`/api/personas/${id}`, {}, accessToken),
  createPersona: (
    payload: {
      Name: string
      Nickname?: string
      Role?: string
      Gender?: string
      Essence?: string
      Beliefs?: string
      Background?: string
      CommunicationStyle?: string
      EmotionalDrivers?: string
      SignatureTraits?: string[]
      NarrativeThemes?: string[]
      DomainExpertise?: string[]
      IsPublic?: boolean
    },
    accessToken?: string
  ) => request<{ id: string }>(`/api/personas`, { method: 'POST', body: JSON.stringify(payload) }, accessToken),
  updatePersona: (id: string, payload: any, accessToken?: string) =>
    request<void>(`/api/personas/${id}`, { method: 'PATCH', body: JSON.stringify(payload) }, accessToken),
  deletePersona: (id: string, accessToken?: string) => request<void>(`/api/personas/${id}`, { method: 'DELETE' }, accessToken),

  setPrimaryPersona: (userId: string, personaId: string, accessToken: string) =>
    request<void>(
      `/api/users/${userId}/primary-persona`,
      { method: 'PATCH', body: JSON.stringify({ PersonaId: personaId }) },
      accessToken
    )
}
