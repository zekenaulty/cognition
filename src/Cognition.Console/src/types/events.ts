// Event DTOs for chat bus events

export interface AssistantMessageAppended {
  conversationId: string;
  personaId: string;
  content: string;
  timestamp: string;
  messageId?: string;
}

export interface PlanReady {
  conversationId: string;
  personaId: string;
  plan: any;
  timestamp: string;
}

export interface ToolExecutionRequested {
  conversationId: string;
  personaId: string;
  toolId: string;
  args: Record<string, any>;
  timestamp: string;
}

export interface ToolExecutionCompleted {
  conversationId: string;
  personaId: string;
  toolId: string;
  result: any;
  success: boolean;
  error?: string;
  timestamp: string;
}

// Streaming token delta during assistant generation
export interface AssistantTokenDelta {
  conversationId: string;
  personaId: string;
  // server may send delta/content/token; accept any
  delta?: string;
  content?: string;
  token?: string;
  timestamp?: string;
}

export interface ConversationCreated {
  conversationId: string;
  personaId: string;
  title?: string | null;
  timestamp?: string;
}

export interface ConversationJoined {
  conversationId: string;
  personaId: string;
  timestamp?: string;
}

export interface ConversationLeft {
  conversationId: string;
  personaId: string;
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
