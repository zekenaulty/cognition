import React from 'react';
import { ChatLayout } from './ChatLayout';

type LayoutProps = React.ComponentProps<typeof ChatLayout>;

type ChatPageViewProps = {
  layout: LayoutProps;
};

// Thin wrapper to isolate the ChatLayout prop shape from the container logic.
export function ChatPageView({ layout }: ChatPageViewProps) {
  return <ChatLayout {...layout} />;
}
