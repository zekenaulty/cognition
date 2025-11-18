import { BacklogActionLog, PersonaObligation } from '../../types/fiction';

export function normalizeObligationStatus(status?: string | null) {
  return (status ?? '').toLowerCase();
}

export function isObligationOpen(status?: string | null) {
  const normalized = normalizeObligationStatus(status);
  return normalized !== 'resolved' && normalized !== 'dismissed';
}

export function formatObligationStatus(status?: string | null) {
  const normalized = normalizeObligationStatus(status);
  if (normalized === 'resolved') return 'Resolved';
  if (normalized === 'dismissed') return 'Dismissed';
  if (normalized === 'open' || normalized === 'pending') return 'Open';
  if (!status) return 'Unknown';
  return status.charAt(0).toUpperCase() + status.slice(1);
}

export type ResolutionNoteSummary = {
  id: string;
  note: string;
  actor?: string;
  timestamp?: string;
};

export type ObligationMetadataSummary = {
  resolutionNotes: ResolutionNoteSummary[];
  otherEntries: string[];
};

export function summarizeObligationMetadata(metadata: PersonaObligation['metadata']): ObligationMetadataSummary {
  const summary: ObligationMetadataSummary = { resolutionNotes: [], otherEntries: [] };
  const record = coerceMetadataRecord(metadata);
  if (!record) {
    return summary;
  }

  const rawNotes = record.resolutionNotes;
  if (Array.isArray(rawNotes)) {
    rawNotes.forEach((entry, index) => {
      if (!entry || typeof entry !== 'object') {
        return;
      }
      const noteText = typeof (entry as any).note === 'string' ? (entry as any).note.trim() : '';
      if (!noteText) {
        return;
      }
      const actor = typeof (entry as any).actor === 'string' ? (entry as any).actor : undefined;
      const timestamp = typeof (entry as any).timestamp === 'string' ? (entry as any).timestamp : undefined;
      summary.resolutionNotes.push({
        id: `${index}-${noteText}`,
        note: noteText,
        actor,
        timestamp
      });
    });
  }

  Object.entries(record).forEach(([key, value]) => {
    if (key === 'resolutionNotes' || key === 'branchLineage') {
      return;
    }
    const formatted = formatMetadataEntry(key, value);
    if (formatted) {
      summary.otherEntries.push(formatted);
    }
  });

  return summary;
}

export function formatResolutionNoteText(
  note: ResolutionNoteSummary,
  formatRelative?: (value?: string | null) => string
) {
  const parts: string[] = [];
  if (note.actor) {
    parts.push(`${note.actor.trim()}:`);
  }
  parts.push(note.note);
  if (note.timestamp) {
    const formatted = formatRelative ? formatRelative(note.timestamp) : note.timestamp;
    parts.push(`(${formatted})`);
  }
  return parts.join(' ').trim();
}

export function buildActionMetadataLine(log: BacklogActionLog) {
  const segments: string[] = [];
  if (log.providerId) {
    segments.push(`Provider ${formatIdFragment(log.providerId)}`);
  }
  if (log.modelId) {
    segments.push(`Model ${formatIdFragment(log.modelId)}`);
  }
  if (log.agentId) {
    segments.push(`Agent ${formatIdFragment(log.agentId)}`);
  }
  return segments.length > 0 ? segments.join(' • ') : null;
}

export function buildActionContextLine(log: BacklogActionLog) {
  const segments: string[] = [];
  if (log.conversationPlanId) {
    segments.push(`Plan ${formatIdFragment(log.conversationPlanId)}`);
  }
  if (log.conversationId) {
    segments.push(`Conversation ${formatIdFragment(log.conversationId)}`);
  }
  if (log.taskId) {
    segments.push(`Task ${formatIdFragment(log.taskId)}`);
  }
  return segments.length > 0 ? segments.join(' • ') : null;
}

function coerceMetadataRecord(metadata: PersonaObligation['metadata']): Record<string, any> | null {
  if (!metadata) {
    return null;
  }
  if (typeof metadata === 'string') {
    try {
      const parsed = JSON.parse(metadata);
      return typeof parsed === 'object' && parsed !== null ? (parsed as Record<string, any>) : null;
    } catch {
      return null;
    }
  }
  if (typeof metadata === 'object' && !Array.isArray(metadata)) {
    return metadata as Record<string, any>;
  }
  return null;
}

function formatMetadataEntry(key: string, value: unknown) {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === 'string') {
    const trimmed = value.trim();
    return trimmed ? `${key}: ${trimmed}` : null;
  }
  if (typeof value === 'number' || typeof value === 'boolean') {
    return `${key}: ${value}`;
  }
  if (Array.isArray(value) && value.length > 0) {
    return `${key}: ${value.join(', ')}`;
  }
  return null;
}

function formatIdFragment(value?: string | null) {
  if (!value) {
    return '';
  }
  const trimmed = value.replace(/-/g, '');
  return trimmed.length <= 8 ? trimmed : trimmed.slice(0, 8);
}
