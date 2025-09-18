import { AppBar, Box, Container, IconButton, Toolbar, Typography, Link, Stack, Button } from '@mui/material'
import React, { useState } from 'react'
import MenuIcon from '@mui/icons-material/Menu'
import { BrowserRouter, Route, Routes, useNavigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { useSecurity } from './hooks/useSecurity'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ChangePasswordPage from './pages/ChangePasswordPage'
import ProfilePage from './pages/ProfilePage'
import ProtectedRoute from './routes/ProtectedRoute'
import AccountMenu from './components/AccountMenu'
import PersonasPage from './pages/PersonasPage'
import ImageLabPage from './pages/ImageLabPage'
import ChatPage from './pages/ChatPage'

import { Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Divider, Accordion, AccordionSummary, AccordionDetails, Tooltip } from '@mui/material'
import HomeIcon from '@mui/icons-material/Home'
import ImageIcon from '@mui/icons-material/Image'
import WorkIcon from '@mui/icons-material/Work'
import ApiIcon from '@mui/icons-material/Api'
import DescriptionIcon from '@mui/icons-material/Description'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import AddIcon from '@mui/icons-material/Add'
import ChatIcon from '@mui/icons-material/Chat'
import DeleteIcon from '@mui/icons-material/Delete'
import { request } from './api/client'
import { PrimaryDrawer } from './components/navigation/PrimaryDrawer'
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material'

function Shell({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, auth } = useAuth()
  const security = useSecurity()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const handleDrawerOpen = () => setDrawerOpen(true)
  const handleDrawerClose = () => setDrawerOpen(false)
  const navigate = useNavigate()
  const [personas, setPersonas] = useState<Array<{ id: string; name: string }>>([])
  const [expandedId, setExpandedId] = useState<string | false>(false)
  const [convsByPersona, setConvsByPersona] = useState<Record<string, Array<{ id: string; title?: string | null }>>>({})
  const [recent, setRecent] = useState<Array<{ id: string; title?: string | null; createdAtUtc?: string }>>([])

  async function loadPersonas() {
    try {
      const list = await request<Array<{ id: string; name: string }>>('/api/personas', {}, auth?.accessToken)
      setPersonas(list)
    } catch {}
  }
  async function loadConversations(pid: string) {
    try {
      const list = await request<Array<{ id: string; title?: string | null }>>(`/api/conversations?participantId=${pid}`, {}, auth?.accessToken)
      setConvsByPersona(prev => ({ ...prev, [pid]: list }))
    } catch {}
  }
  async function loadRecent() {
    try {
      const list = await request<Array<{ id: string; title?: string | null; createdAtUtc?: string }>>(`/api/conversations`, {}, auth?.accessToken)
      setRecent((list || []).slice(0, 5))
    } catch {}
  }
  const handleAccordion = (pid: string) => (_: any, expanded: boolean) => {
    setExpandedId(expanded ? pid : false)
    if (expanded && !convsByPersona[pid]) loadConversations(pid)
  }
  React.useEffect(() => { if (isAuthenticated) { loadPersonas(); loadRecent(); } }, [isAuthenticated])

  async function openRecentConversation(convId: string) {
    try {
      // Try to infer personaId from messages (prefer assistant role)
      const msgs = await request<Array<{ fromPersonaId?: string; FromPersonaId?: string; role: any }>>(`/api/conversations/${convId}/messages`, {}, auth?.accessToken)
      let pid = ''
      const normalizeRole = (r: any) => {
        if (r === 1 || r === '1' || r === 'user' || r === 'User') return 'user'
        if (r === 2 || r === '2' || r === 'assistant' || r === 'Assistant') return 'assistant'
        if (r === 0 || r === '0' || r === 'system' || r === 'System') return 'system'
        const n = Number(r); if (!Number.isNaN(n)) return n === 2 ? 'assistant' : (n === 0 ? 'system' : 'user')
        return 'user'
      }
      const assistant = (msgs || []).find(m => normalizeRole((m as any).role) === 'assistant')
      if (assistant) pid = String((assistant as any).fromPersonaId ?? (assistant as any).FromPersonaId ?? '')
      if (!pid) {
        // fallback to saved persona
        try { pid = localStorage.getItem('cognition.chat.personaId') || '' } catch {}
      }
      if (!pid && personas.length > 0) pid = personas[0].id
      if (pid) {
        navigate(`/chat/${pid}/${convId}`)
        setDrawerOpen(false)
      }
    } catch {
      // fallback: do nothing
    }
  }
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
        <Typography variant="caption">© {new Date().getFullYear()} Cognition</Typography>
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
                <Route path="/chat/:personaId" element={<ChatPage />} />
                <Route path="/chat/:personaId/:conversationId" element={<ChatPage />} />
                <Route path="/image-lab" element={<ImageLabPage />} />
                <Route path="/change-password" element={<ChangePasswordPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/personas" element={<PersonasPage />} />
              </Route>
            </Routes>
          </Shell>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )
}



