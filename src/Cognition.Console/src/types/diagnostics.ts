export type PlannerHealthStatus = 'Healthy' | 'Degraded' | 'Critical' | string;

export type PlannerHealthAlert = {
  id: string;
  severity: 'info' | 'warning' | 'error' | string;
  title: string;
  description: string;
  generatedAtUtc: string;
};

export type PlannerHealthReport = {
  generatedAtUtc: string;
  status: PlannerHealthStatus;
  planners: PlannerHealthPlanner[];
  backlog: PlannerHealthBacklog;
  worldBible: PlannerHealthWorldBibleReport;
  telemetry: PlannerHealthTelemetry;
  alerts: PlannerHealthAlert[];
  warnings: string[];
};

export type PlannerHealthWorldBibleReport = {
  plans: PlannerHealthWorldBiblePlan[];
};

export type PlannerHealthWorldBiblePlan = {
  planId: string;
  planName: string;
  worldBibleId: string;
  domain: string;
  branchSlug?: string | null;
  lastUpdatedUtc?: string | null;
  activeEntries: PlannerHealthWorldBibleEntry[];
};

export type PlannerHealthWorldBibleEntry = {
  category: string;
  entrySlug: string;
  entryName: string;
  summary: string;
  status: string;
  continuityNotes: string[];
  version: number;
  isActive: boolean;
  iterationIndex?: number | null;
  backlogItemId?: string | null;
  updatedAtUtc: string;
};

export type PlannerHealthPlanner = {
  name: string;
  description: string;
  capabilities: string[];
  steps: PlannerHealthStepTemplate[];
};

export type PlannerHealthStepTemplate = {
  stepId: string;
  displayName: string;
  templateId?: string | null;
  templateFound: boolean;
  templateActive: boolean;
  issue?: string | null;
};

export type PlannerHealthBacklog = {
  totalItems: number;
  pending: number;
  inProgress: number;
  complete: number;
  plans: PlannerHealthBacklogPlanSummary[];
  recentTransitions: PlannerHealthBacklogTransition[];
  staleItems: PlannerHealthBacklogItem[];
  orphanedItems: PlannerHealthBacklogItem[];
  telemetryEvents: PlannerBacklogTelemetry[];
  actionLogs: PlannerBacklogActionLog[];
};

export type PlannerHealthBacklogPlanSummary = {
  planId: string;
  planName: string;
  pending: number;
  inProgress: number;
  complete: number;
  lastUpdatedUtc?: string | null;
  lastCompletedUtc?: string | null;
};

export type PlannerHealthBacklogItem = {
  planId: string;
  planName: string;
  backlogId: string;
  description: string;
  status: string | number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  staleDuration?: string | number | null;
};

export type PlannerHealthBacklogTransition = {
  planId: string;
  planName: string;
  backlogId: string;
  description: string;
  status: string | number;
  occurredAtUtc: string;
  age: string | number;
};

export type PlannerBacklogTelemetry = {
  planId: string;
  planName: string;
  backlogId: string;
  phase: string;
  status: string;
  previousStatus?: string | null;
  reason: string;
  branch: string;
  iteration?: number | null;
  timestampUtc: string;
  metadata?: Record<string, string | null> | null;
  characterMetrics?: PlannerBacklogTelemetryCharacterMetrics | null;
  loreMetrics?: PlannerBacklogTelemetryLoreMetrics | null;
  recentCharacters: PlannerBacklogTelemetryCharacter[];
  pendingLore: PlannerBacklogTelemetryLore[];
};

export type PlannerBacklogTelemetryCharacterMetrics = {
  total: number;
  personaLinked: number;
  worldBibleLinked: number;
};

export type PlannerBacklogTelemetryLoreMetrics = {
  total: number;
  ready: number;
  blocked: number;
};

export type PlannerBacklogTelemetryCharacter = {
  id: string;
  slug: string;
  displayName: string;
  personaId?: string | null;
  worldBibleEntryId?: string | null;
  role?: string | null;
  importance?: string | null;
  updatedAtUtc: string;
};

export type PlannerBacklogTelemetryLore = {
  id: string;
  requirementSlug: string;
  title: string;
  status: string;
  worldBibleEntryId?: string | null;
  updatedAtUtc: string;
};

export type PlannerBacklogActionLog = {
  planId: string;
  planName: string;
  backlogId: string;
  description?: string | null;
  action: string;
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

export type PlannerHealthTelemetry = {
  totalExecutions: number;
  lastExecutionUtc?: string | null;
  outcomeCounts: Record<string, number>;
  critiqueStatusCounts: Record<string, number>;
  recentFailures: PlannerHealthExecutionFailure[];
};

export type PlannerHealthExecutionFailure = {
  executionId: string;
  plannerName: string;
  outcome: string;
  createdAtUtc: string;
  diagnostics?: Record<string, string> | null;
  conversationId?: string | null;
  conversationMessageId?: string | null;
  transcriptRole?: string | null;
  transcriptSnippet?: string | null;
};

export type OpenSearchDiagnosticsReport = {
  checkedAtUtc: string;
  endpoint: string;
  defaultIndex: string;
  pipelineId?: string | null;
  modelId?: string | null;
  clusterAvailable: boolean;
  clusterStatus?: string | null;
  indexExists: boolean;
  pipelineExists: boolean;
  modelState?: string | null;
  modelDeployState?: string | null;
  notes: string[];
};
