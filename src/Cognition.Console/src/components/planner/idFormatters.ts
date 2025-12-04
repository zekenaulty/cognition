export function formatIdFragment(value: string) {
  const trimmed = value.replace(/-/g, '');
  return trimmed.length <= 8 ? trimmed : trimmed.slice(0, 8);
}
