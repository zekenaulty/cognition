import React from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  LinearProgress,
  List,
  ListItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography
} from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import { BacklogActionLog, FictionBacklogItem } from '../../types/fiction';

type Props = {
  items?: FictionBacklogItem[] | null;
  loading: boolean;
  error?: string | null;
  placeholder?: string;
  onResume?: (item: FictionBacklogItem) => void;
  resumingId?: string | null;
  isAdmin?: boolean;
  actionLogs?: BacklogActionLog[] | null;
  actionLoading?: boolean;
  actionError?: string | null;
};

export function FictionBacklogPanel({
  items,
  loading,
  error,
  placeholder = 'Select a plan to inspect its backlog.',
  onResume,
  resumingId,
  isAdmin = false,
  actionLogs = [],
  actionLoading = false,
  actionError = null
}: Props) {
  if (loading) {
    return <LinearProgress />;
  }

  if (error) {
    return <Alert severity="warning">{error}</Alert>;
  }

  const backlogItems = items ?? [];

  const renderTable = () => {
    if (backlogItems.length === 0) {
      return (
        <Typography variant="body2" color="text.secondary">
          {placeholder}
        </Typography>
      );
    }

    return (
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Backlog Item</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Task</TableCell>
            <TableCell>Updated</TableCell>
            {isAdmin && onResume ? <TableCell align="right">Actions</TableCell> : null}
          </TableRow>
        </TableHead>
        <TableBody>
          {backlogItems.map(item => {
            const canResume =
              Boolean(onResume) &&
              isAdmin &&
              item.status !== 'Complete' &&
              item.taskId &&
              item.conversationPlanId &&
              item.conversationId;

            const resumeButton = canResume ? (
              <Button
                variant="contained"
                size="small"
                startIcon={<PlayArrowIcon />}
                disabled={resumingId === item.id}
                onClick={() => onResume?.(item)}
              >
                Resume
              </Button>
            ) : (
              <Tooltip title="Resume requires conversation + task metadata." placement="left">
                <span>
                  <Button variant="outlined" size="small" disabled startIcon={<PlayArrowIcon />}>
                    Resume
                  </Button>
                </span>
              </Tooltip>
            );

            return (
              <TableRow key={item.id} hover>
                <TableCell>
                  <Stack spacing={0.5}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {item.description || item.backlogId}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {item.backlogId}
                    </Typography>
                    {item.branchSlug && (
                      <Typography variant="caption" color="text.secondary">
                        Branch: {item.branchSlug}
                      </Typography>
                    )}
                  </Stack>
                </TableCell>
                <TableCell>
                  <StatusChip status={item.status} />
                </TableCell>
                <TableCell>
                  {item.taskId ? (
                    <Stack spacing={0.5}>
                      <Typography variant="body2">
                        {item.toolName || 'Planner'} - Step {item.stepNumber ?? '--'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Task {item.taskId}
                      </Typography>
                      {item.taskStatus && (
                        <Typography variant="caption" color="text.secondary">
                          Last run: {item.taskStatus}
                        </Typography>
                      )}
                    </Stack>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      —
                    </Typography>
                  )}
                </TableCell>
                <TableCell>
                  <Typography variant="body2">{formatRelative(item.updatedAtUtc ?? item.createdAtUtc)}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    Created {formatRelative(item.createdAtUtc)}
                  </Typography>
                </TableCell>
                {isAdmin && onResume ? (
                  <TableCell align="right">
                    {canResume ? resumeButton : <>{resumeButton}</>}
                  </TableCell>
                ) : null}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    );
  };

  const renderActionLog = () => {
    if (actionLoading) {
      return <LinearProgress sx={{ mt: 1 }} />;
    }

    if (actionError) {
      return (
        <Alert severity="warning" sx={{ mt: 1 }}>
          {actionError}
        </Alert>
      );
    }

    if (!actionLogs || actionLogs.length === 0) {
      return (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
          No recent backlog actions recorded.
        </Typography>
      );
    }

    return (
      <List dense sx={{ mt: 1 }}>
        {actionLogs.map(log => (
          <ListItem key={`${log.backlogId}-${log.timestampUtc}-${log.action}`}>
            <Stack spacing={0.25}>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {log.action} - {log.backlogId}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {log.actor ?? 'Unknown actor'} via {log.source} - {formatRelative(log.timestampUtc)}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Branch {log.branch}
                {log.status ? ` - Status ${log.status}` : ''}
              </Typography>
            </Stack>
          </ListItem>
        ))}
      </List>
    );
  };

  return (
    <Stack spacing={3}>
      <Box sx={{ overflowX: 'auto' }}>{renderTable()}</Box>
      <Box>
        <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
          Action Log
        </Typography>
        {renderActionLog()}
      </Box>
    </Stack>
  );
}

function StatusChip({ status }: { status: string }) {
  const normalized = (status || '').toLowerCase();
  let color: 'default' | 'primary' | 'success' | 'warning' | 'error' = 'default';
  if (normalized === 'pending' || normalized === '0') {
    color = 'warning';
  } else if (normalized === 'inprogress' || normalized === 'in_progress' || normalized === '1') {
    color = 'primary';
  } else if (normalized === 'complete' || normalized === '2') {
    color = 'success';
  } else if (normalized === 'failed' || normalized === 'error') {
    color = 'error';
  }

  return <Chip size="small" color={color} label={formatStatus(status)} />;
}

function formatStatus(value?: string | number | null) {
  if (value === null || value === undefined) return 'Unknown';
  const str = value.toString();
  if (!str) return 'Unknown';
  switch (str.toLowerCase()) {
    case '0':
    case 'pending':
      return 'Pending';
    case '1':
    case 'inprogress':
    case 'in_progress':
      return 'In Progress';
    case '2':
    case 'complete':
      return 'Complete';
    default:
      return str.charAt(0).toUpperCase() + str.slice(1);
  }
}

function formatRelative(value?: string | null) {
  if (!value) return '--';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  const diff = Date.now() - date.getTime();
  const ahead = diff < 0;
  const minutes = Math.floor(Math.abs(diff) / 60000);
  if (minutes < 1) return ahead ? 'in moments' : 'just now';
  if (minutes < 60) return `${minutes}m ${ahead ? 'from now' : 'ago'}`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ${minutes % 60}m ${ahead ? 'from now' : 'ago'}`;
  const days = Math.floor(hours / 24);
  return `${days}d ${hours % 24}h ${ahead ? 'from now' : 'ago'}`;
}
