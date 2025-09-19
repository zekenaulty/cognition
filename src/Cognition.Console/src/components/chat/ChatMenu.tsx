import React, { useState } from 'react';
import { IconButton, Popover, MenuList, MenuItem, Divider, ListItemIcon, ListItemText } from '@mui/material';
import SettingsIcon from '@mui/icons-material/Settings';
import AddIcon from '@mui/icons-material/Add';
import PersonIcon from '@mui/icons-material/Person';
import DnsIcon from '@mui/icons-material/Dns';
import MemoryIcon from '@mui/icons-material/Memory';
import ImageIcon from '@mui/icons-material/Image';
import ForumIcon from '@mui/icons-material/Forum';
import PaletteIcon from '@mui/icons-material/Palette';
import ViewModuleIcon from '@mui/icons-material/ViewModule';
import AutoAwesomeIcon from '@mui/icons-material/AutoAwesome';
import ChatBubbleOutlineIcon from '@mui/icons-material/ChatBubbleOutline';

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
  onGenerateImage: (model: string, count: number) => void;
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
          <MenuItem onClick={() => { props.onNewConversation(); }}>
            <ListItemIcon><AddIcon /></ListItemIcon>
            <ListItemText>New Conversation</ListItemText>
          </MenuItem>
          {/* Persona selection removed from chat menu */}
          <MenuItem onClick={e => { setSubMenu('provider'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><DnsIcon /></ListItemIcon>
            <ListItemText>Provider & Model</ListItemText>
          </MenuItem>
          <MenuItem onClick={e => { setSubMenu('images'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><ImageIcon /></ListItemIcon>
            <ListItemText>Images</ListItemText>
          </MenuItem>
          <MenuItem onClick={e => { setSubMenu('conversation-list'); setSubMenuAnchor(e.currentTarget); }}>
            <ListItemIcon><ForumIcon /></ListItemIcon>
            <ListItemText>Conversations</ListItemText>
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
          {/* Persona submenu removed */}
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
          {subMenu === 'images' && (
            <React.Fragment>
              <MenuItem key="img-generate" disabled={props.imgPending} onClick={() => { props.onGenerateImage(props.imgModel, props.imgMsgCount); }}>
                <ListItemIcon><AutoAwesomeIcon /></ListItemIcon>
                <ListItemText>{props.imgPending ? 'Generating…' : 'Generate Image'}</ListItemText>
              </MenuItem>
              <Divider />
              <MenuItem onClick={e => { setSubMenu('images-style'); setSubMenuAnchor(e.currentTarget); }}>
                <ListItemIcon><PaletteIcon /></ListItemIcon>
                <ListItemText>Style ▶</ListItemText>
              </MenuItem>
              <MenuItem onClick={e => { setSubMenu('images-samples'); setSubMenuAnchor(e.currentTarget); }}>
                <ListItemIcon><ViewModuleIcon /></ListItemIcon>
                <ListItemText>Samples ▶</ListItemText>
              </MenuItem>
            </React.Fragment>
          )}
          {subMenu === 'images-style' && (
            <React.Fragment>
              {props.imgStyles.map(s => (
                <MenuItem key={s.id} selected={props.imgStyleId === s.id} onClick={() => { props.onImgStyleChange(s.id); }}>
                  <ListItemIcon><PaletteIcon /></ListItemIcon>
                  <ListItemText>{s.name}</ListItemText>
                </MenuItem>
              ))}
            </React.Fragment>
          )}
          {subMenu === 'images-samples' && (
            <React.Fragment>
              {[6, 12, 24].map((n) => (
                <MenuItem key={n} selected={props.imgMsgCount === n} onClick={() => { props.onImgMsgCountChange(n); }}>
                  <ListItemIcon><ViewModuleIcon /></ListItemIcon>
                  <ListItemText>{n} images</ListItemText>
                </MenuItem>
              ))}
            </React.Fragment>
          )}
          {subMenu === 'conversation-list' && (
            <React.Fragment>
              {props.conversations.map(c => (
                <MenuItem key={c.id} selected={props.conversationId === c.id} onClick={() => { props.onConversationChange(c.id); setSubMenu(null); setSubMenuAnchor(null); }}>
                  <ListItemIcon><ChatBubbleOutlineIcon /></ListItemIcon>
                  <ListItemText>{(c.title && c.title.trim()) ? c.title : 'New Chat'}</ListItemText>
                </MenuItem>
              ))}
            </React.Fragment>
          )}
          {/* Removed invalid spread/mapping for image count and conversation menus. Use only valid fragments and mapping above. */}
        </MenuList>
      </Popover>
    </>
  );
}
