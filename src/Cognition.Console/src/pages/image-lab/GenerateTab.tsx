import { useMemo, useState } from 'react'
import { Button, Chip, FormControl, InputLabel, MenuItem, Select, Stack, TextField, Tooltip, Typography } from '@mui/material'
import type { Style } from '../ImageLabPage'

type Props = {
  accessToken: string
  personaId: string
  providerId: string
  providerName: string
  imageModel: string
  styles: Style[]
  onOpenViewer: (id: string, title?: string) => void
}

export default function GenerateTab(props: Props) {
  const { accessToken, personaId, providerId, providerName, imageModel, styles, onOpenViewer } = props
  const [styleId, setStyleId] = useState<string>('')
  const [prompt, setPrompt] = useState<string>('')
  const [w, setW] = useState<number>(1024)
  const [h, setH] = useState<number>(1024)
  const [pending, setPending] = useState(false)

  const selectedStyle = useMemo(() => styles.find(s => s.id === styleId), [styles, styleId])

  const canGenerate = !!personaId && !!providerId && !!imageModel && prompt.trim().length > 0 && !pending

  const doGenerate = async () => {
    if (!canGenerate) return
    setPending(true)
    try {
      const fullPrompt = `${selectedStyle?.promptPrefix ? selectedStyle.promptPrefix + '\n' : ''}${prompt}`
      const res = await fetch('/api/images/generate', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {})
        },
        body: JSON.stringify({
          ConversationId: null,
          PersonaId: personaId || null,
          Prompt: fullPrompt,
          Width: w,
          Height: h,
          StyleId: styleId || undefined,
          NegativePrompt: selectedStyle?.negativePrompt || undefined,
          Provider: providerName || 'OpenAI',
          Model: imageModel || 'dall-e-3',
        })
      })
      if (!res.ok) {
        const msg = await res.text()
        throw new Error(msg || 'Image generation failed')
      }
      const data = await res.json()
      const id = String(data.id ?? data.Id)
      onOpenViewer(id, selectedStyle?.name ? `Generated · ${selectedStyle.name}` : 'Generated image')
    } catch (err) {
      console.error(err)
    } finally {
      setPending(false)
    }
  }

  return (
    <Stack spacing={2}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <FormControl size="small" sx={{ minWidth: 240 }}>
          <InputLabel id="sty">Style</InputLabel>
          <Select labelId="sty" label="Style" value={styleId} onChange={e => setStyleId(String(e.target.value))}>
            <MenuItem value=""><em>(No style)</em></MenuItem>
            {styles.map(s => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
          </Select>
        </FormControl>
        {selectedStyle?.name && <Chip size="small" label={selectedStyle.name} variant="outlined" />}
      </Stack>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField label="Width" type="number" size="small" value={w} onChange={e => setW(parseInt(e.target.value || '0', 10) || 0)} sx={{ maxWidth: 160 }} />
        <TextField label="Height" type="number" size="small" value={h} onChange={e => setH(parseInt(e.target.value || '0', 10) || 0)} sx={{ maxWidth: 160 }} />
      </Stack>

      <TextField
        label="Prompt"
        value={prompt}
        onChange={e => setPrompt(e.target.value)}
        minRows={4}
        multiline
        fullWidth
      />

      <Stack direction="row" spacing={2} alignItems="center">
        <Tooltip title={!personaId ? 'Choose a persona' : (!providerId ? 'Choose a provider' : (prompt.trim().length === 0 ? 'Write a prompt' : ''))}>
          <span>
            <Button variant="contained" disabled={!canGenerate} onClick={doGenerate}>
              {pending ? 'Generating…' : 'Generate'}
            </Button>
          </span>
        </Tooltip>
        {!!selectedStyle?.negativePrompt && (
          <Typography variant="caption" color="text.secondary">
            Negative prompt active (from style)
          </Typography>
        )}
      </Stack>
    </Stack>
  )
}
