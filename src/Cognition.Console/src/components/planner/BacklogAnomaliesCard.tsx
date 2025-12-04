import React from 'react';
import { Card, CardContent, CardHeader, Divider, List, ListItem, ListItemText, Typography } from '@mui/material';
import { PlannerHealthBacklogItem, PlannerHealthReport } from '../../types/diagnostics';
import { formatDuration, formatRelativeTime } from './timeFormatters';
import { describeBacklogStatus } from './backlogFormatters';

type Props = {
  plannerReport: PlannerHealthReport;
};

export function BacklogAnomaliesCard({ plannerReport }: Props) {
  return (
    <Card>
      <CardHeader title="Stale & Orphaned Items" />
      <CardContent>
        <BacklogAnomalyList
          title="Stale Items"
          items={plannerReport.backlog.staleItems}
          emptyMessage="Backlog SLOs look good."
        />
        <Divider sx={{ my: 2 }} />
        <BacklogAnomalyList
          title="Orphaned Items"
          items={plannerReport.backlog.orphanedItems}
          emptyMessage="No orphaned backlog items detected."
        />
      </CardContent>
    </Card>
  );
}

function BacklogAnomalyList({
  title,
  items,
  emptyMessage
}: {
  title: string;
  items: PlannerHealthBacklogItem[];
  emptyMessage: string;
}) {
  return (
    <>
      <Typography variant="subtitle2" color="text.secondary">
        {title}
      </Typography>
      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {emptyMessage}
        </Typography>
      ) : (
        <List dense>
          {items.map(item => {
            const freshness = item.staleDuration
              ? formatDuration(item.staleDuration)
              : formatRelativeTime(item.updatedAtUtc);
            return (
              <ListItem key={`${item.planId}-${item.backlogId}`} alignItems="flex-start">
                <ListItemText
                  primary={
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {item.planName} · {item.backlogId}
                    </Typography>
                  }
                  secondary={
                    <Typography variant="caption" color="text.secondary" component="span">
                      {item.description}
                      <br />
                      {describeBacklogStatus(item.status)} · {freshness}
                    </Typography>
                  }
                />
              </ListItem>
            );
          })}
        </List>
      )}
    </>
  );
}
