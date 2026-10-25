import { InsightGroup } from '../../../core/services/business-insights.service';

export interface MonthlyJobValue {
  key: string;
  label: string;
  value: number;
  change: number | null;
  width: number;
}

/** Twelve calendar months ending at the selected report end date, including empty months. */
export function monthlyJobValues(end: string, groups: InsightGroup[]): MonthlyJobValue[] {
  const [year, month] = end.split('-').map(Number);
  const values = new Map(groups.map(group => [group.key, group.jobValue]));
  const points: MonthlyJobValue[] = [];
  for (let offset = 11; offset >= 0; offset--) {
    const date = new Date(year, month - 1 - offset, 1);
    const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
    const value = values.get(key) ?? 0;
    const previous = points[points.length - 1]?.value;
    points.push({
      key, value, width: 0,
      label: date.toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
      change: previous === undefined ? null : previous === 0 ? (value === 0 ? 0 : null) : (value - previous) / previous * 100,
    });
  }
  const maximum = Math.max(1, ...points.map(point => point.value));
  return points.map(point => ({ ...point, width: point.value / maximum * 100 }));
}
