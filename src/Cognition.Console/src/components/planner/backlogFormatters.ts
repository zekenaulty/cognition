export function describeBacklogStatus(status: string | number | undefined) {
  const normalized = typeof status === 'number' ? status.toString() : (status ?? '').toString();
  switch (normalized.toLowerCase()) {
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
      return normalized || 'Unknown';
  }
}
