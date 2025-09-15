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

import { Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Divider } from '@mui/material'
import HomeIcon from '@mui/icons-material/Home'
import ImageIcon from '@mui/icons-material/Image'
import WorkIcon from '@mui/icons-material/Work'
import ApiIcon from '@mui/icons-material/Api'
import DescriptionIcon from '@mui/icons-material/Description'

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
            Cognition Console
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
      <Drawer anchor="left" open={drawerOpen} onClose={handleDrawerClose}>
        <Box sx={{ width: 260 }} role="presentation" onClick={handleDrawerClose}>
          <List>
            <ListItem disablePadding>
              <ListItemButton component={Link} href="/">
                <ListItemIcon><HomeIcon /></ListItemIcon>
                <ListItemText primary="Home" />
              </ListItemButton>
            </ListItem>
            {isAuthenticated && (
              <ListItem disablePadding>
                <ListItemButton component={Link} href="/image-lab">
                  <ListItemIcon><ImageIcon /></ListItemIcon>
                  <ListItemText primary="Image Lab" />
                </ListItemButton>
              </ListItem>
            )}
            <ListItem disablePadding>
              <ListItemButton onClick={() => window.open('/hangfire', 'hangfireTab')}>
                <ListItemIcon><WorkIcon /></ListItemIcon>
                <ListItemText primary="Jobs" />
              </ListItemButton>
            </ListItem>
            <ListItem disablePadding>
              <ListItemButton onClick={() => window.open('/openapi/v1.json', 'apiJsonTab')}>
                <ListItemIcon><ApiIcon /></ListItemIcon>
                <ListItemText primary="API JSON" />
              </ListItemButton>
            </ListItem>
            <ListItem disablePadding>
              <ListItemButton onClick={() => window.open('/swagger', 'swaggerTab')}>
                <ListItemIcon><DescriptionIcon /></ListItemIcon>
                <ListItemText primary="Swagger" />
              </ListItemButton>
            </ListItem>
          </List>
          <Divider />
        </Box>
      </Drawer>
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
              <Route path="/image-lab" element={<ImageLabPage />} />
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
