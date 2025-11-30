import React from 'react';
import { Box, Typography, Card, CardContent, List, ListItem, ListItemText, Divider, FormControl, InputLabel, Select, MenuItem, Button, Stack } from '@mui/material';
import { useParams } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export default function AgentDetailPage() {
  const { auth } = useAuth();
  const { agentId } = useParams<{ agentId: string }>();
  const [agent, setAgent] = React.useState<any>(null);
  const [profiles, setProfiles] = React.useState<Array<any>>([]);
  const [selectedProfileId, setSelectedProfileId] = React.useState<string>('');

  React.useEffect(() => {
    (async () => {
      try {
        const res = await fetch(`/api/agents/${agentId}`, { headers: auth?.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : {} });
        if (res.ok) {
          const a = await res.json();
          setAgent(a);
          if (a?.clientProfileId) setSelectedProfileId(String(a.clientProfileId));
        }
      } catch {}
    })();
  }, [agentId, auth?.accessToken]);

  React.useEffect(() => {
    (async () => {
      try {
        const res = await fetch('/api/client-profiles', { headers: auth?.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : {} });
        if (res.ok) setProfiles(await res.json());
      } catch {}
    })();
  }, [auth?.accessToken]);

  async function saveProfile() {
    try {
      if (!selectedProfileId) return;
      await fetch(`/api/agents/${agentId}/client-profile`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json', ...(auth?.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : {}) },
        body: JSON.stringify(selectedProfileId)
      });
    } catch {}
  }

  return (
    <Box sx={{ maxWidth: 900, mx: 'auto' }}>
      <Typography variant="h5" sx={{ mb: 2 }}>Agent Detail</Typography>
      <Card variant="outlined">
        <CardContent>
          {!agent && <Typography variant="caption" color="text.secondary">Loading...</Typography>}
          {agent && (
            <>
              <Typography variant="subtitle1">AgentId: {agent.id}</Typography>
              <Typography variant="subtitle2" sx={{ opacity: 0.8 }}>PersonaId: {agent.personaId ?? '(none)'} </Typography>
              <Stack direction="row" spacing={2} sx={{ my: 2 }}>
                <FormControl size="small" sx={{ minWidth: 260 }}>
                  <InputLabel>Client Profile</InputLabel>
                  <Select label="Client Profile" value={selectedProfileId} onChange={e => setSelectedProfileId(e.target.value as string)}>
                    {profiles.map(p => (
                      <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <Button variant="contained" onClick={saveProfile}>Save</Button>
              </Stack>
              <Divider sx={{ my: 2 }} />
              <Typography variant="subtitle1" sx={{ mb: 1 }}>Tool Bindings</Typography>
              <List>
                {(agent.toolBindings || []).map((b: any) => (
                  <React.Fragment key={b.id}>
                    <ListItem disableGutters>
                      <ListItemText primary={b.toolName} secondary={`Enabled: ${b.enabled}`} />
                    </ListItem>
                    <Divider component="li" />
                  </React.Fragment>
                ))}
                {(!agent.toolBindings || agent.toolBindings.length === 0) && (
                  <Typography variant="caption" color="text.secondary">No tool bindings.</Typography>
                )}
              </List>
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
