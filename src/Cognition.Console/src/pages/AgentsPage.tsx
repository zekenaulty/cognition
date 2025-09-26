import React from 'react';
import { Box, Typography, Card, CardContent, List, ListItem, ListItemText, Divider } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useAgentPersonaIndex } from '../hooks/useAgentPersonaIndex';

export default function AgentsPage() {
  const { auth } = useAuth();
  const navigate = useNavigate();
  const { agents, personaNameMap, loading } = useAgentPersonaIndex(auth?.accessToken);

  return (
    <Box sx={{ maxWidth: 900, mx: 'auto' }}>
      <Typography variant="h5" sx={{ mb: 2 }}>Agents</Typography>
      <Card variant="outlined">
        <CardContent>
          <List>
            {agents.map(agent => {
              const personaName = agent.personaId ? personaNameMap[agent.personaId] : undefined;
              const primary = personaName || agent.label || agent.id;
              const secondaryParts = [`AgentId: ${agent.id}`];
              if (agent.personaId) secondaryParts.push(`PersonaId: ${agent.personaId}`);
              return (
                <React.Fragment key={agent.id}>
                  <ListItem disableGutters onClick={() => navigate(`/agents/${agent.id}`)} sx={{ cursor: 'pointer' }}>
                    <ListItemText primary={primary} secondary={secondaryParts.join(' | ')} />
                  </ListItem>
                  <Divider component="li" />
                </React.Fragment>
              );
            })}
            {!loading && agents.length === 0 && (
              <Typography variant="caption" color="text.secondary">No agents available.</Typography>
            )}
            {loading && agents.length === 0 && (
              <Typography variant="caption" color="text.secondary">Loading agents...</Typography>
            )}
          </List>
        </CardContent>
      </Card>
    </Box>
  );
}
