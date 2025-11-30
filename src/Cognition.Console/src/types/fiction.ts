export type FictionPlanRoster = {
  planId: string;
  planName: string;
  projectTitle?: string | null;
  branchSlug?: string | null;
  characters: FictionCharacterRosterItem[];
  loreRequirements: FictionLoreRequirementItem[];
};

export type FictionPlanSummary = {
  id: string;
  name: string;
  projectTitle?: string | null;
  status: string;
};

export type FictionProjectSummary = {
  id: string;
  title: string;
  logline?: string | null;
  status: string;
  planCount: number;
  activePlanCount: number;
};

export type LoreFulfillmentLog = {
  requirementId: string;
  requirementSlug: string;
  action: string;
  branch: string;
  branchLineage?: string[] | null;
  actor?: string | null;
  actorId?: string | null;
  source: string;
  worldBibleEntryId?: string | null;
  notes?: string | null;
  status?: string | null;
  conversationId?: string | null;
  planPassId?: string | null;
  timestampUtc: string;
  requestedAtUtc?: string | null;
  completedAtUtc?: string | null;
  automationConversationId?: string | null;
  automationAgentId?: string | null;
  slaMinutes?: number | null;
};

export type BacklogActionLog = {
  action: string;
  backlogId: string;
  description?: string | null;
  branch: string;
  actor?: string | null;
  actorId?: string | null;
  source: string;
  providerId?: string | null;
  modelId?: string | null;
  agentId?: string | null;
  status?: string | null;
  conversationId?: string | null;
  conversationPlanId?: string | null;
  taskId?: string | null;
  timestampUtc: string;
};

export type FictionBacklogItem = {
  id: string;
  backlogId: string;
  description: string;
  status: string;
  inputs?: string[] | null;
  outputs?: string[] | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  conversationPlanId?: string | null;
  conversationId?: string | null;
  agentId?: string | null;
  providerId?: string | null;
  modelId?: string | null;
  branchSlug?: string | null;
  taskId?: string | null;
  stepNumber?: number | null;
  toolName?: string | null;
  thought?: string | null;
  taskStatus?: string | null;
};

export type FictionCharacterRosterItem = {
  id: string;
  slug: string;
  displayName: string;
  role: string;
  importance: string;
  summary?: string | null;
  notes?: string | null;
  personaId?: string | null;
  persona?: FictionPersonaSummary | null;
  agentId?: string | null;
  agent?: FictionAgentSummary | null;
  worldBibleEntryId?: string | null;
  worldBible?: FictionWorldBibleSummary | null;
  firstSceneId?: string | null;
  createdByPlanPassId?: string | null;
  branchSlug?: string | null;
  branchLineage?: string[] | null;
  provenance?: any;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type FictionPersonaSummary = {
  id: string;
  name: string;
  role?: string | null;
  voice?: string | null;
  essence?: string | null;
  background?: string | null;
  communicationStyle?: string | null;
};

export type FictionAgentSummary = {
  id: string;
  personaId: string;
  rolePlay: boolean;
};

export type FictionWorldBibleSummary = {
  entryId: string;
  worldBibleId: string;
  domain: string;
  entrySlug: string;
  entryName: string;
  category: string;
  summary: string;
  status: string;
  continuityNotes: string[];
  updatedAtUtc: string;
  agentId?: string | null;
  personaId?: string | null;
  sourcePlanPassId?: string | null;
  sourceConversationId?: string | null;
  sourceBacklogId?: string | null;
  branchSlug?: string | null;
};

export type FictionLoreRequirementItem = {
  id: string;
  title: string;
  requirementSlug: string;
  status: string;
  description?: string | null;
  notes?: string | null;
  worldBibleEntryId?: string | null;
  worldBible?: FictionWorldBibleSummary | null;
  metadata?: any;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  createdByPlanPassId?: string | null;
  chapterScrollId?: string | null;
  chapterSceneId?: string | null;
  branchSlug?: string | null;
  branchLineage?: string[] | null;
};

export type LoreBranchSummary = {
  branchSlug: string;
  branchLineage?: string[] | null;
  ready: number;
  blocked: number;
  planned: number;
};

export type AuthorPersonaContext = {
  personaId: string;
  personaName: string;
  summary: string;
  memories: string[];
  worldNotes: string[];
};

export type FulfillLoreRequirementPayload = {
  worldBibleEntryId?: string | null;
  notes?: string | null;
  conversationId?: string | null;
  planPassId?: string | null;
  branchSlug?: string | null;
  branchLineage?: string[] | null;
  source?: string | null;
};

export type ResumeBacklogPayload = {
  conversationId: string;
  conversationPlanId: string;
  agentId: string;
  providerId: string;
  modelId?: string | null;
  taskId: string;
  branchSlug?: string | null;
};

export type PersonaObligation = {
  id: string;
  title: string;
  description?: string | null;
  status: string;
  sourcePhase?: string | null;
  branchSlug?: string | null;
  branchLineage?: string[] | null;
  personaId: string;
  personaName: string;
  fictionCharacterId?: string | null;
  characterSlug?: string | null;
  sourceBacklogId?: string | null;
  metadata?: any;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
};

export type PersonaObligationListResponse = {
  items: PersonaObligation[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ResolvePersonaObligationPayload = {
  notes?: string | null;
  source?: string | null;
  action?: 'resolve' | 'dismiss' | string | null;
  backlogId?: string | null;
  taskId?: string | null;
  conversationId?: string | null;
  voiceDrift?: boolean | null;
};

export type CreateFictionPlanPayload = {
  projectId?: string | null;
  projectTitle?: string | null;
  projectLogline?: string | null;
  name: string;
  description?: string | null;
  branchSlug?: string | null;
  personaId: string;
  agentId?: string | null;
};

export type AgentSummary = {
  id: string;
  personaId: string;
  clientProfileId?: string | null;
  rolePlay: boolean;
  prefix?: string | null;
  suffix?: string | null;
};
