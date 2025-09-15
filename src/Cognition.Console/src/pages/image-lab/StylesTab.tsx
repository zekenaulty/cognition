import { useEffect, useMemo, useState } from 'react'
import {
  Alert, Button, Card, CardContent, Checkbox, Divider, FormControlLabel,
  List, ListItem, ListItemButton, ListItemText, Stack, TextField, Typography
} from '@mui/material'
import type { Style } from '../ImageLabPage'

type Props = {
  accessToken: string
  styles: Style[]
  onUpdate: (s: Style) => void
}

export default function StylesTab({ accessToken, styles, onUpdate }: Props) {
  const [selectedId, setSelectedId] = useState<string>('')
  const [form, setForm] = useState<Style | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [savedOk, setSavedOk] = useState(false)

  // pick first on mount if available
  useEffect(() => {
    if (!selectedId && styles.length) setSelectedId(styles[0].id)
  }, [styles, selectedId])

  useEffect(() => {
    const s = styles.find(x => x.id === selectedId)
    setForm(s ? { ...s } : null)
    setError(null)
    setSavedOk(false)
  }, [selectedId, styles])

  const sorted = useMemo(() => [...styles].sort((a, b) => a.name.localeCompare(b.name)), [styles])

  const canSave = !!form && !!form.name?.trim() && !saving

  const save = async () => {
    if (!form || !canSave) return
    setSaving(true); setError(null); setSavedOk(false)
    try {
      const res = await fetch(`/api/image-styles/${encodeURIComponent(form.id)}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {})
        },
        body: JSON.stringify({
          Id: form.id,
          Name: form.name,
          Description: form.description ?? null,
          PromptPrefix: form.promptPrefix ?? null,
          NegativePrompt: form.negativePrompt ?? null,
          IsActive: form.isActive ?? true
        })
      })
      if (!res.ok) {
        const msg = await res.text()
        throw new Error(msg || 'Failed to save style')
      }
      const updated = await res.json()
      const mapped: Style = {
        id: updated.id ?? updated.Id,
        name: updated.name ?? updated.Name,
        description: updated.description ?? updated.Description,
        promptPrefix: updated.promptPrefix ?? updated.PromptPrefix,
        negativePrompt: updated.negativePrompt ?? updated.NegativePrompt,
        isActive: updated.isActive ?? updated.IsActive ?? true
      }
      onUpdate(mapped)
      setForm(mapped)
      setSavedOk(true)
    } catch (e: any) {
      setError(e?.message || 'Error')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ flex: 1, minHeight: 0 }}>
      {/* Left: list */}
      <Card sx={{ width: { xs: '100%', md: 320 }, height: { xs: 'auto', md: '100%' }, overflow: 'hidden' }} variant="outlined">
        <CardContent sx={{ pb: 0 }}>
          <Typography variant="subtitle1">Styles ({sorted.length})</Typography>
        </CardContent>
        <Divider />
        <List dense sx={{ overflowY: 'auto', maxHeight: { xs: 280, md: 'calc(100% - 56px)' } }}>
          {sorted.map(s => (
            <ListItem key={s.id} disablePadding>
              <ListItemButton selected={s.id === selectedId} onClick={() => setSelectedId(s.id)}>
                <ListItemText
                  primary={s.name}
                  secondary={s.description || (s.isActive === false ? 'Inactive' : undefined)}
                  secondaryTypographyProps={{ color: s.isActive === false ? 'error' : 'text.secondary' }}
                />
              </ListItemButton>
            </ListItem>
          ))}
        </List>
      </Card>

      {/* Right: editor */}
      <Card sx={{ flex: 1 }} variant="outlined">
        <CardContent>
          {!form ? (
            <Typography color="text.secondary">Select a style to view/edit.</Typography>
          ) : (
            <Stack spacing={2}>
              <Typography variant="subtitle1">Edit style</Typography>
              {error && <Alert severity="error">{error}</Alert>}
              {savedOk && <Alert severity="success">Saved.</Alert>}

              <TextField label="Name" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
              <TextField label="Description" value={form.description || ''} onChange={e => setForm({ ...form, description: e.target.value })} />

              <TextField
                label="Prompt prefix"
                value={form.promptPrefix || ''}
                onChange={e => setForm({ ...form, promptPrefix: e.target.value })}
                minRows={3}
                multiline
              />

              <TextField
                label="Negative prompt"
                value={form.negativePrompt || ''}
                onChange={e => setForm({ ...form, negativePrompt: e.target.value })}
                minRows={2}
                multiline
              />

              <FormControlLabel
                control={
                  <Checkbox
                    checked={form.isActive !== false}
                    onChange={e => setForm({ ...form, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />

              <Stack direction="row" spacing={2}>
                <Button variant="contained" disabled={!canSave} onClick={save}>{saving ? 'Saving…' : 'Save changes'}</Button>
                <Button variant="outlined" disabled={saving} onClick={() => setForm(styles.find(s => s.id === selectedId) || null)}>Reset</Button>
              </Stack>
            </Stack>
          )}
        </CardContent>
      </Card>
    </Stack>
  )
}
