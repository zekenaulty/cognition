import { useCallback, useEffect, useState } from 'react';
import { ApiError, diagnosticsApi } from '../api/client';
import { OpenSearchDiagnosticsReport, PlannerHealthReport } from '../types/diagnostics';

type PlannerTelemetryState = {
  plannerReport: PlannerHealthReport | null;
  openSearchReport: OpenSearchDiagnosticsReport | null;
  loading: boolean;
  refreshing: boolean;
  error: string | null;
  refresh: () => Promise<void>;
};

export function usePlannerTelemetryData(token?: string | null): PlannerTelemetryState {
  const [plannerReport, setPlannerReport] = useState<PlannerHealthReport | null>(null);
  const [openSearchReport, setOpenSearchReport] = useState<OpenSearchDiagnosticsReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadReports = useCallback(
    async (isRefresh: boolean) => {
      if (!token) {
        return;
      }
      if (isRefresh) {
        setRefreshing(true);
      } else {
        setLoading(true);
      }
      setError(null);
      try {
        const [planner, search] = await Promise.all([
          diagnosticsApi.plannerHealth(token),
          diagnosticsApi.openSearch(token),
        ]);
        setPlannerReport(planner);
        setOpenSearchReport(search);
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Unable to load diagnostics.';
        setError(message);
      } finally {
        if (isRefresh) {
          setRefreshing(false);
        } else {
          setLoading(false);
        }
      }
    },
    [token]
  );

  useEffect(() => {
    loadReports(false);
  }, [loadReports]);

  return {
    plannerReport,
    openSearchReport,
    loading,
    refreshing,
    error,
    refresh: () => loadReports(true),
  };
}
