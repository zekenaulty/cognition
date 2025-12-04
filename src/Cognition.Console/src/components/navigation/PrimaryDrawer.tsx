import React from 'react';
import { Drawer, Box, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Accordion, AccordionSummary, AccordionDetails, Tooltip, IconButton, Typography, Chip } from '@mui/material';
import HomeIcon from '@mui/icons-material/Home';
import ImageIcon from '@mui/icons-material/Image';
import WorkIcon from '@mui/icons-material/Work';
import ApiIcon from '@mui/icons-material/Api';
import DescriptionIcon from '@mui/icons-material/Description';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChatIcon from '@mui/icons-material/Chat';
import CloseIcon from '@mui/icons-material/Close';
import DeleteIcon from '@mui/icons-material/Delete';
import InsightsIcon from '@mui/icons-material/Insights';
import MenuBookIcon from '@mui/icons-material/MenuBook';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../auth/AuthContext';
import { useSecurity } from '../../hooks/useSecurity';
import { request } from '../../api/client';
import { useAgentPersonaIndex } from '../../hooks/useAgentPersonaIndex';

type ConversationSummary = { id: string; title?: string | null };
type ConversationCache = Record<string, ConversationSummary[]>;

export function PrimaryDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const navigate = useNavigate();
  const { isAuthenticated, auth } = useAuth();
  const security = useSecurity();
  const token = auth?.accessToken;
  const { agents } = useAgentPersonaIndex(token);
  const [expandedAgentId, setExpandedAgentId] = React.useState<string | false>(false);
  const [convsByAgent, setConvsByAgent] = React.useState<ConversationCache>({});
  const [recent, setRecent] = React.useState<Array<{ id: string; title?: string | null; createdAtUtc?: string; updatedAtUtc?: string | null }>>([]);

  const loadConversationsForAgent = React.useCallback(async (agentId: string) => {
    try {
      const list = await request<ConversationSummary[]>(`/api/conversations?agentId=${agentId}`, {}, token);
      setConvsByAgent(prev => ({ ...prev, [agentId]: list }));
    } catch {}
  }, [token]);

  const loadRecent = React.useCallback(async () => {
    try {
      const list = await request<Array<{ id: string; title?: string | null; createdAtUtc?: string; updatedAtUtc?: string | null }>>('/api/conversations', {}, token);
      const sorted = (list || []).slice().sort((a, b) => {
        const aNew = !(a.title && a.title.trim());
        const bNew = !(b.title && b.title.trim());
        if (aNew !== bNew) return aNew ? -1 : 1;
        const ta = Date.parse((a.updatedAtUtc || a.createdAtUtc || '')) || 0;
        const tb = Date.parse((b.updatedAtUtc || b.createdAtUtc || '')) || 0;
        return tb - ta;
      });
      setRecent(sorted.slice(0, 5));
    } catch {}
  }, [token]);

  React.useEffect(() => {
    if (isAuthenticated) {
      loadRecent();
    } else {
      setRecent([]);
      setConvsByAgent({});
      setExpandedAgentId(false);
    }
  }, [isAuthenticated, loadRecent]);

  React.useEffect(() => {
    if (expandedAgentId && !agents.some(a => a.id === expandedAgentId)) {
      setExpandedAgentId(false);
    }
    setConvsByAgent(prev => {
      const next: ConversationCache = {};
      agents.forEach(agent => {
        if (prev[agent.id]) {
          next[agent.id] = prev[agent.id];
        }
      });
      const prevKeys = Object.keys(prev);
      const nextKeys = Object.keys(next);
      if (prevKeys.length !== nextKeys.length) {
        return next;
      }
      for (const key of nextKeys) {
        if (prev[key] !== next[key]) {
          return next;
        }
      }
      return prev;
    });
  }, [agents, expandedAgentId]);

  const handleAccordion = React.useCallback((agentId: string) => (_: unknown, expanded: boolean) => {
    setExpandedAgentId(expanded ? agentId : false);
    if (expanded && !convsByAgent[agentId]) {
      loadConversationsForAgent(agentId);
    }
  }, [convsByAgent, loadConversationsForAgent]);

  const openRecentConversation = React.useCallback(async (convId: string) => {
    try {
      const convo = await request<{ id: string; agentId?: string; AgentId?: string }>(`/api/conversations/${convId}`, {}, token);
      const agentId = String(convo?.agentId ?? convo?.AgentId ?? '');
      if (agentId) {
        navigate(`/chat/${agentId}/${convId}`);
        onClose();
      }
    } catch {}
  }, [navigate, onClose, token]);

  const deleteConversation = React.useCallback(async (convId: string) => {
    if (!confirm('Delete this conversation?')) return;
    try {
      await request<void>(`/api/conversations/${convId}`, { method: 'DELETE' }, token);
      setRecent(prev => prev.filter(r => r.id !== convId));
      setConvsByAgent(prev => {
        const next: ConversationCache = {};
        Object.entries(prev).forEach(([aid, list]) => {
          const filtered = (list || []).filter(c => c.id !== convId);
          if (filtered.length > 0) next[aid] = filtered;
        });
        return next;
      });
    } catch {}
  }, [token]);

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
          {isAuthenticated && (
            <ListItem disablePadding>
              <ListItemButton component={Link} to="/agents" onClick={onClose} sx={{ pl: 0 }}>
                <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><WorkIcon /></ListItemIcon>
                <ListItemText primary="Agents" />
              </ListItemButton>
            </ListItem>
          )}
          {security.isAdmin && (
            <>
              <ListItem disablePadding>
                <ListItemButton component={Link} to="/operations/backlog" onClick={onClose} sx={{ pl: 0 }}>
                  <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><InsightsIcon /></ListItemIcon>
                  <ListItemText primary="Backlog Telemetry" />
                </ListItemButton>
              </ListItem>
              <ListItem disablePadding>
                <ListItemButton component={Link} to="/fiction/projects" onClick={onClose} sx={{ pl: 0 }}>
                  <ListItemIcon sx={{ minWidth: 0, pl: 0.75, mr: 1 }}><MenuBookIcon /></ListItemIcon>
                  <ListItemText primary="Fiction Projects" />
                </ListItemButton>
              </ListItem>
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
        {recent.length > 0 && (
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
            <Typography variant="subtitle2" sx={{ pl: 1, pr: 1, pt: 1, pb: 1, opacity: 0.9 }}>Agents</Typography>
            {agents.map(agent => (
              <Accordion key={agent.id} expanded={expandedAgentId === agent.id} onChange={handleAccordion(agent.id)} sx={{ bgcolor: '#0f1115', color: '#e0e0e0' }}>
                <AccordionSummary expandIcon={<ExpandMoreIcon htmlColor="#bbb" />} sx={{ pl: 2, pr: 0 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, width: '100%', justifyContent: 'space-between' }} onClick={(e) => e.stopPropagation()}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>{agent.label || agent.id.slice(0, 8)}</Typography>
                      <Chip size="small" label={agent.id.slice(0, 8)} variant="outlined" sx={{ height: 18, ml: 1 }} />
                    </Box>
                    <Tooltip title="New chat">
                      <IconButton size="small" onClick={(e) => { e.stopPropagation(); navigate(`/chat/${agent.id}`); onClose(); }} sx={{ color: '#9ad' }}>
                        <ChatIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </AccordionSummary>
                <AccordionDetails sx={{ pl: 2, pr: 0 }}>
                  <List dense disablePadding>
                    {(convsByAgent[agent.id] || []).map(c => (
                      <ListItem key={c.id} disablePadding secondaryAction={
                        <Tooltip title="Delete">
                          <IconButton edge="end" size="small" onClick={() => deleteConversation(c.id)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      }>
                        <ListItemButton onClick={() => { navigate(`/chat/${agent.id}/${c.id}`); onClose(); }} sx={{ pl: 2 }}>
                          <ListItemIcon sx={{ minWidth: 0, mr: 1 }}><ChatIcon fontSize="small" /></ListItemIcon>
                          <ListItemText primary={(c.title && c.title.trim()) ? c.title : 'New Chat'} />
                          <Chip size="small" label={c.id.slice(0, 8)} variant="outlined" sx={{ height: 18, ml: 1 }} />
                        </ListItemButton>
                      </ListItem>
                    ))}
                    {(!convsByAgent[agent.id] || convsByAgent[agent.id].length === 0) && (
                      <Typography variant="caption" color="text.secondary">No conversations</Typography>
                    )}
                  </List>
                </AccordionDetails>
              </Accordion>
            ))}
            {agents.length === 0 && (
              <Typography variant="caption" color="text.secondary">No agents available.</Typography>
            )}
          </Box>
        )}
      </Box>
    </Drawer>
  );
}
