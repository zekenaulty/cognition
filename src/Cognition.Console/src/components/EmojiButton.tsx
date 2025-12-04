import React, { useMemo, useState } from 'react'
import { Box, IconButton, Popover, Tab, Tabs, TextField, Tooltip } from '@mui/material'
import EmojiEmotionsIcon from '@mui/icons-material/EmojiEmotions'
// emoji-mart v5
// @ts-ignore - types shipped separately
import Picker from '@emoji-mart/react'
// @ts-ignore
import data from '@emoji-mart/data'

type Props = {
  onInsert: (text: string) => void
  onCloseFocus?: () => void
}

const KAOMOJI = [
  '≽^•⩊•^≼', '(＾▽＾)', '(￣▽￣)ノ', '(｡•̀ᴗ-)✧', '(╯°□°）╯︵ ┻━┻', '┬─┬ ノ( ゜-゜ノ)',
  '(•‿•)', '(ᵔ◡ᵔ)', '(╯︵╰,)', '(¬_¬ )', '(•̀ω•́ )σ', '(✿◠‿◠)', '(ﾉ◕ヮ◕)ﾉ*:･ﾟ✧',
  '(ง •̀_•́)ง', '(づ｡◕‿‿◕｡)づ', '^_^', '¯\\_(ツ)_/¯', '(>‿◠)✌', '(ಥ﹏ಥ)', '(◕‿◕)'
]

export default function EmojiButton({ onInsert, onCloseFocus }: Props) {
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null)
  const [tab, setTab] = useState(0)
  const [q, setQ] = useState('')

  const open = Boolean(anchorEl)

  const handleOpen = (e: React.MouseEvent<HTMLElement>) => setAnchorEl(e.currentTarget)
  const handleClose = () => { setAnchorEl(null); onCloseFocus && onCloseFocus() }

  const filteredKaomoji = useMemo(() => {
    const t = q.trim().toLowerCase()
    if (!t) return KAOMOJI
    return KAOMOJI.filter(k => k.toLowerCase().includes(t))
  }, [q])

  return (
    <>
      <Tooltip title="Insert emoji">
        <IconButton onClick={handleOpen} size="small" aria-label="emoji-picker">
          <EmojiEmotionsIcon />
        </IconButton>
      </Tooltip>
      <Popover
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{ vertical: 'top', horizontal: 'right' }}
        transformOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        PaperProps={{ sx: { p: 1 } }}
      >
        <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="fullWidth" sx={{ mb: 1 }}>
          <Tab label="Emoji" />
          <Tab label="Kaomoji" />
        </Tabs>
        {tab === 0 && (
          <Box sx={{ width: 320, maxWidth: '90vw' }}>
            {/* @ts-ignore props typing from lib */}
            <Picker data={data} theme="dark" previewPosition="none" onEmojiSelect={(e: any) => onInsert(e.native)} navPosition="top" />
          </Box>
        )}
        {tab === 1 && (
          <Box sx={{ width: 320, maxWidth: '90vw' }}>
            <TextField value={q} onChange={e => setQ(e.target.value)} size="small" placeholder="Search kaomoji" fullWidth sx={{ mb: 1 }} />
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 1, maxHeight: 300, overflowY: 'auto' }}>
              {filteredKaomoji.map((k, i) => (
                <Box key={i} onClick={() => onInsert(k)} sx={{ p: 1, borderRadius: 1, bgcolor: 'action.hover', cursor: 'pointer', textAlign: 'center', userSelect: 'none' }}>{k}</Box>
              ))}
            </Box>
          </Box>
        )}
      </Popover>
    </>
  )
}
