import React from 'react'
import { Dialog, DialogContent, DialogTitle, IconButton } from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'

type Props = {
  open: boolean
  onClose: () => void
  imageId?: string
  title?: string
}

export default function ImageViewer({ open, onClose, imageId, title }: Props) {
  const src = imageId ? `/api/images/content?id=${imageId}` : undefined
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="lg">
      <DialogTitle sx={{ pr: 6 }}>
        {title || 'Image'}
        <IconButton onClick={onClose} size="small" sx={{ position: 'absolute', right: 8, top: 8 }}>
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        {src && (
          <img alt={title || 'image'} src={src} style={{ maxWidth: '100%', height: 'auto', display: 'block', margin: '0 auto' }} />
        )}
      </DialogContent>
    </Dialog>
  )
}

