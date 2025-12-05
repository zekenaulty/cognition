import { AppBar, Box, Container, IconButton, Toolbar, Typography, Link, Stack, Button } from '@mui/material'
import React, { useState } from 'react'
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
import ImageLabPage from './pages/ImageLabPage'
import ChatPage from './pages/ChatPage'
import AgentsPage from './pages/AgentsPage'
import AgentDetailPage from './pages/AgentDetailPage'
import PlannerTelemetryPage from './pages/PlannerTelemetryPage'
import FictionProjectsPage from './pages/FictionProjectsPage'
import AdminLlmDefaultsPage from './pages/AdminLlmDefaultsPage'

import { PrimaryDrawer } from './components/navigation/PrimaryDrawer'
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material'

function Shell({ children }: { children: React.ReactNode }) {

  const { isAuthenticated } = useAuth()

  const [drawerOpen, setDrawerOpen] = useState(false)

  const handleDrawerOpen = () => setDrawerOpen(true)

  const handleDrawerClose = () => setDrawerOpen(false)

  return (

    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>

      <AppBar position="static" color="transparent" enableColorOnDark>

        <Toolbar>

          <IconButton size="large" edge="start" color="inherit" aria-label="menu" sx={{ mr: 2 }} onClick={handleDrawerOpen}>

            <MenuIcon />

          </IconButton>

          <Typography variant="h6" sx={{ flexGrow: 1 }}>

            <Link href="/" color="inherit" underline="none">Cognition Console</Link>

          </Typography>

          <Stack direction="row" spacing={1} alignItems="center">

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

      <PrimaryDrawer open={drawerOpen} onClose={handleDrawerClose} />

      <Container sx={{ flex: 1, py: 4 }}>{children}</Container>

      <Box component="footer" sx={{ py: 3, textAlign: 'center', color: 'text.secondary' }}>

        <Typography variant="caption">(c) {new Date().getFullYear()} Cognition</Typography>

      </Box>

    </Box>

  )

}

function Home() { return null }

export default function App() {
  const theme = React.useMemo(() => createTheme({
    palette: { mode: 'dark', background: { paper: '#0b0c10', default: '#0a0b0e' } },
    components: {
      MuiPopover: { styleOverrides: { paper: { backgroundColor: '#0b0c10' } } },
      MuiMenu: { styleOverrides: { paper: { backgroundColor: '#0b0c10' } } },
      MuiDrawer: { styleOverrides: { paper: { backgroundColor: '#0b0c10' } } },
      MuiTooltip: { styleOverrides: { tooltip: { backgroundColor: '#0b0c10' } } },
    }
  }), [])

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AuthProvider>
        <BrowserRouter>
          <Shell>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route element={<ProtectedRoute />}>
                <Route path="/" element={<Home />} />
                <Route path="/chat/:agentId" element={<ChatPage />} />
                <Route path="/chat/:agentId/:conversationId" element={<ChatPage />} />
                <Route path="/image-lab" element={<ImageLabPage />} />
                <Route path="/change-password" element={<ChangePasswordPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/personas" element={<PersonasPage />} />
                <Route path="/agents" element={<AgentsPage />} />
                <Route path="/agents/:agentId" element={<AgentDetailPage />} />
                <Route path="/operations/backlog" element={<PlannerTelemetryPage />} />
                <Route path="/fiction/projects" element={<FictionProjectsPage />} />
                <Route path="/admin/llm-defaults" element={<AdminLlmDefaultsPage />} />
              </Route>
            </Routes>
          </Shell>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )
}

