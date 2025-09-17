import React from 'react';
import { Select, MenuItem, FormControl, InputLabel } from '@mui/material';

export type Persona = { id: string; name: string };
export type PersonaPickerProps = {
  personas: Persona[];
  value: string;
  onChange: (id: string) => void;
};

export function PersonaPicker({ personas, value, onChange }: PersonaPickerProps) {
  return (
    <FormControl fullWidth size="small" sx={{ mb: 2 }}>
      <InputLabel>Persona</InputLabel>
      <Select value={value} label="Persona" onChange={e => onChange(e.target.value as string)}>
        {personas.map(p => (
          <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}
