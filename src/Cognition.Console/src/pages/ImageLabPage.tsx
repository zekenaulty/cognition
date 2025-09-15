import { useEffect, useState } from 'react'
import { Box, Button, Card, CardContent, Divider, FormControl, InputLabel, MenuItem, Select, Stack, Tab, Tabs, TextField, Typography } from '@mui/material'

type Style = { id: string; name: string; description?: string; promptPrefix?: string; negativePrompt?: string }
type Provider = { id: string; name: string; displayName?: string }
type Model = { id: string; name: string; displayName?: string }

export default function ImageLabPage() {
  const [tab, setTab] = useState(0)
  const [styles, setStyles] = useState<Style[]>([])
  const [providers, setProviders] = useState<Provider[]>([])
  const [models, setModels] = useState<Model[]>([])
  const [providerId, setProviderId] = useState('')
  const [modelId, setModelId] = useState('dall-e-3')
  const [styleId, setStyleId] = useState('')
  const [prompt, setPrompt] = useState('')
  const [w, setW] = useState(1024)
  const [h, setH] = useState(1024)
  const [imgId, setImgId] = useState<string | null>(null)

  useEffect(() => { (async () => {
    try { const r = await fetch('/api/image-styles'); if (r.ok) setStyles((await r.json()).map((s: any) => ({ id: s.id ?? s.Id, name: s.name ?? s.Name, description: s.description ?? s.Description, promptPrefix: s.promptPrefix ?? s.PromptPrefix, negativePrompt: s.negativePrompt ?? s.NegativePrompt }))) } catch {}
    try {
      const r = await fetch('/api/llm/providers');
      if (r.ok) {
        const p = (await r.json()).map((x: any) => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setProviders(p)
        if (!providerId && p.length) setProviderId(p[0].id)
      }
    } catch {}
  })() }, [])

  useEffect(() => { (async () => {
    if (!providerId) return
    try {
      const r = await fetch(`/api/llm/providers/${providerId}/models`)
      if (r.ok) {
        const m = (await r.json()).map((x: any) => ({ id: x.id ?? x.Id, name: x.name ?? x.Name, displayName: x.displayName ?? x.DisplayName }))
        setModels(m)
        if (!modelId && m.length) setModelId(m[0].id)
      }
    } catch {}
  })() }, [providerId])

  const doGenerate = async () => {
    const style = styles.find(s => s.id === styleId)
    const fullPrompt = `${style?.promptPrefix ? style.promptPrefix + '\n' : ''}${prompt}`
    const res = await fetch('/api/images/generate', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ConversationId: null, PersonaId: null, Prompt: fullPrompt, Width: w, Height: h, StyleId: styleId || undefined, NegativePrompt: style?.negativePrompt || undefined, Provider: providers.find(p => p.id === providerId)?.name || 'OpenAI', Model: modelId || 'dall-e-3' }) })
    if (res.ok) { const b = await res.json(); setImgId(b.id ?? b.Id) }
  }

  return (
    <Stack spacing={2} sx={{ height: 'calc(100vh - 160px)' }}>
      <Typography variant="h5">Image Lab</Typography>
      <Card variant="outlined" sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
        <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, height: '100%' }}>
          <Tabs value={tab} onChange={(_, v) => setTab(v)}>
            <Tab label="Generate" />
            <Tab label="Styles" />
            <Tab label="Gallery" />
          </Tabs>
          {tab === 0 && (
            <Stack spacing={2} sx={{ flex: 1, minHeight: 0 }}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <InputLabel id="prov">Provider</InputLabel>
                  <Select labelId="prov" label="Provider" value={providerId} onChange={e => setProviderId(e.target.value)}>
                    {providers.map(p => <MenuItem key={p.id} value={p.id}>{p.displayName || p.name}</MenuItem>)}
                  </Select>
                </FormControl>
                <FormControl size="small" sx={{ minWidth: 200 }}>
                  <InputLabel id="mod">Model</InputLabel>
                  <Select labelId="mod" label="Model" value={modelId} onChange={e => setModelId(e.target.value)}>
                    {models.map(m => <MenuItem key={m.id} value={m.name}>{m.displayName || m.name}</MenuItem>)}
                  </Select>
                </FormControl>
                <FormControl size="small" sx={{ minWidth: 240 }}>
                  <InputLabel id="sty">Style</InputLabel>
                  <Select labelId="sty" label="Style" value={styleId} onChange={e => setStyleId(e.target.value)}>
                    {styles.map(s => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
                  </Select>
                </FormControl>
              </Stack>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField label="Width" type="number" value={w} onChange={e => setW(parseInt(e.target.value || '0')||0)} size="small" sx={{ maxWidth: 120 }} />
                <TextField label="Height" type="number" value={h} onChange={e => setH(parseInt(e.target.value || '0')||0)} size="small" sx={{ maxWidth: 120 }} />
              </Stack>
              <TextField multiline minRows={6} placeholder="Image prompt..." value={prompt} onChange={e => setPrompt(e.target.value)} />
              <Stack direction="row" justifyContent="flex-end">
                <Button variant="contained" onClick={doGenerate}>Generate</Button>
              </Stack>
              <Divider />
              <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto' }}>
                {imgId && <img alt="generated" src={`/api/images/content?id=${imgId}`} style={{ maxWidth: '100%', borderRadius: 8 }} />}
              </Box>
            </Stack>
          )}
          {tab === 1 && (
            <Stack spacing={2}>
              <Typography variant="subtitle1">Styles</Typography>
              <Stack spacing={1}>
                {styles.map(s => (
                  <Box key={s.id}>
                    <Typography variant="subtitle2">{s.name}</Typography>
                    <Typography variant="body2" color="text.secondary">{s.description}</Typography>
                  </Box>
                ))}
              </Stack>
              <Divider />
              <Typography variant="subtitle1">Create Style</Typography>
              <CreateStyle onCreated={(st) => setStyles([st, ...styles])} />
            </Stack>
          )}
          {tab === 2 && (
            <Typography variant="body2" color="text.secondary">Gallery coming next (by persona)</Typography>
          )}
        </CardContent>
      </Card>
    </Stack>
  )
}

function CreateStyle({ onCreated }: { onCreated: (s: Style) => void }) {
  const [name, setName] = useState('')
  const [desc, setDesc] = useState('')
  const [prefix, setPrefix] = useState('')
  const [neg, setNeg] = useState('')
  const canSave = name.trim().length > 0
  const save = async () => {
    const res = await fetch('/api/image-styles', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ Name: name.trim(), Description: desc || null, PromptPrefix: prefix || null, NegativePrompt: neg || null, IsActive: true }) })
    if (!res.ok) return
    const id = (await res.json()).id
    onCreated({ id, name, description: desc, promptPrefix: prefix, negativePrompt: neg })
    setName(''); setDesc(''); setPrefix(''); setNeg('')
  }
  return (
    <Stack spacing={1}>
      <TextField label="Name" value={name} onChange={e => setName(e.target.value)} size="small" />
      <TextField label="Description" value={desc} onChange={e => setDesc(e.target.value)} size="small" />
      <TextField label="Prompt Prefix" value={prefix} onChange={e => setPrefix(e.target.value)} multiline minRows={4} />
      <TextField label="Negative Prompt" value={neg} onChange={e => setNeg(e.target.value)} multiline minRows={2} />
      <Stack direction="row" justifyContent="flex-end"><Button disabled={!canSave} onClick={save} variant="outlined">Save</Button></Stack>
    </Stack>
  )
}
