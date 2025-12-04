import { useMemo } from 'react';
import { PlannerHealthReport } from '../types/diagnostics';
import { FictionPlanRoster, FictionBacklogItem, PersonaObligation } from '../types/fiction';
import { isObligationOpen, summarizeObligationMetadata } from '../components/fiction/backlogUtils';

type Options = {
  rosterPlanId: string | null;
  plannerReport: PlannerHealthReport | null;
  planBacklogItems: FictionBacklogItem[];
  planObligations: PersonaObligation[];
  planRoster: FictionPlanRoster | null;
};

export function usePlannerFilters({
  rosterPlanId,
  plannerReport,
  planBacklogItems,
  planObligations,
  planRoster
}: Options) {
  const planStaleBacklogItems = useMemo(() => {
    if (!planBacklogItems || planBacklogItems.length === 0) {
      return [];
    }
    const now = Date.now();
    const thresholdMs = 60 * 60 * 1000;
    return planBacklogItems.filter(item => {
      const status = (item.status ?? '').toString().toLowerCase();
      if (status !== 'in_progress') {
        return false;
      }
      const stamp = item.updatedAtUtc ?? item.createdAtUtc;
      if (!stamp) {
        return false;
      }
      const updated = new Date(stamp);
      if (Number.isNaN(updated.getTime())) {
        return false;
      }
      return now - updated.getTime() > thresholdMs;
    });
  }, [planBacklogItems]);

  const planMissingLoreRequirements = useMemo(
    () => planRoster?.loreRequirements?.filter(req => (req.status ?? '').toLowerCase() !== 'ready') ?? [],
    [planRoster]
  );

  const planDriftObligations = useMemo(
    () => planObligations.filter(obligation => summarizeObligationMetadata(obligation.metadata).voiceDrift),
    [planObligations]
  );

  const planAgingObligations = useMemo(() => {
    const thresholdMs = 72 * 60 * 60 * 1000;
    return planObligations.filter(obligation => {
      if (!obligation.createdAtUtc) return false;
      const created = new Date(obligation.createdAtUtc);
      if (Number.isNaN(created.getTime())) return false;
      return Date.now() - created.getTime() > thresholdMs;
    });
  }, [planObligations]);

  const planOpenObligations = useMemo(
    () => planObligations.filter(obligation => isObligationOpen(obligation.status)),
    [planObligations]
  );

  const resolvedObligationCount = useMemo(
    () => planObligations.filter(obligation => (obligation.status ?? '').toString().toLowerCase() === 'resolved').length,
    [planObligations]
  );

  const dismissedObligationCount = useMemo(
    () => planObligations.filter(obligation => (obligation.status ?? '').toString().toLowerCase() === 'dismissed').length,
    [planObligations]
  );

  const contractTelemetryEvents = useMemo(() => {
    if (!plannerReport) {
      return [];
    }
    return (plannerReport.backlog.telemetryEvents ?? []).filter(
      evt => (evt.status ?? '').toString().toLowerCase() === 'contract'
    );
  }, [plannerReport]);

  const planContractEvents = useMemo(() => {
    if (!rosterPlanId) {
      return [];
    }
    return contractTelemetryEvents.filter(evt => evt.planId === rosterPlanId);
  }, [rosterPlanId, contractTelemetryEvents]);

  const planAlertCount =
    planStaleBacklogItems.length +
    planMissingLoreRequirements.length +
    planOpenObligations.length +
    planDriftObligations.length +
    planAgingObligations.length +
    planContractEvents.length;

  return {
    planStaleBacklogItems,
    planMissingLoreRequirements,
    planDriftObligations,
    planAgingObligations,
    planOpenObligations,
    resolvedObligationCount,
    dismissedObligationCount,
    planContractEvents,
    planAlertCount,
  };
}
