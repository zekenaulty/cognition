import React from 'react';
import { Select, MenuItem, FormControl, InputLabel } from '@mui/material';

export type Provider = { id: string; name: string; displayName?: string };
export type Model = { id: string; name: string; displayName?: string };
export type ProviderModelPickerProps = {
  providers: Provider[];
  models: Model[];
  providerId: string;
  modelId: string;
  onProviderChange: (id: string) => void;
  onModelChange: (id: string) => void;
};

export function ProviderModelPicker({ providers, models, providerId, modelId, onProviderChange, onModelChange }: ProviderModelPickerProps) {
  return (
    <FormControl fullWidth size="small" sx={{ mb: 2 }}>
      <InputLabel>Provider</InputLabel>
      <Select value={providerId} label="Provider" onChange={e => onProviderChange(e.target.value as string)}>
        {providers.map(p => (
          <MenuItem key={p.id} value={p.id}>{p.displayName || p.name}</MenuItem>
        ))}
      </Select>
      <InputLabel>Model</InputLabel>
      <Select value={modelId} label="Model" onChange={e => onModelChange(e.target.value as string)}>
        {models.map(m => (
          <MenuItem key={m.id} value={m.id}>{m.displayName || m.name}</MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}
