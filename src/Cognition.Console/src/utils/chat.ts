export type NormalizedRole = 'system' | 'user' | 'assistant';

export function normalizeRole(r: any): NormalizedRole {
  if (r === 1 || r === '1' || r === 'user' || r === 'User') return 'user';
  if (r === 2 || r === '2' || r === 'assistant' || r === 'Assistant') return 'assistant';
  if (r === 0 || r === '0' || r === 'system' || r === 'System') return 'system';
  if (typeof r === 'string') {
    const t = r.toLowerCase();
    if (t === 'system' || t === 'user' || t === 'assistant') return t as NormalizedRole;
    const n = Number(t);
    if (!Number.isNaN(n)) return normalizeRole(n);
  }
  if (typeof r === 'number') {
    if (r === 0) return 'system';
    if (r === 2) return 'assistant';
    return 'user';
  }
  return 'user';
}

