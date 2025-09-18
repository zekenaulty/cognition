import { useMemo, useState } from 'react'
import { Button, Chip, FormControl, InputLabel, MenuItem, Select, Stack, TextField, Tooltip, Typography } from '@mui/material'
import type { Style } from '../../pages/ImageLabPage'

type Props = {
  accessToken: string
  personaId: string
  styles: Style[]
  onOpenViewer: (id: string, title?: string) => void
}

export default function GenerateTab(props: Props) {
  const { accessToken, personaId, styles, onOpenViewer } = props
  const [styleId, setStyleId] = useState<string>('')
  const [prompt, setPrompt] = useState<string>('')
  const [model, setModel] = useState<string>('dall-e-3')
  const [size, setSize] = useState<string>('1024x1024')
  const [w, setW] = useState<number>(1024)
  const [h, setH] = useState<number>(1024)
  const [pending, setPending] = useState(false)

  const selectedStyle = useMemo(() => styles.find(s => s.id === styleId), [styles, styleId])

  const canGenerate = !!personaId && prompt.trim().length > 0 && !pending

  const handleSizeChange = (val: string) => {
    setSize(val)
    const [sw, sh] = val.split('x').map(v => parseInt(v, 10))
    if (!isNaN(sw) && !isNaN(sh)) { setW(sw); setH(sh) }
  }

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
          Provider: 'OpenAI',
          Model: model || 'dall-e-3',
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
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel id="mod">Image Model</InputLabel>
          <Select labelId="mod" label="Image Model" value={model} onChange={e => setModel(String(e.target.value))}>
            <MenuItem value="dall-e-3">DALL·E 3</MenuItem>
          </Select>
        </FormControl>
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
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel id="size">Size</InputLabel>
          <Select labelId="size" label="Size" value={size} onChange={e => handleSizeChange(String(e.target.value))}>
            <MenuItem value="1024x1024">1024 x 1024 (Square)</MenuItem>
            <MenuItem value="1792x1024">1792 x 1024 (Wide)</MenuItem>
            <MenuItem value="1024x1792">1024 x 1792 (Tall)</MenuItem>
          </Select>
        </FormControl>
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
        <Tooltip title={!personaId ? 'Choose a persona' : (prompt.trim().length === 0 ? 'Write a prompt' : '')}>
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
