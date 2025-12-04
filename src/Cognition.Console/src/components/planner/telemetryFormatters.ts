export function formatTelemetryMetadata(metadata?: Record<string, string | null> | null) {
  if (!metadata) return '—';
  const parts: string[] = [];
  if (metadata.characters) parts.push(`Chars: ${metadata.characters}`);
  if (metadata.lore) parts.push(`Lore: ${metadata.lore}`);
  if (metadata.branch) parts.push(`Branch: ${metadata.branch}`);
  if (metadata.phase) parts.push(`Phase: ${metadata.phase}`);
  return parts.length ? parts.join(' · ') : '—';
}

export function formatTelemetryContext(metadata?: Record<string, string | null> | null) {
  if (!metadata) return '';
  const parts: string[] = [];
  if (metadata.backlogDescription) parts.push(metadata.backlogDescription);
  if (metadata.action) parts.push(`Action: ${metadata.action}`);
  return parts.join(' · ');
}
