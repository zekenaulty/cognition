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
