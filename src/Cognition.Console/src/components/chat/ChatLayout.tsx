import React from 'react';
import { Box, Stack, Card, CardContent, Alert, CircularProgress, Typography } from '@mui/material';
import { PlanTimeline } from './PlanTimeline';
import { ToolTrace } from './ToolTrace';
import { MessageList } from './MessageList';
import { MessageInput } from './MessageInput';
import ImageViewer from '../ImageViewer';
import { ChatMenu } from './ChatMenu';

export type ChatLayoutProps = {
  personas: any[];
  personaId: string;
  onPersonaChange: (id: string) => void;
  providers: any[];
  models: any[];
  providerId: string;
  modelId: string;
  onProviderChange: (id: string) => void;
  onModelChange: (id: string) => void;
  messages: any[];
  onSend: (text: string) => void;
  busy?: boolean;
  error?: string;
  loading?: boolean;
  planSteps: string[];
  toolActions: string[];
  conversations: { id: string; title?: string | null }[];
  conversationId: string | null;
  onConversationChange: (id: string | null) => void;
  imgStyles: { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string }[];
  imgStyleId: string;
  onImgStyleChange: (id: string) => void;
  imgPending?: boolean;
  onGenerateImage?: (model: string, count: number) => void;
  onNewConversation?: () => void;
  connectionState?: 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
  assistantVoiceName?: string;
  assistantGender?: string;
  onRegenerate?: (index: number) => void;
  onPrevVersion?: (index: number) => void;
  onNextVersion?: (index: number) => void;
};

export function ChatLayout({ personas, personaId, onPersonaChange, providers, models, providerId, modelId, onProviderChange, onModelChange, messages, onSend, busy, error, loading, planSteps, toolActions, conversations, conversationId, onConversationChange, imgStyles, imgStyleId, onImgStyleChange, imgPending, onGenerateImage, onNewConversation, connectionState, assistantVoiceName, assistantGender, onRegenerate, onPrevVersion, onNextVersion }: ChatLayoutProps) {
  // Placeholder state for image menu, image model, image count, viewer, and input only.
  const [imgModel, setImgModel] = React.useState('dall-e-3');
  const [imgMsgCount, setImgMsgCount] = React.useState(6);
  const [viewer, setViewer] = React.useState<{ open: boolean, id?: string, title?: string, prompt?: string }>({ open: false });
  const [input, setInput] = React.useState('');

  // Image click handler for MessageList
  const handleImageClick = (id: string, prompt?: string) => setViewer({ open: true, id, prompt });

  // Restore single column layout with Card, correct height/overflow
  return (
    <Box sx={{ width: '100%', p: 0, m: 0 }}>
      <Box sx={{ maxWidth: 900, mx: 'auto', mt: 2 }}>
        <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 210px)' }}>
          <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
            {/* Menu bar (gear/settings, etc.) */}
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1, position: 'sticky', top: 0, zIndex: 1, bgcolor: 'background.paper' }}>
              <ChatMenu
                personas={personas}
                personaId={personaId}
                onPersonaChange={onPersonaChange}
                providers={providers}
                models={models}
                providerId={providerId}
                modelId={modelId}
                onProviderChange={onProviderChange}
                onModelChange={onModelChange}
                imgStyles={imgStyles}
                imgStyleId={imgStyleId}
                onImgStyleChange={onImgStyleChange}
                imgModel={imgModel}
                onImgModelChange={setImgModel}
                imgMsgCount={imgMsgCount}
              onImgMsgCountChange={setImgMsgCount}
                imgPending={!!imgPending}
                onGenerateImage={() => { onGenerateImage && onGenerateImage(imgModel, imgMsgCount); }}
                conversations={conversations}
                conversationId={conversationId ?? ''}
                onConversationChange={onConversationChange}
                onNewConversation={() => { onNewConversation && onNewConversation(); }}
              />
              {/* Conversation title next to gear/settings */}
              <Box sx={{ ml: 2 }}>
                <span className={"conversation-title"}>
                  {(() => {
                    const t = conversations.find(c => c.id === (conversationId ?? ''))?.title;
                    if (t && t.trim().length > 0) return t;
                    return 'New Chat';
                  })()}
                </span>
              </Box>
              {/* Connection status sticky right */}
              <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                {connectionState && (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
                    <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: (
                      connectionState === 'connected' ? 'success.main' :
                      connectionState === 'connecting' ? 'warning.main' :
                      connectionState === 'reconnecting' ? 'warning.main' : 'error.main'
                    ) }} />
                    <Typography variant="caption" color="text.secondary">
                      {connectionState === 'connected' ? 'Connected' : (connectionState === 'connecting' ? 'Connecting…' : (connectionState === 'reconnecting' ? 'Reconnecting…' : 'Offline'))}
                    </Typography>
                  </Box>
                )}
              </Box>
            </Box>
            {/* PlanTimeline and ToolTrace connected to hub state */}
            {(planSteps?.length > 0 || toolActions?.length > 0) && (
              <Box sx={{ mb: 2 }}>
                {planSteps?.length > 0 && <PlanTimeline steps={planSteps} />}
                {toolActions?.length > 0 && <ToolTrace actions={toolActions} />}
              </Box>
            )}
            {/* Chat area */}
            <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', overflowX: 'hidden', pr: 1 }}>
              <MessageList messages={messages} onImageClick={handleImageClick} ttsVoiceName={assistantVoiceName} assistantGender={assistantGender} onRegenerate={onRegenerate} onPrevVersion={onPrevVersion} onNextVersion={onNextVersion} />
            </Box>
            {/* Input bar */}
            <MessageInput
              value={input}
              onChange={setInput}
              onSend={() => { onSend(input); setInput(''); }}
              busy={busy}
              onSTT={(t) => setInput(prev => prev + (prev ? ' ' : '') + t)}
            />
            {/* Error/loading states */}
            {error && (
              <Alert severity="error">{error}</Alert>
            )}
            {loading && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CircularProgress size={18} />
                <Typography variant="caption" color="text.secondary">Loading…</Typography>
              </Box>
            )}
            {/* Context caption */}
            <Typography variant="caption" color="text.secondary">
              {`Chatting as ${personas.find((p: any) => p.id === personaId)?.name || 'Assistant'} via ${providers.find((p: any) => p.id === providerId)?.displayName || providers.find((p: any) => p.id === providerId)?.name || 'Provider'}${modelId ? ` · ${models.find((m: any) => m.id === modelId)?.displayName || models.find((m: any) => m.id === modelId)?.name || modelId}` : ''}${conversationId ? '' : ' · New conversation on first send'}`}
            </Typography>
          </CardContent>
        </Card>
      </Box>
      <ImageViewer open={viewer.open} onClose={() => setViewer({ open: false })} imageId={viewer.id} title={viewer.title} prompt={viewer.prompt} />
    </Box>
  );
}





