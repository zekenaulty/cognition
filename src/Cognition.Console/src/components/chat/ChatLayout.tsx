import React from 'react';
import { Box, Stack, Card, CardContent } from '@mui/material';
import { PersonaPicker } from './PersonaPicker';
import { ProviderModelPicker } from './ProviderModelPicker';
import { PlanTimeline } from './PlanTimeline';
import { ToolTrace } from './ToolTrace';
import { MessageList } from './MessageList';
import { MessageComposer } from './MessageComposer';
import { FeedbackBar } from './FeedbackBar';
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
};

export function ChatLayout({ personas, personaId, onPersonaChange, providers, models, providerId, modelId, onProviderChange, onModelChange, messages, onSend, busy, planSteps, toolActions, conversations, conversationId, onConversationChange, imgStyles, imgStyleId, onImgStyleChange }: ChatLayoutProps) {
  // Placeholder state for image menu, image model, image count, image pending, viewer, and input only.
  const [imgModel, setImgModel] = React.useState('dall-e-3');
  const [imgMsgCount, setImgMsgCount] = React.useState(6);
  const [imgPending, setImgPending] = React.useState(false);
  const [viewer, setViewer] = React.useState<{ open: boolean, id?: string, title?: string }>({ open: false });
  const [input, setInput] = React.useState('');

  // Image click handler for MessageList
  const handleImageClick = (id: string, title?: string) => setViewer({ open: true, id, title });

  // Restore single column layout with Card, correct height/overflow
  return (
    <Box sx={{ width: '100%', p: 0, m: 0 }}>
      <Box sx={{ maxWidth: 900, mx: 'auto', mt: 2 }}>
        <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 210px)' }}>
          <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
            {/* Menu bar (gear/settings, etc.) */}
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
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
                imgPending={imgPending}
                onGenerateImage={() => {}}
                conversations={conversations}
                conversationId={conversationId ?? ''}
                onConversationChange={onConversationChange}
                onNewConversation={() => {}}
              />
              {/* Conversation title next to gear/settings */}
              <Box sx={{ ml: 2 }}>
                <span className={"conversation-title"}>
                  {conversations.find(c => c.id === (conversationId ?? ''))?.title || (conversationId ? conversationId.slice(0,8) + '...' : 'New Conversation')}
                </span>
              </Box>
            </Box>
            {/* PlanTimeline and ToolTrace connected to hub state */}
            <Box sx={{ mb: 2 }}>
              <PlanTimeline steps={planSteps} />
              <ToolTrace actions={toolActions} />
            </Box>
            {/* Chat area */}
            <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', pr: 1 }}>
              <MessageList messages={messages} onImageClick={handleImageClick} />
            </Box>
            {/* Input bar */}
            <MessageInput
              value={input}
              onChange={setInput}
              onSend={() => { onSend(input); setInput(''); }}
              busy={busy}
            />
            {/* Error/loading states (to be restored) */}
            {/* TODO: Render error/loading states here */}
          </CardContent>
        </Card>
      </Box>
      <ImageViewer open={viewer.open} onClose={() => setViewer({ open: false })} imageId={viewer.id} title={viewer.title} />
    </Box>
  );
}
