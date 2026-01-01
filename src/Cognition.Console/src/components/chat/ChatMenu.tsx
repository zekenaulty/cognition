import React, { useState } from 'react';
import { IconButton, Popover, MenuList, MenuItem, Divider, ListItemIcon, ListItemText } from '@mui/material';
import SettingsIcon from '@mui/icons-material/Settings';
import DnsIcon from '@mui/icons-material/Dns';
import MemoryIcon from '@mui/icons-material/Memory';

export type ChatMenuProps = {
  providers: { id: string; name: string; displayName?: string }[];
  models: { id: string; name: string; displayName?: string }[];
  providerId: string;
  modelId: string;
  onProviderChange: (id: string) => void;
  onModelChange: (id: string) => void;
};

export function ChatMenu(props: ChatMenuProps) {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [subMenu, setSubMenu] = useState<string | null>(null);
  const [subMenuAnchor, setSubMenuAnchor] = useState<null | HTMLElement>(null);

  const open = Boolean(anchorEl);
  const subOpen = Boolean(subMenuAnchor);

  return (
    <>
      <IconButton aria-label="Settings" onClick={e => setAnchorEl(e.currentTarget)} size="large">
        <SettingsIcon />
      </IconButton>
      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={() => { setAnchorEl(null); setSubMenu(null); setSubMenuAnchor(null); }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
      >
        <MenuList>
          <MenuItem onClick={e => { setSubMenu('provider'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><DnsIcon /></ListItemIcon>
            <ListItemText>Provider & Model</ListItemText>
          </MenuItem>
        </MenuList>
      </Popover>
      <Popover
        open={subOpen}
        anchorEl={subMenuAnchor}
        onClose={() => { setSubMenu(null); setSubMenuAnchor(null); }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
      >
        <MenuList>
          {subMenu === 'provider' && (
            <React.Fragment>
              {props.providers.map(pr => (
                <MenuItem key={pr.id} selected={props.providerId === pr.id} onClick={() => { props.onProviderChange(pr.id); }}>
                  <ListItemIcon><DnsIcon /></ListItemIcon>
                  <ListItemText>{pr.displayName || pr.name}</ListItemText>
                </MenuItem>
              ))}
              {props.models.length > 0 && <Divider />}
              {props.models.map(m => (
                <MenuItem key={m.id} selected={props.modelId === m.id} onClick={() => { props.onModelChange(m.id); setSubMenu(null); setSubMenuAnchor(null); }}>
                  <ListItemIcon><MemoryIcon /></ListItemIcon>
                  <ListItemText>{m.displayName || m.name}</ListItemText>
                </MenuItem>
              ))}
            </React.Fragment>
          )}
        </MenuList>
      </Popover>
    </>
  );
}
