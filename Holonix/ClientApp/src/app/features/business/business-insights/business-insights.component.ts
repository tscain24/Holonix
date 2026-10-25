import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, Subscription } from 'rxjs';
import { MonthlyJobValue, monthlyJobValues } from './monthly-job-values';
import { BusinessInsightsService, InsightEmployee, InsightGroup, InsightsFilters, InsightsReport } from '../../../core/services/business-insights.service';

@Component({ selector: 'app-business-insights', templateUrl: './business-insights.component.html', styleUrls: ['./business-insights.component.css'] })
export class BusinessInsightsComponent implements OnInit, OnDestroy {
  code = '';
  report: InsightsReport | null = null;
  monthlyValues: MonthlyJobValue[] = [];
  trendStart = '';
  activeMonth: string | null = null;
  monthTooltipLeft = 0;
  monthTooltipTop = 0;

  showMonthTooltip(point: MonthlyJobValue, event: Event): void {
    const element = event.currentTarget as HTMLElement;
    const bounds = element.getBoundingClientRect();
    this.activeMonth = point.key;
    const tooltipWidth = 220;
    const gap = 12;
    const right = bounds.right + gap;
    const left = right + tooltipWidth <= window.innerWidth - gap
      ? right
      : bounds.left - tooltipWidth - gap;
    this.monthTooltipLeft = Math.max(gap, Math.min(left, window.innerWidth - tooltipWidth - gap));
    this.monthTooltipTop = Math.max(gap, Math.min(bounds.top + bounds.height / 2 - 85, window.innerHeight - 190));
  }

  @HostListener('window:scroll')
  @HostListener('window:resize')
  hideMonthTooltip(): void { this.activeMonth = null; }

  loading = false;
  error = '';
  exportError = '';
  exporting = false;
  denied = false;
  page = 1;
  visualization = 'bar';
  metric: 'jobValue' | 'jobs' | 'averageValue' | 'completedJobs' | 'completedValue' | 'cancelledJobs' = 'jobValue';
  showMetrics = false;
  private request?: Subscription;
  private exportRequest?: Subscription;
  filters: InsightsFilters = { start: this.date(new Date(new Date().getFullYear(), new Date().getMonth(), 1)), end: this.date(new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0)), groupBy: 'day', status: '', customer: '', exactCustomer: false, service: '', employeeId: null, minValue: null, maxValue: null };
  applied = { ...this.filters };
  cards = [
    { key: 'bookedValue', title: 'Total booked job value', money: true, visible: true, definition: 'Sum of accepted and legacy job values; cancelled jobs excluded. Not cash collected.' },
    { key: 'completedValue', title: 'Completed job value', money: true, visible: true, definition: 'Job value for jobs with a recorded completion timestamp; cancelled jobs excluded.' },
    { key: 'averageValue', title: 'Average job value', money: true, visible: true, definition: 'Booked job value divided by non-cancelled jobs.' },
    { key: 'completedJobs', title: 'Jobs completed', money: false, visible: true, definition: 'Jobs with a recorded completion timestamp; cancelled jobs excluded.' },
    { key: 'upcomingValue', title: 'Upcoming scheduled value', money: true, visible: false, definition: 'Non-cancelled, incomplete jobs scheduled after the current UTC clock (business timezone is not recorded).' },
    { key: 'cancelledJobs', title: 'Cancelled jobs', money: false, visible: false, definition: 'Jobs linked to cancelled bookings. Their value is excluded from financial totals.' }
  ];
  constructor(private api: BusinessInsightsService, private route: ActivatedRoute) {}
  ngOnInit(): void { this.code = this.route.snapshot.paramMap.get('businessCode') ?? ''; this.run(); }
  ngOnDestroy(): void { this.request?.unsubscribe(); this.exportRequest?.unsubscribe(); }
  private date(d: Date): string { return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; }
  run(page = 1): void {
    this.hideMonthTooltip();
    this.request?.unsubscribe();
    if (page === 1) this.applied = { ...this.filters };

    this.page = page; this.loading = true; this.error = ''; this.report = null;
    const [year, month] = this.applied.end.split('-').map(Number);
    this.trendStart = this.date(new Date(year, month - 12, 1));
    this.request = forkJoin({
      report: this.api.report(this.code, this.applied, page),
      trend: this.api.report(this.code, { ...this.applied, start: this.trendStart, groupBy: 'month' }, 1)
    }).subscribe({
      next: ({ report, trend }) => { this.report = report;
        const options = new Map(this.employeeOptions.map(employee => [employee.employeeId, employee]));
        [...report.employees, ...trend.employees].forEach(employee => {
          if (employee.employeeId !== 0) options.set(employee.employeeId, employee);
        });
        this.employeeOptions = [...options.values()].sort((a, b) => a.name.localeCompare(b.name));
        this.monthlyValues = monthlyJobValues(this.applied.end, trend.groups); this.loading = false; this.denied = false; },
      error: err => { this.loading = false; this.denied = err.status === 403 || err.status === 401; this.error = err.error?.errors?.[0] ?? 'Could not load this report. Please try again.'; }
    });
  }
  monthlyChange(point: MonthlyJobValue, index: number): string {
    if (index === 0) return 'Starting month';
    if (point.change === null) return 'No prior-month value';
    return (point.change > 0 ? '+' : '') + point.change.toFixed(1) + '% vs prior month';
  }
  drillMonth(point: MonthlyJobValue): void {
    const [year, month] = point.key.split('-').map(Number);
    this.filters = { ...this.applied, start: point.key + '-01', end: this.date(new Date(year, month, 0)) };
    if (this.filters.end > this.applied.end) this.filters.end = this.applied.end;
    this.run();
  }
  cardValue(key: string): number { return this.report ? this.report.summary[key as keyof InsightsReport['summary']] : 0; }
  money(value: number): string { return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value); }
  value(group: InsightGroup): number { return group[this.metric]; }
  formatted(group: InsightGroup): string { return ['jobValue', 'averageValue', 'completedValue'].includes(this.metric) ? this.money(this.value(group)) : String(this.value(group)); }
  width(group: InsightGroup): number { const max = Math.max(...(this.report?.groups ?? []).map(g => this.value(g)), 1); return Math.max(0, this.value(group) / max * 100); }
  schedule(value: string): string { return value.slice(0,16).replace('T',' · '); }
  drill(group: InsightGroup): void {
    this.filters = { ...this.applied };
    if (this.applied.groupBy === 'employee') { this.filters.employeeId = group.employeeId ?? null; }
    else if (this.applied.groupBy === 'status') this.filters.status = group.key;
    else if (this.applied.groupBy === 'customer') { this.filters.customer = group.key; this.filters.exactCustomer = true; }
    else {
      const start = this.applied.groupBy === 'year' ? `${group.key}-01-01` : this.applied.groupBy === 'month' ? `${group.key}-01` : group.key;
      const d = new Date(`${start}T12:00:00`);
      const end = this.applied.groupBy === 'year' ? `${group.key}-12-31` : this.applied.groupBy === 'month' ? this.date(new Date(d.getFullYear(), d.getMonth()+1, 0)) : start;
      this.filters.start = start > this.applied.start ? start : this.applied.start;
      this.filters.end = end < this.applied.end ? end : this.applied.end;
    }
    this.run();
  }
  employeeOptions: InsightEmployee[] = [];
  reset(): void { this.filters = { start: this.date(new Date(new Date().getFullYear(), new Date().getMonth(), 1)), end: this.date(new Date(new Date().getFullYear(), new Date().getMonth()+1, 0)), groupBy: 'day', status: '', customer: '', exactCustomer: false, service: '', employeeId: null, minValue: null, maxValue: null }; this.run(); }
  export(): void {
    this.exporting = true; this.exportError = '';
    this.exportRequest = this.api.export(this.code, this.applied).subscribe({ next: blob => {
      const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'holonix-report.csv'; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000); this.exporting = false;
    }, error: () => { this.exporting = false; this.exportError = 'Export failed. Check your access and try again.'; } });
  }
}
