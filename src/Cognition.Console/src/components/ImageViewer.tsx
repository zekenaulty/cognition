import React from 'react'
import { Dialog, DialogContent, DialogTitle, IconButton } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'

type Props = {
  open: boolean
  onClose: () => void
  imageId?: string
  title?: string
  prompt?: string
}

export default function ImageViewer({ open, onClose, imageId, title, prompt }: Props) {
  const src = imageId ? `/api/images/content?id=${imageId}` : undefined
  const [expanded, setExpanded] = React.useState(false)
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="lg">
      <DialogTitle sx={{ pr: 6 }}>
        {title || 'Image'}
        <IconButton onClick={onClose} size="small" sx={{ position: 'absolute', right: 8, top: 8 }}>
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        {/* Collapsible prompt preview */}
        {prompt && (
          <div style={{ marginBottom: 8 }}>
            <div style={{
              maxHeight: expanded ? 'none' : '2.6em',
              overflow: 'hidden',
              color: 'rgba(255,255,255,0.75)'
            }}>
              {prompt}
            </div>
            <div>
              <button onClick={() => setExpanded(e => !e)} style={{ background: 'none', border: 'none', color: '#90caf9', cursor: 'pointer', padding: 0 }}>
                {expanded ? 'Show less' : 'Show more'}
              </button>
            </div>
          </div>
        )}
        {src && (
          <img alt={title || 'image'} src={src} style={{ maxWidth: '100%', height: 'auto', display: 'block', margin: '0 auto' }} />
        )}
      </DialogContent>
    </Dialog>
  )
}
