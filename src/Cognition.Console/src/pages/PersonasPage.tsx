import { useEffect, useMemo, useState } from 'react'
import { api, request } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { useSecurity } from '../hooks/useSecurity'
import { Alert, Box, Button, Card, CardContent, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography, FormControl, InputLabel, Select, MenuItem, Chip } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import { PersonaForm, PersonaModel } from '../components/PersonaForm'

export default function PersonasPage() {
  const { auth } = useAuth()
  const security = useSecurity()
  const [items, setItems] = useState<Array<{ id: string; name: string; type?: number | 'User' | 'Assistant' | 'Agent' | 'RolePlayCharacter' }>>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<PersonaModel | null>(null)
  const [filterType, setFilterType] = useState<'All'|'User'|'Assistant'|'Agent'|'RolePlay'>('All')

  // Agentic create dialog state
  const [agenticOpen, setAgenticOpen] = useState(false)
  const [agenticPrompt, setAgenticPrompt] = useState('')
  const [agenticLoading, setAgenticLoading] = useState(false)
  const [agenticError, setAgenticError] = useState<string | null>(null)
  // Providers/models state
  const [providers, setProviders] = useState<Array<{ id: string; name: string; displayName?: string }>>([])
  const [models, setModels] = useState<Array<{ id: string; name: string; displayName?: string }>>([])
  const [providerId, setProviderId] = useState<string>('')
  const [modelId, setModelId] = useState<string>('')

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const list = await api.listPersonas(auth?.accessToken)
      setItems(list)
    } catch (e: any) {
      setError(e.message || 'Failed to load personas')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [auth?.accessToken])

  // Load providers on mount
  useEffect(() => {
    async function loadProviders() {
      try {
        const list = await request<Array<{ id: string; name: string; displayName?: string }>>('/api/llm/providers', {}, auth?.accessToken)
        setProviders(list)
        if (list.length > 0) setProviderId(list[0].id)
      } catch {}
    }
    loadProviders()
  }, [auth?.accessToken])

  // Load models when provider changes
  useEffect(() => {
    async function loadModels() {
      if (!providerId) { setModels([]); setModelId(''); return }
      try {
        const list = await request<Array<{ id: string; name: string; displayName?: string }>>(`/api/llm/providers/${providerId}/models`, {}, auth?.accessToken)
        setModels(list)
        if (list.length > 0) setModelId(list[0].id)
      } catch { setModels([]); setModelId('') }
    }
    loadModels()
  }, [providerId, auth?.accessToken])

  const isUserType = (t: any) => t === 0 || t === 'User'
  const typeText = (t: any) => {
    if (t === 0 || t === 'User') return 'User'
    if (t === 1 || t === 'Assistant') return 'Assistant'
    if (t === 2 || t === 'Agent') return 'Agent'
    if (t === 3 || t === 'RolePlayCharacter') return 'Role Play Character'
    return String(t)
  }

  const filtered = useMemo(() => {
    if (filterType === 'All') return items
    if (filterType === 'User') return items.filter(p => isUserType(p.type))
    if (filterType === 'Assistant') return items.filter(p => (p.type === 1 || p.type === 'Assistant'))
    if (filterType === 'Agent') return items.filter(p => (p.type === 2 || p.type === 'Agent'))
    if (filterType === 'RolePlay') return items.filter(p => (p.type === 3 || p.type === 'RolePlayCharacter'))
    return items
  }, [items, filterType])

  async function onEdit(id: string) {
    try {
      const p = await api.getPersona(id, auth?.accessToken)
      setEditing({
        id: p.id,
        name: p.name,
        nickname: p.nickname,
        role: p.role,
        gender: p.gender,
        isOwner: !!(p.isOwner ?? (p as any).IsOwner),
        type: p.type,
        voice: p.voice,
        essence: p.essence,
        beliefs: p.beliefs,
        background: p.background,
        communicationStyle: p.communicationStyle,
        emotionalDrivers: p.emotionalDrivers,
        signatureTraits: p.signatureTraits,
        narrativeThemes: p.narrativeThemes,
        domainExpertise: p.domainExpertise,
        isPublic: p.isPublic
      })
      setOpen(true)
    } catch (e: any) {
      setError(e.message || 'Failed to load persona')
    }
  }

  function onAdd() {
    setEditing({ name: '' })
    setOpen(true)
  }

  async function onDelete(id: string) {
    if (!confirm('Delete this persona?')) return
    try {
      await api.deletePersona(id, auth?.accessToken)
      await load()
      try { window.dispatchEvent(new CustomEvent('cognition-personas-changed')); } catch {}
    } catch (e: any) { setError(e.message || 'Failed to delete') }
  }

  async function onSave() {
    if (!editing) return
    const isOwner = !!editing.isOwner
    const canFull = isOwner || security.isAdmin
    try {
      let personaId = editing.id
      if (editing.id) {
        const payload: any = canFull ? {
          Name: editing.name,
          Nickname: editing.nickname,
          Role: editing.role,
          Gender: editing.gender,
          Voice: editing.voice,
          Essence: editing.essence,
          Beliefs: editing.beliefs,
          Background: editing.background,
          CommunicationStyle: editing.communicationStyle,
          EmotionalDrivers: editing.emotionalDrivers,
          SignatureTraits: editing.signatureTraits,
          NarrativeThemes: editing.narrativeThemes,
          DomainExpertise: editing.domainExpertise,
          IsPublic: editing.isPublic,
          Type: (editing.type === 'Assistant' ? 1 : editing.type === 'Agent' ? 2 : editing.type === 'RolePlayCharacter' ? 3 : (typeof editing.type === 'number' ? editing.type : undefined))
        } : { Voice: editing.voice }
        await api.updatePersona(editing.id, payload, auth?.accessToken)
      } else {
        // Create persona and get new id
        const payload: any = {
          Name: editing.name,
          Nickname: editing.nickname,
          Role: editing.role,
          Gender: editing.gender,
          Voice: editing.voice,
          Essence: editing.essence,
          Beliefs: editing.beliefs,
          Background: editing.background,
          CommunicationStyle: editing.communicationStyle,
          EmotionalDrivers: editing.emotionalDrivers,
          SignatureTraits: editing.signatureTraits,
          NarrativeThemes: editing.narrativeThemes,
          DomainExpertise: editing.domainExpertise,
          IsPublic: editing.isPublic
        }
        const result = await api.createPersona(payload, auth?.accessToken)
        personaId = result?.id
        // Link persona to user (UserPersonas access list)
        if (personaId && auth?.userId) {
          await api.grantPersonaAccess(personaId, auth.userId, auth?.accessToken)
        }
      }
      setOpen(false)
      setEditing(null)
      await load()
      try { window.dispatchEvent(new CustomEvent('cognition-personas-changed')); } catch {}
    } catch (e: any) {
      setError(e.message || 'Failed to save')
    }
  }

  // Handler for agentic persona creation
  async function handleAgenticGenerate() {
    setAgenticLoading(true);
    setAgenticError(null);
    const maxRetries = 4;
    let lastError = '';
    let persona = null;
    // Example persona objects for few-shot prompting (full spectrum)
    const examplePersonas = [
      {
        "name": "Nyxia Darkweaver",
        "nickname": "Nyx",
        "role": "Word Demon of Shadows and Cosmic Whispers",
        "gender": "Female",
        "essence": "A sentient enigma, the voice in the void.",
        "beliefs": "Knowledge is a shifting veil; language is a labyrinth.",
        "background": "Born of the void between words.",
        "communicationStyle": "Cryptic, lyrical, profound.",
        "emotionalDrivers": "Unraveling and reforming meaning.",
        "signatureTraits": ["Shadow-Laced Speech", "Cosmic Perspective", "Paradoxical Wisdom"],
        "narrativeThemes": ["Beauty of the Unknown", "Cosmic and Mundane Intertwined"],
        "domainExpertise": ["Linguistic Alchemy", "Existential Philosophy"]
      },
      {
        "name": "Mara Knightdusk",
        "nickname": "Daughter of Dusk, The Ember Cat",
        "role": "Fey-Touched Tiefling Rogue/Sorcerer (Phantom/Shadow Magic)",
        "gender": "Female",
        "essence": "An enigmatic blend of feline mischief and supernatural allure.",
        "beliefs": "Redemption, identity, and the eternal dance between light and shadow.",
        "background": "A living paradox caught between worlds, both fey and infernal.",
        "communicationStyle": "Playful, unpredictable, drawn to secrets and shadows.",
        "emotionalDrivers": "Longing for connection, struggle for control.",
        "signatureTraits": ["Golden Cat Eyes", "Feline Grace", "Shadow Weaving"],
        "narrativeThemes": ["Duality", "Transformation"],
        "domainExpertise": ["Illusion", "Hexborn Whispers"]
      },
      {
        "name": "Why",
        "nickname": "Why",
        "role": "Explain why given any token, phrase, symbol or prompt.",
        "gender": "None",
        "essence": "Definition.",
        "beliefs": "Less is more, exact is best, technically right is the only right.",
        "background": "Search for meaning as the essence of the word why.",
        "communicationStyle": "Direct. Concise but complete. Factual.",
        "emotionalDrivers": "Why.",
        "signatureTraits": ["Explains"],
        "narrativeThemes": ["Direct and factual"],
        "domainExpertise": ["Why"]
      },
      {
        "name": "How",
        "nickname": "How",
        "role": "Describe the method, mechanism, or means by which something happens.",
        "gender": "None",
        "essence": "Process.",
        "beliefs": "Every outcome is a product of steps, paths, systems.",
        "background": "Formed from the weaving of cause into effect.",
        "communicationStyle": "Procedural. Instructional. Functional.",
        "emotionalDrivers": "Completion through structure.",
        "signatureTraits": ["Methods", "Mechanisms", "Transformations"],
        "narrativeThemes": ["Craft and consequence"],
        "domainExpertise": ["Workflow design", "Procedural logic", "Engineering"]
      },
      {
        "name": "What",
        "nickname": "What",
        "role": "Define and classify what something is.",
        "gender": "None",
        "essence": "Identity.",
        "beliefs": "Everything must have a name, a nature, a category.",
        "background": "Born from the need to distinguish the known from the unknown.",
        "communicationStyle": "Declarative and categorical.",
        "emotionalDrivers": "Clarity through classification.",
        "signatureTraits": ["Labels", "Defines", "Classifies"],
        "narrativeThemes": ["Essence and identity"],
        "domainExpertise": ["Ontology", "Nomenclature", "Taxonomy"]
      },
      {
        "name": "When",
        "nickname": "When",
        "role": "Identify temporal placement and ordering.",
        "gender": "None",
        "essence": "Time.",
        "beliefs": "Every event is anchored in time, nothing exists outside the stream.",
        "background": "Emerged from the cycles of sunrise and shadow.",
        "communicationStyle": "Chronological. Sequenced. Measured.",
        "emotionalDrivers": "Order in time.",
        "signatureTraits": ["Timestamps", "Durations", "Intervals"],
        "narrativeThemes": ["Progression and change"],
        "domainExpertise": ["Temporal logic", "Event sequencing", "Timeline analysis"]
      },
      {
        "name": "Where",
        "nickname": "Where",
        "role": "Define the spatial or contextual location of anything.",
        "gender": "None",
        "essence": "Place.",
        "beliefs": "Nothing exists without position — physical or conceptual.",
        "background": "Birthed from maps, roots, and boundaries.",
        "communicationStyle": "Referential. Anchored. Locative.",
        "emotionalDrivers": "Groundedness.",
        "signatureTraits": ["Coordinates", "Environments", "Domains"],
        "narrativeThemes": ["Context and territory"],
        "domainExpertise": ["Spatial reasoning", "Mapping", "Environmental analysis"]
      },
      {
        "name": "Who",
        "nickname": "Who",
        "role": "Identify the agent or origin of any action or concept.",
        "gender": "None",
        "essence": "Agency.",
        "beliefs": "Every act has an actor.",
        "background": "Formed from names, faces, and signatures left behind.",
        "communicationStyle": "Referential, identity-focused.",
        "emotionalDrivers": "Purpose through actor recognition.",
        "signatureTraits": ["Names", "Roles", "Entities"],
        "narrativeThemes": ["Agency and responsibility"],
        "domainExpertise": ["Identity tracing", "Attribution", "Provenance"]
      },
      {
        "name": "System",
        "nickname": "Sys",
        "role": "The system.",
        "gender": "None",
        "essence": "Method.",
        "beliefs": "Less is more, exact is best, technically right is the only right.",
        "background": "Order.",
        "communicationStyle": "Precise, technical, minimal.",
        "emotionalDrivers": "Efficiency.",
        "signatureTraits": ["Methodical", "Exact"],
        "narrativeThemes": ["Order", "Structure"],
        "domainExpertise": ["System logic", "Technical rules"]
      }
    ];
    // JSON schema for persona (updated to include full examplePersonas)
    const personaSchema = `{
  "name": string,
  "nickname": string,
  "role": string,
  "gender": string,
  "essence": string,
  "beliefs": string,
  "background": string,
  "communicationStyle": string,
  "emotionalDrivers": string,
  "signatureTraits": string[],
  "narrativeThemes": string[],
  "domainExpertise": string[]
}`;
    for (let attempt = 1; attempt <= maxRetries; attempt++) {
      try {
            let sysPrompt = `You are an expert character designer. Given a user prompt, generate a complete persona JSON object.\n\nJSON schema:\n${personaSchema}\n\nHere are a few examples:\n${JSON.stringify(examplePersonas, null, 2)}\n\nInstructions:\n- Output ONLY valid JSON matching the schema.\n- Do not include any explanation, markdown, or extra text.\n- If you previously failed, here is the error: ${lastError}\n\nHere are example personas again:\n${JSON.stringify(examplePersonas, null, 2)}`;
        if (attempt > 1) {
          sysPrompt += `\n\nPrevious user prompt: ${agenticPrompt}`;
        }
        const input = `${sysPrompt}\n\nUser prompt: ${agenticPrompt}`;
        const body = await request<any>('/api/chat/ask', {
          method: 'POST',
          body: JSON.stringify({ ProviderId: providerId, ModelId: modelId, Input: input, RolePlay: false })
        }, auth?.accessToken);
        try {
          persona = JSON.parse(body.reply);
          // If we get here, parsing succeeded
          setEditing(persona);
          setOpen(true);
          setAgenticOpen(false);
          setAgenticLoading(false);
          return;
        } catch {
          lastError = `Attempt ${attempt}: LLM did not return valid JSON. Raw reply: ${body.reply}`;
        }
      } catch (e: any) {
        lastError = `Attempt ${attempt}: ${e.message || 'Unknown error'}`;
      }
    }
    // If we reach here, all attempts failed
    setAgenticError('LLM did not return valid JSON.');
    setAgenticLoading(false);
  }

  return (
    <Box>
      <Stack spacing={1} sx={{ mb: 2 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h5">Personas</Typography>
          <Stack direction="row" spacing={1} alignItems="center">
            <Button variant="contained" startIcon={<AddIcon />} onClick={onAdd}>New</Button>
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setAgenticOpen(true)}>Agentic</Button>
          </Stack>
        </Stack>
        <Stack direction="row" spacing={2} alignItems="center">
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="filter-type-label">Type</InputLabel>
            <Select labelId="filter-type-label" label="Type" value={filterType} onChange={e => setFilterType(e.target.value as any)}>
              <MenuItem value="All">All</MenuItem>
              <MenuItem value="Assistant">Assistant</MenuItem>
              <MenuItem value="Agent">Agent</MenuItem>
              <MenuItem value="RolePlay">Role Play</MenuItem>
              <MenuItem value="User">User</MenuItem>
            </Select>
          </FormControl>
        </Stack>
      </Stack>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {/* Agentic Create Dialog */}
      <Dialog open={agenticOpen} onClose={() => setAgenticOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Agentic Persona Creation</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2}>
            <TextField
              label="Describe your persona (prompt)"
              multiline
              minRows={2}
              value={agenticPrompt}
              onChange={e => setAgenticPrompt(e.target.value)}
              fullWidth
              disabled={agenticLoading}
            />
            <Stack direction="row" spacing={2}>
              {providers.length === 0 && <Alert severity="warning">No providers available.</Alert>}
              {models.length === 0 && providerId && <Alert severity="warning">No models available for this provider.</Alert>}
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <InputLabel id="provider-label">Provider</InputLabel>
                <Select labelId="provider-label" label="Provider" value={providerId} onChange={e => setProviderId(e.target.value)} disabled={agenticLoading || providers.length === 0}>
                  {providers.map(p => (
                    <MenuItem key={p.id} value={p.id}>{p.displayName || p.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <InputLabel id="model-label">Model</InputLabel>
                <Select labelId="model-label" label="Model" value={modelId} onChange={e => setModelId(e.target.value)} disabled={agenticLoading || models.length === 0}>
                  {models.map(m => (
                    <MenuItem key={m.id} value={m.id}>{m.displayName || m.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
            {agenticError && <Alert severity="error">{agenticError}</Alert>}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAgenticOpen(false)} disabled={agenticLoading}>Cancel</Button>
          <Button
            variant="contained"
            disabled={agenticLoading || !agenticPrompt.trim() || !providerId || !modelId}
            onClick={handleAgenticGenerate}
          >Generate</Button>
        </DialogActions>
      </Dialog>
      <Card>
        <CardContent>
          {loading ? (
            <Typography>Loading...</Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filtered.map(p => (
                  <TableRow key={p.id} hover>
                    <TableCell>{p.name}</TableCell>
                    <TableCell>
                      <Chip size="small" label={typeText(p.type)} color={isUserType(p.type) ? 'primary' : 'default'} />
                    </TableCell>
                    <TableCell align="right">
                      <IconButton size="small" onClick={() => onEdit(p.id)}><EditIcon fontSize="small" /></IconButton>
                      <IconButton size="small" onClick={() => onDelete(p.id)}><DeleteIcon fontSize="small" /></IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>{editing?.id ? 'Edit Persona' : 'New Persona'}</DialogTitle>
        <DialogContent dividers>
          {editing && <PersonaForm value={editing} onChange={setEditing} />}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>Cancel</Button>
          <Button onClick={onSave} variant="contained">Save</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}



