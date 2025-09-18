import { Box, Button, Chip, Grid2 as Grid, Stack, Switch, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { useSecurity } from '../hooks/useSecurity'

export type PersonaModel = {
  id?: string
  name: string
  nickname?: string
  role?: string
  gender?: string
  isOwner?: boolean
  type?: number | 'User' | 'Assistant' | 'Agent' | 'RolePlayCharacter'
  essence?: string
  beliefs?: string
  background?: string
  communicationStyle?: string
  emotionalDrivers?: string
  signatureTraits?: string[]
  narrativeThemes?: string[]
  domainExpertise?: string[]
  isPublic?: boolean
  voice?: string
}

export function PersonaForm({ value, onChange }: { value: PersonaModel; onChange: (p: PersonaModel) => void }) {
  const [model, setModel] = useState<PersonaModel>(value)
  const security = useSecurity()
  useEffect(() => setModel(value), [value])
  function set<K extends keyof PersonaModel>(k: K, v: PersonaModel[K]) {
    const next = { ...model, [k]: v }
    setModel(next)
    onChange(next)
  }

  function toList(s: string | undefined) {
    return s ? s.split(',').map(x => x.trim()).filter(Boolean) : []
  }

  return (
    <Stack spacing={2}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField label="Name" value={model.name} onChange={e => set('name', e.target.value)} fullWidth required />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField label="Nickname" value={model.nickname || ''} onChange={e => set('nickname', e.target.value)} fullWidth />
        </Grid>
        {(model.isOwner || security.isAdmin) && (
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField
              label="Type"
              select
              fullWidth
              value={model.type as any ?? 1}
              onChange={e => set('type', Number(e.target.value))}
              SelectProps={{ native: true, inputProps: { title: 'Type' } }}
              helperText="Choose how this persona behaves"
            >
              {/* Intentionally omit 'User' from options */}
              <option value={1}>Assistant</option>
              <option value={2}>Agent</option>
              <option value={3}>Role Play Character</option>
            </TextField>
          </Grid>
        )}
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField label="Role" value={model.role || ''} onChange={e => set('role', e.target.value)} fullWidth />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField label="Gender" value={model.gender || ''} onChange={e => set('gender', e.target.value)} fullWidth />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <VoicePicker value={model.voice || ''} onChange={(v) => set('voice', v)} />
        </Grid>
        <Grid size={12}>
          <TextField label="Essence" value={model.essence || ''} onChange={e => set('essence', e.target.value)} fullWidth multiline minRows={2} />
        </Grid>
        <Grid size={12}>
          <TextField label="Beliefs" value={model.beliefs || ''} onChange={e => set('beliefs', e.target.value)} fullWidth multiline minRows={2} />
        </Grid>
        <Grid size={12}>
          <TextField label="Background" value={model.background || ''} onChange={e => set('background', e.target.value)} fullWidth multiline minRows={2} />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField label="Communication Style" value={model.communicationStyle || ''} onChange={e => set('communicationStyle', e.target.value)} fullWidth />
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField label="Emotional Drivers" value={model.emotionalDrivers || ''} onChange={e => set('emotionalDrivers', e.target.value)} fullWidth />
        </Grid>
        <Grid size={12}>
          <TextField label="Signature Traits (comma-separated)" value={(model.signatureTraits || []).join(', ')} onChange={e => set('signatureTraits', toList(e.target.value))} fullWidth />
        </Grid>
        <Grid size={12}>
          <TextField label="Narrative Themes (comma-separated)" value={(model.narrativeThemes || []).join(', ')} onChange={e => set('narrativeThemes', toList(e.target.value))} fullWidth />
        </Grid>
        <Grid size={12}>
          <TextField label="Domain Expertise (comma-separated)" value={(model.domainExpertise || []).join(', ')} onChange={e => set('domainExpertise', toList(e.target.value))} fullWidth />
        </Grid>
        <Grid size={12}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Typography>Public</Typography>
            <Switch checked={!!model.isPublic} onChange={(e) => set('isPublic', e.target.checked)} />
          </Stack>
        </Grid>
      </Grid>
    </Stack>
  )
}

function VoicePicker({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const [voices, setVoices] = useState<SpeechSynthesisVoice[]>([])

  useEffect(() => {
    const load = () => {
      try { setVoices(window.speechSynthesis?.getVoices?.() || []) } catch { setVoices([]) }
    }
    load()
    try { window.speechSynthesis?.addEventListener?.('voiceschanged', load as any) } catch {}
    return () => { try { window.speechSynthesis?.removeEventListener?.('voiceschanged', load as any) } catch {} }
  }, [])

  const handleTest = () => {
    try {
      const utter = new window.SpeechSynthesisUtterance('This is a test of the selected voice.');
      const v = (window.speechSynthesis?.getVoices?.() || []).find(x => x.name === value || x.voiceURI === value)
      if (v) utter.voice = v
      window.speechSynthesis?.speak(utter)
    } catch {}
  }

  const options = voices.map(v => ({ key: v.voiceURI || v.name, label: `${v.name} (${v.lang})` }))

  return (
    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
      <TextField
        select
        fullWidth
        label="TTS Voice"
        value={value}
        onChange={e => onChange(e.target.value)}
        SelectProps={{ native: true, inputProps: { title: 'TTS Voice' } }}
      >
        <option value=""></option>
        {options.map(o => (
          <option key={o.key} value={o.key}>{o.label}</option>
        ))}
      </TextField>
      <Button variant="outlined" onClick={handleTest}>Test</Button>
    </Box>
  )
}
