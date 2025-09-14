import { useEffect, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { api } from '../api/client'
import { Alert, Box, Button, Card, CardContent, FormControl, InputLabel, MenuItem, Select, Stack, TextField, Typography } from '@mui/material'
import { PersonaForm, PersonaModel } from '../components/PersonaForm'

export default function ProfilePage() {
  const { auth, setPrimaryPersona } = useAuth()
  const [personas, setPersonas] = useState<Array<{ id: string; name: string }>>([])
  const [saveOk, setSaveOk] = useState(false)
  const [err, setErr] = useState<string | null>(null)
  const [email, setEmail] = useState<string>('')
  const [username, setUsername] = useState<string>('')
  const [userPersona, setUserPersona] = useState<PersonaModel | null>(null)

  useEffect(() => {
    let mounted = true
    api.listPersonas(auth?.accessToken)
      .then(items => { if (mounted) setPersonas(items) })
      .catch(e => setErr(e.message || 'Failed to load personas'))
    // Load user profile via /users/me
    if (auth) {
      api.me(auth.accessToken)
        .then(async u => {
          if (mounted) {
            setEmail(u.email || '')
            setUsername(u.username)
            if (u.primaryPersonaId) {
              const p = await api.getPersona(u.primaryPersonaId, auth.accessToken)
              setUserPersona({
                id: p.id, name: p.name, nickname: p.nickname, role: p.role, gender: p.gender,
                essence: p.essence, beliefs: p.beliefs, background: p.background,
                communicationStyle: p.communicationStyle, emotionalDrivers: p.emotionalDrivers,
                signatureTraits: p.signatureTraits, narrativeThemes: p.narrativeThemes,
                domainExpertise: p.domainExpertise, isPublic: p.isPublic
              })
            }
          }
        })
        .catch(e => setErr(e.message || 'Failed to load profile'))
    }
    return () => { mounted = false }
  }, [auth?.accessToken])

  async function onSaveProfile() {
    try {
      if (!auth) return
      await api.updateUser(auth.userId, { email, username }, auth.accessToken)
      setSaveOk(true)
      setTimeout(() => setSaveOk(false), 2000)
    } catch (e: any) {
      setErr(e.message || 'Failed to save profile')
    }
  }

  async function onSaveUserPersona() {
    try {
      if (!auth || !userPersona?.id) return
      const payload: any = {
        Name: userPersona.name,
        Nickname: userPersona.nickname,
        Role: userPersona.role,
        Gender: userPersona.gender,
        Essence: userPersona.essence,
        Beliefs: userPersona.beliefs,
        Background: userPersona.background,
        CommunicationStyle: userPersona.communicationStyle,
        EmotionalDrivers: userPersona.emotionalDrivers,
        SignatureTraits: userPersona.signatureTraits,
        NarrativeThemes: userPersona.narrativeThemes,
        DomainExpertise: userPersona.domainExpertise,
        IsPublic: userPersona.isPublic
      }
      await api.updatePersona(userPersona.id, payload, auth.accessToken)
      setSaveOk(true)
      setTimeout(() => setSaveOk(false), 2000)
    } catch (e: any) {
      setErr(e.message || 'Failed to save persona')
    }
  }

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
      <Card sx={{ width: 600 }}>
        <CardContent>
          <Typography variant="h5" gutterBottom>Profile</Typography>
          {saveOk && <Alert severity="success" sx={{ mb: 2 }}>Saved</Alert>}
          {err && <Alert severity="error" sx={{ mb: 2 }}>{err}</Alert>}
          <Stack spacing={2}>
            <Typography variant="body1">User ID: <b>{auth?.userId}</b></Typography>
            <TextField label="Username" value={username} onChange={e => setUsername(e.target.value)} sx={{ maxWidth: 360 }} />
            <TextField label="Email" value={email} onChange={e => setEmail(e.target.value)} sx={{ maxWidth: 360 }} />
            <Box>
              <Button variant="contained" onClick={onSaveProfile} sx={{ mr: 1 }}>Save Profile</Button>
            </Box>
            {userPersona && (
              <Box sx={{ mt: 4 }}>
                <Typography variant="h6" gutterBottom>My Persona</Typography>
                <PersonaForm value={userPersona} onChange={setUserPersona} />
                <Box sx={{ mt: 2 }}>
                  <Button variant="contained" onClick={onSaveUserPersona}>Save Persona</Button>
                </Box>
              </Box>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  )
}
