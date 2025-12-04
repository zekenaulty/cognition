import { Avatar, IconButton, Menu, MenuItem, ListItemIcon, Tooltip } from '@mui/material'
import AccountCircle from '@mui/icons-material/AccountCircle'
import LogoutIcon from '@mui/icons-material/Logout'
import LockIcon from '@mui/icons-material/Lock'
import BadgeIcon from '@mui/icons-material/Badge'
import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { useNavigate } from 'react-router-dom'

export default function AccountMenu() {
  const { auth, logout } = useAuth()
  const [anchor, setAnchor] = useState<null | HTMLElement>(null)
  const open = Boolean(anchor)
  const navigate = useNavigate()


  return (
    <>
      <Tooltip title={auth?.username || 'Account'}>
        <IconButton color="inherit" onClick={(e) => setAnchor(e.currentTarget)} size="small" sx={{ ml: 1 }}>
          <Avatar sx={{ width: 32, height: 32 }}><AccountCircle /></Avatar>
        </IconButton>
      </Tooltip>
      <Menu anchorEl={anchor} open={open} onClose={() => setAnchor(null)}>
        <MenuItem onClick={() => { setAnchor(null); navigate('/profile') }}>
          <ListItemIcon><BadgeIcon fontSize="small" /></ListItemIcon>
          Profile
        </MenuItem>
        <MenuItem onClick={() => { setAnchor(null); navigate('/personas') }}>
          <ListItemIcon><BadgeIcon fontSize="small" /></ListItemIcon>
          Personas
        </MenuItem>
        <MenuItem onClick={() => { setAnchor(null); navigate('/change-password') }}>
          <ListItemIcon><LockIcon fontSize="small" /></ListItemIcon>
          Change password
        </MenuItem>
        <MenuItem onClick={() => { setAnchor(null); logout(); navigate('/login') }}>
          <ListItemIcon><LogoutIcon fontSize="small" /></ListItemIcon>
          Logout
        </MenuItem>
      </Menu>
    </>
  )
}
