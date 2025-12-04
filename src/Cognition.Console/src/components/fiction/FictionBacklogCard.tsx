import React from 'react';
import { Card, CardHeader, CardContent } from '@mui/material';
import { FictionBacklogPanel } from './FictionBacklogPanel';
import {
  AuthorPersonaContext,
  BacklogActionLog,
  FictionBacklogItem,
  PersonaObligation,
} from '../../types/fiction';

type Props = {
  rosterPresent: boolean;
  backlogItems: FictionBacklogItem[];
  backlogLoading: boolean;
  backlogError: string | null;
  placeholder: string;
  isAdmin: boolean;
  resumingId: string | null;
  actionLogs: BacklogActionLog[];
  actionLoading: boolean;
  actionError: string | null;
  obligations: PersonaObligation[];
  obligationsLoading: boolean;
  obligationsError: string | null;
  obligationActionId: string | null;
  obligationActionError: string | null;
  personaContext: AuthorPersonaContext | null;
  onResume?: (item: FictionBacklogItem) => void;
  onResolveObligation?: (obligation: PersonaObligation, action: 'resolve' | 'dismiss') => void;
};

export function FictionBacklogCard({
  rosterPresent,
  backlogItems,
  backlogLoading,
  backlogError,
  placeholder,
  isAdmin,
  resumingId,
  actionLogs,
  actionLoading,
  actionError,
  obligations,
  obligationsLoading,
  obligationsError,
  obligationActionId,
  obligationActionError,
  personaContext,
  onResume,
  onResolveObligation,
}: Props) {
  return (
    <Card>
      <CardHeader
        title="Backlog"
        subheader={
          rosterPresent
            ? `${backlogItems.length} tracked items`
            : 'View pending planner work and resume blocked steps.'
        }
      />
      <CardContent>
        <FictionBacklogPanel
          items={backlogItems}
          loading={backlogLoading}
          error={backlogError}
          placeholder={placeholder}
          onResume={isAdmin ? onResume : undefined}
          resumingId={resumingId}
          isAdmin={isAdmin}
          actionLogs={actionLogs}
          actionLoading={actionLoading}
          actionError={actionError}
          obligations={obligations}
          obligationsLoading={obligationsLoading}
          obligationsError={obligationsError}
          onResolveObligation={isAdmin ? onResolveObligation : undefined}
          obligationActionId={obligationActionId}
          obligationActionError={obligationActionError}
          personaContext={personaContext}
        />
      </CardContent>
    </Card>
  );
}
