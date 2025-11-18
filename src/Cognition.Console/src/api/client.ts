import { OpenSearchDiagnosticsReport, PlannerHealthReport } from '../types/diagnostics';
import {
  BacklogActionLog,
  AuthorPersonaContext,
  AgentSummary,
  CreateFictionPlanPayload,
  FictionBacklogItem,
  FictionPlanRoster,
  FictionPlanSummary,
  FictionProjectSummary,
  FulfillLoreRequirementPayload,
  LoreFulfillmentLog,
  LoreBranchSummary,
  PersonaObligation,
  PersonaObligationListResponse,
  ResolvePersonaObligationPayload,
  ResumeBacklogPayload
} from '../types/fiction';

export async function fetchImageStyles(accessToken: string): Promise<any[]> {
  const headers: HeadersInit = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const res = await fetch('/api/image-styles', { headers });
  if (res.ok) return await res.json();
  return [];
}
export async function fetchConversations(accessToken: string, options?: { agentId?: string; personaId?: string }): Promise<any[]> {
  const headers = { Authorization: `Bearer ${accessToken}` };
  const params = new URLSearchParams();
  if (options?.agentId) {
    params.set('agentId', options.agentId);
  } else if (options?.personaId) {
    params.set('participantId', options.personaId);
  }
  const query = params.toString();
  const url = query ? `/api/conversations?${query}` : `/api/conversations`;
  const res = await fetch(url, { headers });
  if (res.ok) return await res.json();
  return [];
}

export async function fetchMessages(accessToken: string, conversationId: string): Promise<any[]> {
  const headers = { Authorization: `Bearer ${accessToken}` };
  const res = await fetch(`/api/conversations/${conversationId}/messages`, { headers });
  if (res.ok) return await res.json();
  return [];
}
export async function fetchProviders(accessToken: string): Promise<any[]> {
  const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` };
  const res = await fetch('/api/llm/providers', { headers });
  if (res.ok) return await res.json();
  return [];
}

export async function fetchModels(accessToken: string, providerId: string): Promise<any[]> {
  const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${accessToken}` };
  const res = await fetch(`/api/llm/providers/${providerId}/models`, { headers });
  if (res.ok) return await res.json();
  return [];
}
// Persona API client
export async function fetchPersonas(accessToken: string, userId: string): Promise<any[]> {
  const headers = { Authorization: `Bearer ${accessToken}` };
  const res = await fetch(`/api/users/${userId}/personas`, { headers });
  if (res.ok) {
    return await res.json();
  }
  // Fallback: global assistants
  const globalRes = await fetch('/api/personas', { headers });
  if (globalRes.ok) {
    return await globalRes.json();
  }
  return [];
}

export type LoginResponse = {
  id: string
  username: string
  primaryPersonaId?: string | null
  accessToken: string
  expiresAt: string
  refreshToken: string
}
// Vite exposes env variables via import.meta.env
const API_BASE = (import.meta as any).env?.VITE_API_BASE_URL || '';

export class ApiError extends Error {
  status?: number
  url: string
  bodyText?: string
  isNetworkError?: boolean
  code?: string
  constructor(message: string, url: string, status?: number, bodyText?: string, isNetworkError?: boolean, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.url = url
    this.status = status
    this.bodyText = bodyText
    this.isNetworkError = isNetworkError
    this.code = code
  }
}

function joinUrl(base: string, path: string) {
  if (!base) return path
  if (base.endsWith('/') && path.startsWith('/')) return base.slice(0, -1) + path
  if (!base.endsWith('/') && !path.startsWith('/')) return base + '/' + path
  return base + path
}

export async function request<T>(path: string, options: RequestInit = {}, accessToken?: string): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Console-Client': 'cognition-console',
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
    let code: string | undefined
    let message = `HTTP ${res.status}`
    try {
      text = await res.text()
      if (text) {
        try {
          const parsed = JSON.parse(text)
          if (parsed && typeof parsed.message === 'string') {
            message = parsed.message
          } else if (text) {
            message = text
          }
          if (parsed && typeof parsed.code === 'string') {
            code = parsed.code
          }
        } catch {
          message = text
        }
      }
    } catch {
      // ignore
    }
    throw new ApiError(message, url, res.status, text, false, code)
  }
  const ct = res.headers.get('content-type') || ''
  if (ct.includes('application/json')) return (await res.json()) as T
  // @ts-ignore allow void
  return undefined
}

export const api = {
  grantPersonaAccess: (
    personaId: string,
    userId: string,
    accessToken?: string,
    isDefault: boolean = false,
    label: string | null = null
  ) => request<{ id: string }>(
    `/api/personas/${personaId}/access`,
    {
      method: 'POST',
      body: JSON.stringify({ UserId: userId, IsDefault: isDefault, Label: label })
    },
    accessToken
  ),
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

  listAgents: (accessToken?: string) =>
    request<AgentSummary[]>('/api/agents', {}, accessToken),

  // type comes from backend enum: 0=User, 1=Assistant, 2=Agent, 3=RolePlayCharacter (or string if enum serialized as string)
  listPersonas: (accessToken?: string) => request<Array<{ id: string; name: string; type?: number | 'User' | 'Assistant' | 'Agent' | 'RolePlayCharacter' }>>('/api/personas', {}, accessToken),
  getPersona: (id: string, accessToken?: string) => request<any>(`/api/personas/${id}`, {}, accessToken),
  createPersona: (
    payload: {
      Name: string
      Nickname?: string
      Role?: string
      Gender?: string
      Voice?: string
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

export const diagnosticsApi = {
  plannerHealth: (accessToken?: string) =>
    request<PlannerHealthReport>('/api/diagnostics/planner', {}, accessToken),
  openSearch: (accessToken?: string) =>
    request<OpenSearchDiagnosticsReport>('/api/diagnostics/opensearch', {}, accessToken)
}

export const fictionApi = {
  getPlanRoster: (planId: string, accessToken?: string) =>
    request<FictionPlanRoster>(`/api/fiction/plans/${planId}/roster`, {}, accessToken),
  fulfillLoreRequirement: (planId: string, requirementId: string, payload: FulfillLoreRequirementPayload, accessToken?: string) =>
    request<FictionPlanRoster['loreRequirements'][number]>(
      `/api/fiction/plans/${planId}/lore/${requirementId}/fulfill`,
      { method: 'POST', body: JSON.stringify(payload ?? {}) },
      accessToken
    ),
  getLoreSummary: (planId: string, accessToken?: string) =>
    request<LoreBranchSummary[]>(`/api/fiction/plans/${planId}/lore/summary`, {}, accessToken),
  getAuthorPersonaContext: (planId: string, accessToken?: string) =>
    request<AuthorPersonaContext>(`/api/fiction/plans/${planId}/author-persona`, {}, accessToken),
  getPlanBacklog: (planId: string, accessToken?: string) =>
    request<FictionBacklogItem[]>(`/api/fiction/plans/${planId}/backlog`, {}, accessToken),
  getBacklogActions: (planId: string, accessToken?: string) =>
    request<BacklogActionLog[]>(`/api/fiction/plans/${planId}/backlog/actions`, {}, accessToken),
  getPersonaObligations: (planId: string, accessToken?: string, page = 1, pageSize = 50) =>
    request<PersonaObligationListResponse>(
      `/api/fiction/plans/${planId}/persona-obligations?page=${page}&pageSize=${pageSize}`,
      {},
      accessToken
    ),
  resolvePersonaObligation: (planId: string, obligationId: string, payload: ResolvePersonaObligationPayload, accessToken?: string) =>
    request<PersonaObligation>(
      `/api/fiction/plans/${planId}/persona-obligations/${obligationId}/resolve`,
      { method: 'POST', body: JSON.stringify(payload ?? {}) },
      accessToken
    ),
  getLoreHistory: (planId: string, accessToken?: string) =>
    request<LoreFulfillmentLog[]>(`/api/fiction/plans/${planId}/lore/history`, {}, accessToken),
  resumeBacklog: (planId: string, backlogId: string, payload: ResumeBacklogPayload, accessToken?: string) =>
    request<FictionBacklogItem>(
      `/api/fiction/plans/${planId}/backlog/${encodeURIComponent(backlogId)}/resume`,
      { method: 'POST', body: JSON.stringify(payload) },
      accessToken
    ),
  listPlans: (accessToken?: string) =>
    request<FictionPlanSummary[]>('/api/fiction/plans', {}, accessToken),
  listProjects: (accessToken?: string) =>
    request<FictionProjectSummary[]>('/api/fiction/projects', {}, accessToken),
  createProject: (payload: { Title: string; Logline?: string | null }, accessToken?: string) =>
    request<FictionProjectSummary>(
      '/api/fiction/projects',
      { method: 'POST', body: JSON.stringify(payload) },
      accessToken
    ),
  createPlan: (payload: CreateFictionPlanPayload, accessToken?: string) =>
    request<FictionPlanSummary>(
      '/api/fiction/plans',
      { method: 'POST', body: JSON.stringify(payload) },
      accessToken
    )
}

export async function fetchImageMessages(accessToken: string, conversationId: string): Promise<any[]> {
  const headers: Record<string, string> = accessToken ? { Authorization: `Bearer ${accessToken}` } : {};
  const res = await fetch(`/api/images/by-conversation/${conversationId}`, { headers });
  if (res.ok) return await res.json();
  return [];
}
