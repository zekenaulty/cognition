import { AppBar, Box, Container, IconButton, Toolbar, Typography, Link, Stack, Button } from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ChangePasswordPage from './pages/ChangePasswordPage'
import ProfilePage from './pages/ProfilePage'
import ProtectedRoute from './routes/ProtectedRoute'
import AccountMenu from './components/AccountMenu'
import PersonasPage from './pages/PersonasPage'
import ChatPage from './pages/ChatPage'

function Shell({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth()
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static" color="transparent" enableColorOnDark>
        <Toolbar>
          <IconButton size="large" edge="start" color="inherit" aria-label="menu" sx={{ mr: 2 }}>
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            Cognition Console
          </Typography>
          <Stack direction="row" spacing={1} alignItems="center">
            <Button color="inherit" component={Link} href="/hangfire" target="_blank" rel="noopener">Jobs</Button>
            <Button color="inherit" component={Link} href="/openapi/v1.json" target="_blank" rel="noopener">API JSON</Button>
            <Button color="inherit" component={Link} href="/swagger" target="_blank" rel="noopener">Swagger</Button>
            {isAuthenticated ? (
              <AccountMenu />
            ) : (
              <>
                <Button color="inherit" component={Link} href="/login">Login</Button>
                <Button color="inherit" component={Link} href="/register">Register</Button>
              </>
            )}
          </Stack>
        </Toolbar>
      </AppBar>
      <Container sx={{ flex: 1, py: 4 }}>{children}</Container>
      <Box component="footer" sx={{ py: 3, textAlign: 'center', color: 'text.secondary' }}>
        <Typography variant="caption">© {new Date().getFullYear()} Cognition</Typography>
      </Box>
    </Box>
  )
}

function Home() {
  return (
    <>
      <Typography variant="h4" gutterBottom>Welcome</Typography>
      <Typography variant="body1" color="text.secondary">
        Vite + React + TypeScript + Material UI (dark mode), with ASP.NET Core backend.
      </Typography>
    </>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Shell>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route element={<ProtectedRoute />}>
              <Route path="/" element={<ChatPage />} />
              <Route path="/chat" element={<ChatPage />} />
              <Route path="/change-password" element={<ChangePasswordPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/personas" element={<PersonasPage />} />
            </Route>
          </Routes>
        </Shell>
      </BrowserRouter>
    </AuthProvider>
  )
}
