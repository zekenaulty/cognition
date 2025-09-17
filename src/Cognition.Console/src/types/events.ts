// Event DTOs for chat bus events

export interface AssistantMessageAppended {
  conversationId: string;
  personaId: string;
  content: string;
  timestamp: string;
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

export type ChatBusEvent =
  | AssistantMessageAppended
  | PlanReady
  | ToolExecutionRequested
  | ToolExecutionCompleted;
