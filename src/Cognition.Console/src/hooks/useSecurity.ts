import { useMemo } from 'react';
import { useAuth } from '../auth/AuthContext';

export type Role = 'Viewer' | 'User' | 'Administrator';

export function useSecurity() {
  const { auth } = useAuth();
  const role = (auth?.role as Role | undefined) || 'User';

  const isAdmin = role === 'Administrator';
  const isUser = role === 'User' || isAdmin;
  const isViewer = role === 'Viewer';

  const canChat = true; // everyone can chat
  const canCreatePersona = isAdmin || role === 'User';
  const canAccessAdmin = isAdmin;

  function hasRole(required: Role | Role[]) {
    const rr = Array.isArray(required) ? required : [required];
    return rr.includes(role);
  }

  return useMemo(() => ({
    role,
    isAdmin,
    isUser,
    isViewer,
    canChat,
    canCreatePersona,
    canAccessAdmin,
    hasRole,
  }), [role]);
}

