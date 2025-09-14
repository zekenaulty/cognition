import { Alert, Box, Button, Card, CardContent, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { api } from '../api/client'

export default function ChangePasswordPage() {
  const { auth } = useAuth()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState(false)

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault()
    setOk(false)
    setError(null)
    if (newPassword !== confirmPassword) {
      setError('Passwords do not match')
      return
    }
    try {
      await api.changePassword(auth!.userId, currentPassword, newPassword, auth!.accessToken)
      setOk(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err: any) {
      setError(err.message || 'Failed to change password')
    }
  }

  return (
    <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
      <Card sx={{ width: 480 }}>
        <CardContent>
          <Typography variant="h5" gutterBottom>Change Password</Typography>
          {ok && <Alert severity="success" sx={{ mb: 2 }}>Password changed</Alert>}
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <form onSubmit={onSubmit}>
            <Stack spacing={2}>
              <TextField label="Current password" type="password" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} fullWidth />
              <TextField label="New password" type="password" value={newPassword} onChange={e => setNewPassword(e.target.value)} fullWidth />
              <TextField label="Confirm password" type="password" value={confirmPassword} onChange={e => setConfirmPassword(e.target.value)} fullWidth />
              <Button variant="contained" type="submit">Update</Button>
            </Stack>
          </form>
        </CardContent>
      </Card>
    </Box>
  )
}
