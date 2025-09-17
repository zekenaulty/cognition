import React, { useState } from 'react';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import SentimentSatisfiedAltIcon from '@mui/icons-material/SentimentSatisfiedAlt';
import SentimentVerySatisfiedIcon from '@mui/icons-material/SentimentVerySatisfied';
import SentimentSatisfiedIcon from '@mui/icons-material/SentimentSatisfied';
import FavoriteIcon from '@mui/icons-material/Favorite';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import EmojiObjectsIcon from '@mui/icons-material/EmojiObjects';
import VisibilityIcon from '@mui/icons-material/Visibility';
import WhatshotIcon from '@mui/icons-material/Whatshot';
import EmojiEmotionsIcon from '@mui/icons-material/EmojiEmotions';
import SentimentVeryDissatisfiedIcon from '@mui/icons-material/SentimentVeryDissatisfied';
import SentimentDissatisfiedIcon from '@mui/icons-material/SentimentDissatisfied';
import { Box, IconButton, Popover, Tooltip } from '@mui/material';

export const FEEDBACK_ICONS = [
  { key: 'satisfied', icon: <SentimentSatisfiedAltIcon fontSize="medium" />, label: 'Satisfied' },
  { key: 'very_satisfied', icon: <SentimentVerySatisfiedIcon fontSize="medium" />, label: 'Very Satisfied' },
  { key: 'neutral', icon: <SentimentSatisfiedIcon fontSize="medium" />, label: 'Neutral' },
  { key: 'love', icon: <FavoriteIcon fontSize="medium" />, label: 'Love' },
  { key: 'like', icon: <FavoriteBorderIcon fontSize="medium" />, label: 'Like' },
  { key: 'idea', icon: <EmojiObjectsIcon fontSize="medium" />, label: 'Interesting' },
  { key: 'see', icon: <VisibilityIcon fontSize="medium" />, label: 'See' },
  { key: 'hot', icon: <WhatshotIcon fontSize="medium" />, label: 'Hot' },
  { key: 'funny', icon: <EmojiEmotionsIcon fontSize="medium" />, label: 'Funny' },
  { key: 'very_funny', icon: <SentimentVerySatisfiedIcon fontSize="medium" />, label: 'Very Funny' },
  { key: 'dissatisfied', icon: <SentimentDissatisfiedIcon fontSize="medium" />, label: 'Dissatisfied' },
  { key: 'very_dissatisfied', icon: <SentimentVeryDissatisfiedIcon fontSize="medium" />, label: 'Very Dissatisfied' },
];

export type FeedbackBarProps = {
  onRate?: (key: string) => void;
  selected?: string;
};

export function FeedbackBar({ onRate, selected }: FeedbackBarProps) {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);
  const handleOpen = (e: React.MouseEvent<HTMLElement>) => setAnchorEl(e.currentTarget);
  const handleClose = () => setAnchorEl(null);
  const handleSelect = (key: string) => {
    if (onRate) onRate(key);
    handleClose();
  };
  const selectedIcon = FEEDBACK_ICONS.find(f => f.key === selected)?.icon;
  return (
    <>
      <IconButton size="small" onClick={handleOpen} sx={{ p: 0.5 }}>
        {selectedIcon || <HelpOutlineIcon fontSize="medium" />}
      </IconButton>
      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
      >
        <Box sx={{ display: 'flex', gap: 0.5, p: 1 }}>
          {FEEDBACK_ICONS.map(f => (
            <Tooltip key={f.key} title={f.label}>
              <IconButton
                size="small"
                sx={{ p: 0.5, background: selected === f.key ? 'rgba(255,200,0,0.15)' : undefined }}
                onClick={() => handleSelect(f.key)}
              >
                {f.icon}
              </IconButton>
            </Tooltip>
          ))}
        </Box>
      </Popover>
    </>
  );
}
