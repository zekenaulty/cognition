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
  telemetry: PlannerHealthTelemetry;
  alerts: PlannerHealthAlert[];
  warnings: string[];
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
