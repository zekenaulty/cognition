import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert, Box, Button, Card, CardContent, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import EditIcon from '@mui/icons-material/Edit'
import DeleteIcon from '@mui/icons-material/Delete'
import { PersonaForm, PersonaModel } from '../components/PersonaForm'

export default function PersonasPage() {
  const { auth } = useAuth()
  const [items, setItems] = useState<Array<{ id: string; name: string; type?: 'User'|'Assistant' }>>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<PersonaModel | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const list = await api.listPersonas(auth?.accessToken)
      setItems(list.filter(p => p.type !== 'User'))
    } catch (e: any) {
      setError(e.message || 'Failed to load personas')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [auth?.accessToken])

  async function onEdit(id: string) {
    try {
      const p = await api.getPersona(id, auth?.accessToken)
      setEditing({
        id: p.id,
        name: p.name,
        nickname: p.nickname,
        role: p.role,
        gender: p.gender,
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
    } catch (e: any) { setError(e.message || 'Failed to delete') }
  }

  async function onSave() {
    if (!editing) return
    const payload: any = {
      Name: editing.name,
      Nickname: editing.nickname,
      Role: editing.role,
      Gender: editing.gender,
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
    try {
      if (editing.id) await api.updatePersona(editing.id, payload, auth?.accessToken)
      else await api.createPersona(payload, auth?.accessToken)
      setOpen(false)
      setEditing(null)
      await load()
    } catch (e: any) { setError(e.message || 'Failed to save') }
  }

  return (
    <Box>
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 2 }}>
        <Typography variant="h5">Personas</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={onAdd}>New</Button>
      </Stack>
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      <Card>
        <CardContent>
          {loading ? (
            <Typography>Loading...</Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {items.map(p => (
                  <TableRow key={p.id} hover>
                    <TableCell>{p.name}</TableCell>
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
