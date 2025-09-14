import { Alert, Box, Button, Card, CardContent, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'

export default function RegisterPage() {
  const { login } = useAuth()
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState(false)
  const [busy, setBusy] = useState(false)
  const navigate = useNavigate()

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setOk(false)
    if (!username || !password) { setError('Username and password are required'); return }
    if (password !== confirm) { setError('Passwords do not match'); return }
    setBusy(true)
    try {
      await api.register(username, password, email || undefined)
      // auto-login for convenience
      await login(username, password)
      setOk(true)
      navigate('/')
    } catch (err: any) {
      setError(err.message || 'Registration failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
      <Card sx={{ width: 460 }}>
        <CardContent>
          <Typography variant="h5" gutterBottom>Create Account</Typography>
          {ok && <Alert severity="success" sx={{ mb: 2 }}>Registered</Alert>}
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <form onSubmit={onSubmit}>
            <Stack spacing={2}>
              <TextField label="Username" value={username} onChange={e => setUsername(e.target.value)} autoFocus fullWidth />
              <TextField label="Email (optional)" value={email} onChange={e => setEmail(e.target.value)} type="email" fullWidth />
              <TextField label="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} fullWidth />
              <TextField label="Confirm password" type="password" value={confirm} onChange={e => setConfirm(e.target.value)} fullWidth />
              <Button variant="contained" type="submit" disabled={busy}>Register</Button>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  )
}

