// Event DTOs for chat bus events

export interface AssistantMessageAppended {
  conversationId: string;
  agentId?: string;
  personaId?: string;
  content: string;
  timestamp: string;
  messageId?: string;
}

export interface PlanReady {
  conversationId: string;
  agentId: string;
  personaId: string;
  providerId: string;
  modelId?: string | null;
  plan: any;
  conversationPlanId: string;
  fictionPlanId: string;
  branchSlug: string;
  metadata?: Record<string, any> | null;
  timestamp: string;
}

export interface ToolExecutionRequested {
  conversationId: string;
  agentId: string;
  personaId: string;
  tool: string;
  args: Record<string, any>;
  conversationPlanId?: string | null;
  stepNumber?: number;
  fictionPlanId?: string | null;
  branchSlug?: string;
  metadata?: Record<string, any> | null;
  timestamp: string;
}

export interface ToolExecutionCompleted {
  conversationId: string;
  agentId: string;
  personaId: string;
  tool: string;
  result: any;
  success: boolean;
  error?: string;
  conversationPlanId?: string | null;
  stepNumber?: number;
  fictionPlanId?: string | null;
  branchSlug?: string;
  metadata?: Record<string, any> | null;
  timestamp: string;
}

// Streaming token delta during assistant generation
export interface AssistantTokenDelta {
  conversationId: string;
  agentId?: string;
  personaId?: string;
  // server may send delta/content/token; accept any
  delta?: string;
  content?: string;
  token?: string;
  timestamp?: string;
}

export interface ConversationCreated {
  conversationId: string;
  agentId?: string;
  personaId?: string;
  title?: string | null;
  timestamp?: string;
}

export interface ConversationJoined {
  conversationId: string;
  agentId?: string;
  personaId?: string;
  timestamp?: string;
}

export interface ConversationLeft {
  conversationId: string;
  agentId?: string;
  personaId?: string;
  timestamp?: string;
}

export interface ConversationUpdated {
  conversationId: string;
  title?: string | null;
  timestamp?: string;
}

export type ChatBusEvent =
  | AssistantMessageAppended
  | PlanReady
  | ToolExecutionRequested
  | ToolExecutionCompleted
  | AssistantTokenDelta
  | ConversationCreated
  | ConversationJoined
  | ConversationLeft
  | ConversationUpdated;
