import React from 'react';
import { Drawer, Box, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Divider, Accordion, AccordionSummary, AccordionDetails, Tooltip, IconButton, Typography, Chip } from '@mui/material';
import HomeIcon from '@mui/icons-material/Home';
import ImageIcon from '@mui/icons-material/Image';
import WorkIcon from '@mui/icons-material/Work';
import ApiIcon from '@mui/icons-material/Api';
import DescriptionIcon from '@mui/icons-material/Description';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChatIcon from '@mui/icons-material/Chat';
import CloseIcon from '@mui/icons-material/Close';
import DeleteIcon from '@mui/icons-material/Delete';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { useSecurity } from '../../hooks/useSecurity';
import { request } from '../../api/client';

export function PrimaryDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const navigate = useNavigate();
  const { isAuthenticated, auth } = useAuth();
  const security = useSecurity();
  const [personas, setPersonas] = React.useState<Array<{ id: string; name: string; isSystem?: boolean }>>([]);
  const [expandedId, setExpandedId] = React.useState<string | false>(false);
  const [convsByPersona, setConvsByPersona] = React.useState<Record<string, Array<{ id: string; title?: string | null }>>>({});
  const [recent, setRecent] = React.useState<Array<{ id: string; title?: string | null; createdAtUtc?: string; updatedAtUtc?: string | null }>>([]);

  async function loadPersonas() {
    try {
      const list = await request<Array<{ id: string; name: string; type?: number | string }>>('/api/personas', {}, auth?.accessToken);
      // Fetch default assistant persona (available to all authenticated users)
      let defaultSystem: { id: string; name: string } | null = null;
      try { defaultSystem = await request('/api/personas/default-assistant', {}, auth?.accessToken) as any } catch {}
      let filtered = (list || []).filter(p => {
        const t: any = (p as any).type;
        return t === 1 || t === 'Assistant' || t === 3 || t === 'RolePlayCharacter';
      }).map(p => ({ id: (p as any).id, name: (p as any).name }));
      // Ensure default system assistant is present for starting chats
      const pinnedId = defaultSystem ? (defaultSystem as any).id as string : '';
      if (pinnedId && !filtered.some(x => x.id === pinnedId)) {
        filtered = [{ id: pinnedId, name: (defaultSystem as any).name as string }, ...filtered];
      }
      const ordered = filtered;
      // Drop personas that have no conversations (except the pinned system persona)
      const toCheck = ordered.filter(p => p.id !== pinnedId);
      const results = await Promise.all(toCheck.map(async (p) => {
        try {
          const convs = await request<Array<{ id: string }>>(`/api/conversations?participantId=${p.id}`, {}, auth?.accessToken);
          return { p, convs };
        } catch { return { p, convs: [] as Array<{ id: string }> } }
      }));
      const kept = [
        ...(pinnedId ? ordered.filter(x => x.id === pinnedId) : []),
        ...results.filter(r => (r.convs || []).length > 0).map(r => r.p)
      ].map(x => ({ ...x, isSystem: x.id === pinnedId }));
      setPersonas(kept);
      // Prune conversations for personas no longer visible
      const preloads = Object.fromEntries(results.map(r => [r.p.id, r.convs || []]));
      setConvsByPersona(prev => {
        const merged = { ...prev, ...preloads };
        return Object.fromEntries(Object.entries(merged).filter(([pid]) => kept.some(f => f.id === pid)));
      });
    } catch {}
  }
  async function loadConversations(pid: string) { try { const list = await request<Array<{ id: string; title?: string | null }>>(`/api/conversations?participantId=${pid}`, {}, auth?.accessToken); setConvsByPersona(prev => ({ ...prev, [pid]: list })); } catch {} }
  async function loadRecent() {
    try {
      const list = await request<Array<{ id: string; title?: string | null; createdAtUtc?: string; updatedAtUtc?: string | null }>>(`/api/conversations`, {}, auth?.accessToken);
      const sorted = (list || []).slice().sort((a, b) => {
        const aNew = !(a.title && a.title.trim());
        const bNew = !(b.title && b.title.trim());
        if (aNew !== bNew) return aNew ? -1 : 1; // new chats first
        const ta = Date.parse((a.updatedAtUtc || a.createdAtUtc || '')) || 0;
        const tb = Date.parse((b.updatedAtUtc || b.createdAtUtc || '')) || 0;
        return tb - ta; // newest first
      });
      setRecent(sorted.slice(0, 5));
    } catch {}
  }
  const handleAccordion = (pid: string) => (_: any, expanded: boolean) => { setExpandedId(expanded ? pid : false); if (expanded && !convsByPersona[pid]) loadConversations(pid); };
  React.useEffect(() => { if (isAuthenticated) { loadPersonas(); loadRecent(); } }, [isAuthenticated]);
  // Refresh personas if another view updates persona types
  React.useEffect(() => {
    const handler = () => { if (isAuthenticated) loadPersonas(); };
    window.addEventListener('cognition-personas-changed', handler as any);
    return () => { window.removeEventListener('cognition-personas-changed', handler as any); };
  }, [isAuthenticated]);

  async function openRecentConversation(convId: string) {
    try {
      const msgs = await request<Array<{ fromPersonaId?: string; FromPersonaId?: string; role: any }>>(`/api/conversations/${convId}/messages`, {}, auth?.accessToken);
      let pid = '';
      const normalizeRole = (r: any) => { if (r === 1 || r === '1' || r === 'user' || r === 'User') return 'user'; if (r === 2 || r === '2' || r === 'assistant' || r === 'Assistant') return 'assistant'; if (r === 0 || r === '0' || r === 'system' || r === 'System') return 'system'; const n = Number(r); if (!Number.isNaN(n)) return n === 2 ? 'assistant' : (n === 0 ? 'system' : 'user'); return 'user'; };
      const assistant = (msgs || []).find(m => normalizeRole((m as any).role) === 'assistant');
      if (assistant) pid = String((assistant as any).fromPersonaId ?? (assistant as any).FromPersonaId ?? '');
      if (!pid) { try { pid = localStorage.getItem('cognition.chat.personaId') || '' } catch {} }
      if (!pid && personas.length > 0) pid = personas[0].id;
      if (pid) { navigate(`/chat/${pid}/${convId}`); onClose(); }
    } catch {}
  }

  async function deleteConversation(convId: string) {
    if (!confirm('Delete this conversation?')) return;
    try { await request<void>(`/api/conversations/${convId}`, { method: 'DELETE' }, auth?.accessToken); setRecent(prev => prev.filter(r => r.id !== convId)); setConvsByPersona(prev => Object.fromEntries(Object.entries(prev).map(([pid, list]) => [pid, (list || []).filter(c => c.id !== convId)]))); } catch {}
  }

  return (
    <Drawer anchor="left" open={open} onClose={onClose} PaperProps={{ sx: { bgcolor: '#0b0c10', color: '#e0e0e0' } }}>
      <Box sx={{ width: 375 }} role="presentation">
        <Box sx={{ position: 'sticky', top: 0, zIndex: 1, bgcolor: '#0b0c10', display: 'flex', justifyContent: 'flex-end', p: 1, borderBottom: '1px solid #1a1c22' }}>
          <Tooltip title="Close">
            <IconButton size="small" onClick={onClose} sx={{ color: '#bbb' }}>
              <CloseIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
        <List disablePadding>
          <ListItem disablePadding>
            <ListItemButton component={Link} to="/" onClick={onClose} sx={{ pl: 0 }}>
              <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><HomeIcon /></ListItemIcon>
              <ListItemText primary="Home" />
            </ListItemButton>
          </ListItem>
          {isAuthenticated && (
            <ListItem disablePadding>
              <ListItemButton component={Link} to="/image-lab" onClick={onClose} sx={{ pl: 0 }}>
                <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><ImageIcon /></ListItemIcon>
                <ListItemText primary="Image Lab" />
              </ListItemButton>
            </ListItem>
          )}
          {security.isAdmin && (
            <>
              <ListItem disablePadding>
                <ListItemButton onClick={() => { window.open('/hangfire', 'hangfireTab'); onClose(); }} sx={{ pl: 0 }}>
                  <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><WorkIcon /></ListItemIcon>
                  <ListItemText primary="Jobs" />
                </ListItemButton>
              </ListItem>
              <ListItem disablePadding>
                <ListItemButton onClick={() => { window.open('/openapi/v1.json', 'apiJsonTab'); onClose(); }} sx={{ pl: 0 }}>
                  <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><ApiIcon /></ListItemIcon>
                  <ListItemText primary="API JSON" />
                </ListItemButton>
              </ListItem>
              <ListItem disablePadding>
                <ListItemButton onClick={() => { window.open('/swagger', 'swaggerTab'); onClose(); }} sx={{ pl: 0 }}>
                  <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><DescriptionIcon /></ListItemIcon>
                  <ListItemText primary="Swagger" />
                </ListItemButton>
              </ListItem>
            </>
          )}
        </List>
        <Divider />
        {isAuthenticated && (
          <Box sx={{ pr: 1, pb: 1 }}>
            <Typography variant="subtitle2" sx={{ pl: 1, pr: 1, pt: 1, pb: 1, opacity: 0.9 }}>Recent</Typography>
            <List dense disablePadding>
              {recent.map(r => (
                <ListItem key={r.id} disablePadding secondaryAction={
                  <Tooltip title="Delete">
                    <IconButton edge="end" size="small" onClick={() => deleteConversation(r.id)}>
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                }>
                  <ListItemButton onClick={() => openRecentConversation(r.id)} sx={{ pl: 2 }}>
                    <ListItemIcon sx={{ minWidth: 0, mr: 1 }}><ChatIcon fontSize="small" /></ListItemIcon>
                    <ListItemText primary={(r.title && r.title.trim()) ? r.title : 'New Chat'} />
                  </ListItemButton>
                </ListItem>
              ))}
              {recent.length === 0 && (
                <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>No recent conversations</Typography>
              )}
            </List>
          </Box>
        )}
        {isAuthenticated && (
          <Box sx={{ pr: 1, pb: 2 }}>
            <Typography variant="subtitle2" sx={{ pl: 1, pr: 1, pt: 1, pb: 1, opacity: 0.9 }}>Personas</Typography>
            {personas.map(p => (
              <Accordion key={p.id} expanded={expandedId === p.id} onChange={handleAccordion(p.id)} sx={{ bgcolor: '#0f1115', color: '#e0e0e0' }}>
                <AccordionSummary expandIcon={<ExpandMoreIcon htmlColor="#bbb" />} sx={{ pl: 2, pr: 0 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, width: '100%', justifyContent: 'space-between' }} onClick={(e) => e.stopPropagation()}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>{p.name}</Typography>
                      {p.isSystem && <Chip size="small" label="System" variant="outlined" sx={{ height: 18 }} />}
                    </Box>
                    <Tooltip title="New chat">
                      <IconButton size="small" onClick={(e) => { e.stopPropagation(); navigate(`/chat/${p.id}`); onClose(); }} sx={{ color: '#9ad' }}>
                        <ChatIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </AccordionSummary>
                <AccordionDetails sx={{ pl: 2, pr: 0 }}>
                  <List dense disablePadding>
                    {(convsByPersona[p.id] || []).map(c => (
                      <ListItem key={c.id} disablePadding secondaryAction={
                        <Tooltip title="Delete">
                          <IconButton edge="end" size="small" onClick={() => deleteConversation(c.id)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      }>
                        <ListItemButton onClick={() => { navigate(`/chat/${p.id}/${c.id}`); onClose(); }} sx={{ pl: 2 }}>
                          <ListItemIcon sx={{ minWidth: 0, mr: 1 }}><ChatIcon fontSize="small" /></ListItemIcon>
                          <ListItemText primary={(c.title && c.title.trim()) ? c.title : 'New Chat'} />
                        </ListItemButton>
                      </ListItem>
                    ))}
                    {(!convsByPersona[p.id] || convsByPersona[p.id].length === 0) && (
                      <Typography variant="caption" color="text.secondary">No conversations</Typography>
                    )}
                  </List>
                </AccordionDetails>
              </Accordion>
            ))}
          </Box>
        )}
      </Box>
    </Drawer>
  );
}
