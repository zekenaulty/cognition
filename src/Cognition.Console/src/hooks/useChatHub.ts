import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import {
  AssistantMessageAppended,
  PlanReady,
  ToolExecutionRequested,
  ToolExecutionCompleted,
  ChatBusEvent,
} from '../types/events';

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

      connection.on('AssistantMessageAppended', (msg: AssistantMessageAppended) => {
        if (onAssistantMessage) onAssistantMessage(msg);
      });
      connection.on('PlanReady', (evt: PlanReady) => {
        if (onPlanReady) onPlanReady(evt);
      });
      connection.on('ToolExecutionRequested', (evt: ToolExecutionRequested) => {
        if (onToolExecutionRequested) onToolExecutionRequested(evt);
      });
      connection.on('ToolExecutionCompleted', (evt: ToolExecutionCompleted) => {
        if (onToolExecutionCompleted) onToolExecutionCompleted(evt);
      });

      connection.start().then(() => {
        if (!isMounted) return;
        connection.invoke('JoinConversation', conversationId);
      });

      return () => {
        isMounted = false;
        connection.stop();
        connectionRef.current = null;
      };
    }, [conversationId, accessToken, onAssistantMessage, onPlanReady, onToolExecutionRequested, onToolExecutionCompleted]);

    return {
      sendUserMessage,
      requestPlan,
      ackToolStep,
      connectionRef,
    };
  }
