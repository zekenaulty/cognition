import { useEffect, useMemo, useState } from 'react'
import { CircularProgress, FormControl, Grid, InputLabel, MenuItem, Select, Stack, Typography } from '@mui/material'
import type { Persona, Style } from '../../pages/ImageLabPage'

type GalleryItem = { id: string; prompt?: string; styleName?: string; createdAtUtc?: string }

type Props = {
  accessToken: string
  personas: Persona[]
  personaId: string
  onChangePersona: (id: string) => void
  styles: Style[]
  onOpenViewer: (id: string, title?: string) => void
}

export default function GalleryTab({ accessToken, personas, personaId, onChangePersona, styles, onOpenViewer }: Props) {
  const [items, setItems] = useState<GalleryItem[]>([])
  const [loading, setLoading] = useState(false)
  const [styleFilter, setStyleFilter] = useState<string>('')

  useEffect(() => {
    const load = async () => {
      if (!personaId) { setItems([]); return }
      setLoading(true)
      try {
        const headers = accessToken ? { Authorization: `Bearer ${accessToken}` } as any : undefined
        const r = await fetch(`/api/images/by-persona/${personaId}`, { headers })
        if (r.ok) {
          const list = await r.json()
          const mapped: GalleryItem[] = (list as any[]).map(i => ({
            id: String(i.id ?? i.Id),
            prompt: i.prompt ?? i.Prompt,
            styleName: i.styleName ?? i.StyleName,
            createdAtUtc: i.createdAtUtc ?? i.CreatedAtUtc
          }))
          setItems(mapped)
        }
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [accessToken, personaId])

  const filtered = useMemo(() => {
    if (!styleFilter) return items
    return items.filter(i => (i.styleName || '') === styles.find(s => s.id === styleFilter)?.name)
  }, [items, styleFilter, styles])

  return (
    <Stack spacing={2} sx={{ flex: 1, minHeight: 0 }}>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ flexWrap: 'nowrap' }}>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel id="persona">Persona</InputLabel>
          <Select labelId="persona" label="Persona" value={personaId} onChange={e => onChangePersona(String(e.target.value))}>
            {personas.map(p => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel id="gal-style">Style</InputLabel>
          <Select labelId="gal-style" label="Style" value={styleFilter} onChange={e => setStyleFilter(String(e.target.value))}>
            <MenuItem value=""><em>All</em></MenuItem>
            {styles.map(s => <MenuItem key={s.id} value={s.id}>{s.name}</MenuItem>)}
          </Select>
        </FormControl>
        {loading && <CircularProgress size={18} />}
        <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
          {filtered.length} image{filtered.length === 1 ? '' : 's'}
        </Typography>
      </Stack>

      <Grid container spacing={2}>
        {filtered.map(i => (
          <Grid key={i.id} item xs={12} sm={6} md={4} lg={3}>
            <img
              src={`/api/images/content?id=${i.id}`}
              alt={i.prompt || 'image'}
              style={{ width: '100%', height: 240, objectFit: 'cover', borderRadius: 8, cursor: 'pointer' }}
              onClick={() => onOpenViewer(i.id, i.styleName ? `Style · ${i.styleName}` : undefined)}
            />
            {i.styleName && (
              <Typography variant="caption" color="text.secondary">{i.styleName}</Typography>
            )}
          </Grid>
        ))}
      </Grid>
    </Stack>
  )
}
