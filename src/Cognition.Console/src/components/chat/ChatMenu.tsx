import React, { useState } from 'react';
import { IconButton, Popover, MenuList, MenuItem, Divider, ListItemIcon, ListItemText } from '@mui/material';
import SettingsIcon from '@mui/icons-material/Settings';

export type ChatMenuProps = {
  personas: { id: string; name: string }[];
  personaId: string;
  onPersonaChange: (id: string) => void;
  providers: { id: string; name: string; displayName?: string }[];
  models: { id: string; name: string; displayName?: string }[];
  providerId: string;
  modelId: string;
  onProviderChange: (id: string) => void;
  onModelChange: (id: string) => void;
  imgStyles: { id: string; name: string }[];
  imgStyleId: string;
  onImgStyleChange: (id: string) => void;
  imgModel: string;
  onImgModelChange: (id: string) => void;
  imgMsgCount: number;
  onImgMsgCountChange: (n: number) => void;
  imgPending: boolean;
  onGenerateImage: () => void;
  conversations: { id: string; title?: string | null }[];
  conversationId: string;
  onConversationChange: (id: string) => void;
  onNewConversation: () => void;
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
          <MenuItem onClick={e => { setSubMenu('persona'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><SettingsIcon /></ListItemIcon>
            <ListItemText>Persona</ListItemText>
          </MenuItem>
          <MenuItem onClick={e => { setSubMenu('provider'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><SettingsIcon /></ListItemIcon>
            <ListItemText>Provider & Model</ListItemText>
          </MenuItem>
          <MenuItem onClick={e => { setSubMenu('images'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><SettingsIcon /></ListItemIcon>
            <ListItemText>Images</ListItemText>
          </MenuItem>
          <MenuItem onClick={e => { setSubMenu('conversation'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><SettingsIcon /></ListItemIcon>
            <ListItemText>Conversation</ListItemText>
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
          {subMenu === 'persona' && props.personas.map(p => (
            <MenuItem key={p.id} selected={props.personaId === p.id} onClick={() => { props.onPersonaChange(p.id); setSubMenu(null); setSubMenuAnchor(null); }}>{p.name}</MenuItem>
          ))}
          {subMenu === 'provider' && (
            <React.Fragment>
              {props.providers.map(pr => (
                <MenuItem key={pr.id} selected={props.providerId === pr.id} onClick={() => { props.onProviderChange(pr.id); }}>{pr.displayName || pr.name}</MenuItem>
              ))}
              {props.models.length > 0 && <Divider />}
              {props.models.map(m => (
                <MenuItem key={m.id} selected={props.modelId === m.id} onClick={() => { props.onModelChange(m.id); setSubMenu(null); setSubMenuAnchor(null); }}>{m.displayName || m.name}</MenuItem>
              ))}
            </React.Fragment>
          )}
          {subMenu === 'images' && (
            <React.Fragment>
              {/* Generate at top */}
              <MenuItem key="img-generate" disabled={props.imgPending} onClick={() => { props.onGenerateImage(); setSubMenu(null); setSubMenuAnchor(null); }}>{props.imgPending ? 'Generating…' : 'Generate Image'}</MenuItem>
              <Divider />
              {props.imgStyles.map(s => (
                <MenuItem key={s.id} selected={props.imgStyleId === s.id} onClick={() => { props.onImgStyleChange(s.id); setSubMenu(null); setSubMenuAnchor(null); }}>{s.name}</MenuItem>
              ))}
              <Divider />
              {[6, 12, 24].map((n) => (
                <MenuItem key={n} selected={props.imgMsgCount === n} onClick={() => { props.onImgMsgCountChange(n); setSubMenu(null); setSubMenuAnchor(null); }}>{n} images</MenuItem>
              ))}
            </React.Fragment>
          )}
          {subMenu === 'conversation' && (
            <React.Fragment>
              {props.conversations.map(c => (
                <MenuItem key={c.id} selected={props.conversationId === c.id} onClick={() => { props.onConversationChange(c.id); setSubMenu(null); setSubMenuAnchor(null); }}>{c.title || `Conversation ${c.id}`}</MenuItem>
              ))}
              <Divider />
              <MenuItem key="new" onClick={() => { props.onNewConversation(); setSubMenu(null); setSubMenuAnchor(null); }}>+ New Conversation</MenuItem>
            </React.Fragment>
          )}
          {/* Removed invalid spread/mapping for image count and conversation menus. Use only valid fragments and mapping above. */}
        </MenuList>
      </Popover>
    </>
  );
}
