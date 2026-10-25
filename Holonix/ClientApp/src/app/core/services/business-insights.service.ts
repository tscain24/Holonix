import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
export interface InsightSummary { jobs: number; bookedValue: number; completedValue: number; averageValue: number; completedJobs: number; cancelledJobs: number; upcomingValue: number; }
export interface InsightEmployee { employeeId: number; name: string; jobs: number; bookedValue: number; completedValue: number; completedJobs: number; cancelledJobs: number; }
export interface InsightGroup { employeeId?: number | null; key: string; jobs: number; jobValue: number; averageValue: number; completedJobs: number; completedValue: number; cancelledJobs: number; }
export interface InsightJob { jobId: number; name: string; customer: string; start: string; end: string; completedAt: string | null; status: string; value: number; paymentStatus: string; employees: string[]; services: string[]; }
export interface InsightsReport { employees: InsightEmployee[]; summary: InsightSummary; groups: InsightGroup[]; jobs: InsightJob[]; totalJobs: number; page: number; pageSize: number; }
export interface InsightsFilters { start: string; end: string; groupBy: string; status: string; customer: string; exactCustomer: boolean; service: string; employeeId: number | null; minValue: number | null; maxValue: number | null; }
@Injectable({ providedIn: 'root' })
export class BusinessInsightsService {
  constructor(private http: HttpClient) {}
  private params(filters: InsightsFilters, page: number): HttpParams {
    let params = new HttpParams().set('page', page).set('pageSize', 25);
    Object.entries(filters).forEach(([key, value]) => { if (value !== null && value !== '') params = params.set(key, String(value)); });
    return params;
  }
  report(code: string, filters: InsightsFilters, page: number) {
    return this.http.get<InsightsReport>(`/api/business/${encodeURIComponent(code)}/insights`, { params: this.params(filters, page) });
  }
  export(code: string, filters: InsightsFilters) {
    return this.http.get(`/api/business/${encodeURIComponent(code)}/insights/export`, { params: this.params(filters, 1), responseType: 'blob' });
  }
}
