import { Box, Button, Card, CardContent, Stack, TextField, Typography, Alert, Collapse, Link } from '@mui/material'
import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { ApiError } from '../api/client'

export default function LoginPage() {
  const { login } = useAuth()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [details, setDetails] = useState<string | null>(null)
  const [show, setShow] = useState(false)
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const location = useLocation() as any
  const from = location.state?.from || '/'

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setDetails(null)
    setLoading(true)
    try {
      await login(username, password)
      navigate(from, { replace: true })
    } catch (err: any) {
      if (err instanceof ApiError) {
        const base = err.isNetworkError ? 'Cannot reach API' : `Login failed (HTTP ${err.status})`
        setError(base)
        const info = [
          `URL: ${err.url}`,
          err.bodyText ? `Body: ${err.bodyText.substring(0, 500)}` : undefined,
          typeof err.status === 'number' ? `Status: ${err.status}` : undefined
        ].filter(Boolean).join('\n')
        setDetails(info || null)
      } else {
        setError(err.message || 'Login failed')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
      <Card sx={{ width: 400 }}>
        <CardContent>
          <Typography variant="h5" gutterBottom>Sign in</Typography>
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              <Stack spacing={1}>
                <span>{error}</span>
                {details && (
                  <>
                    <Link component="button" type="button" onClick={() => setShow((v) => !v)} sx={{ fontSize: 12 }}>
                      {show ? 'Hide details' : 'Show details'}
                    </Link>
                    <Collapse in={show}>
                      <Box component="pre" sx={{ m: 0, whiteSpace: 'pre-wrap', fontSize: 12 }}>{details}</Box>
                    </Collapse>
                  </>
                )}
              </Stack>
            </Alert>
          )}
          <form onSubmit={onSubmit}>
            <Stack spacing={2}>
              <TextField label="Username" value={username} onChange={e => setUsername(e.target.value)} autoFocus fullWidth />
              <TextField label="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} fullWidth />
              <Button variant="contained" type="submit" disabled={loading}>Login</Button>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  )
}
