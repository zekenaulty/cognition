import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import {
  AssistantMessageAppended,
  PlanReady,
  ToolExecutionRequested,
  ToolExecutionCompleted,
  ChatBusEvent,
  AssistantTokenDelta,
} from '../types/events';
import { chatBus } from '../bus/chatBus';

export interface UseChatHubOptions {
  conversationId: string;
  accessToken?: string;
  onAssistantMessage?: (msg: AssistantMessageAppended) => void;
  onPlanReady?: (evt: PlanReady) => void;
  onToolExecutionRequested?: (evt: ToolExecutionRequested) => void;
  onToolExecutionCompleted?: (evt: ToolExecutionCompleted) => void;
}

export function useChatHub(options: UseChatHubOptions) {
    const {
      conversationId,
      accessToken,
      onAssistantMessage,
      onPlanReady,
      onToolExecutionRequested,
      onToolExecutionCompleted,
    } = options;

    const connectionRef = useRef<signalR.HubConnection | null>(null);

    // --- Client→Server Hub Methods ---
    // AppendUserMessage(text: string, personaId?: string, providerId?: string, modelId?: string)
    // RequestPlan()
    // AckToolStep(stepId: string, status: string)

    // Send a user message to the server
    const sendUserMessage = async (
      text: string,
      personaId?: string,
      providerId?: string,
      modelId?: string
    ): Promise<boolean> => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== signalR.HubConnectionState.Connected) return false;
      try {
        //await conn.invoke('AppendUserMessage', text, personaId, providerId, modelId);
        await conn.invoke('AppendUserMessage', conversationId, text, personaId, providerId, modelId);
        return true;
      } catch (err) {
        console.error('Hub sendUserMessage error:', err);
        return false;
      }
    };

    // Send a user message to an explicit conversation id
    const sendUserMessageTo = async (
      convId: string,
      text: string,
      personaId?: string,
      providerId?: string,
      modelId?: string
    ): Promise<boolean> => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== signalR.HubConnectionState.Connected) return false;
      try {
        await conn.invoke('AppendUserMessage', convId, text, personaId, providerId, modelId);
        return true;
      } catch (err) {
        console.error('Hub sendUserMessageTo error:', err);
        return false;
      }
    };

    // Request a plan from the server
    const requestPlan = async (): Promise<boolean> => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== signalR.HubConnectionState.Connected) return false;
      try {
        await conn.invoke('RequestPlan');
        return true;
      } catch (err) {
        console.error('Hub requestPlan error:', err);
        return false;
      }
    };

    // Acknowledge a tool step
    const ackToolStep = async (stepId: string, status: string): Promise<boolean> => {
      const conn = connectionRef.current;
      if (!conn || conn.state !== signalR.HubConnectionState.Connected) return false;
      try {
        await conn.invoke('AckToolStep', stepId, status);
        return true;
      } catch (err) {
        console.error('Hub ackToolStep error:', err);
        return false;
      }
    };

    useEffect(() => {
      if (!conversationId) return;
      let isMounted = true;
      const optionsObj = accessToken ? { accessTokenFactory: () => accessToken } : {};
      const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hub/chat', optionsObj)
        .withAutomaticReconnect()
        .build();
      connectionRef.current = connection;

      connection.on('AssistantMessageAppended', (msg: AssistantMessageAppended & { messageId?: string }) => {
        chatBus.emit('assistant-message', msg);
        if (onAssistantMessage) onAssistantMessage(msg);
      });
      connection.on('PlanReady', (evt: PlanReady) => {
        chatBus.emit('plan-ready', evt);
        if (onPlanReady) onPlanReady(evt);
      });
      connection.on('ToolExecutionRequested', (evt: ToolExecutionRequested) => {
        chatBus.emit('tool-requested', evt);
        if (onToolExecutionRequested) onToolExecutionRequested(evt);
      });
      connection.on('ToolExecutionCompleted', (evt: ToolExecutionCompleted) => {
        chatBus.emit('tool-completed', evt);
        if (onToolExecutionCompleted) onToolExecutionCompleted(evt);
      });
      connection.on('AssistantTokenDelta', (evt: AssistantTokenDelta) => {
        const text = (evt.delta ?? (evt as any).content ?? (evt as any).token ?? '') as string;
        try {
          chatBus.emit('assistant-delta', { conversationId, personaId: evt.personaId, text, timestamp: evt.timestamp });
        } catch {}
      });

      // Conversation lifecycle
      connection.on('ConversationCreated', (evt: any) => {
        try { chatBus.emit('conversation-created', evt); } catch {}
      });
      connection.on('ConversationJoined', (evt: any) => {
        try { chatBus.emit('conversation-joined', evt); } catch {}
      });
      connection.on('ConversationLeft', (evt: any) => {
        try { chatBus.emit('conversation-left', evt); } catch {}
      });

      // Assistant message version events
      connection.on('AssistantMessageVersionAppended', (evt: any) => {
        try { chatBus.emit('assistant-version-appended', evt); } catch {}
      });
      connection.on('AssistantActiveVersionChanged', (evt: any) => {
        try { chatBus.emit('assistant-version-activated', evt); } catch {}
      });

      chatBus.emit('connection-state', { state: 'connecting' });
      connection.start().then(() => {
        if (!isMounted) return;
        chatBus.emit('connection-state', { state: 'connected' });
        connection.invoke('JoinConversation', conversationId);
      }).catch(err => console.warn('Hub start error', err));

      connection.onreconnecting(() => {
        chatBus.emit('connection-state', { state: 'reconnecting' });
      });
      connection.onreconnected(() => {
        // Rejoin the conversation after reconnect
        try { connection.invoke('JoinConversation', conversationId); } catch (e) { console.warn('Rejoin failed', e); }
        chatBus.emit('connection-state', { state: 'connected' });
      });

      return () => {
        isMounted = false;
        connection.stop();
        connectionRef.current = null;
        chatBus.emit('connection-state', { state: 'disconnected' });
      };
    }, [conversationId, accessToken, onAssistantMessage, onPlanReady, onToolExecutionRequested, onToolExecutionCompleted]);

    return {
      sendUserMessage,
      sendUserMessageTo,
      requestPlan,
      ackToolStep,
      connectionRef,
    };
  }
