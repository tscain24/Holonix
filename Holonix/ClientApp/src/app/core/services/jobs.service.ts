import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BusinessWorkspaceJobLocation } from './business.service';
export interface JobMember { businessUserId: number; displayName: string; profileImageBase64?: string | null; }
export interface OperationalJob {
  jobId: number; name: string; customerName: string; customerEmail: string | null; customerPhone: string | null;
  startDateTime: string; endDateTime: string; status: string; cost: number | null; paymentStatus: string | null;
  assignedEmployees: JobMember[]; serviceLocation: BusinessWorkspaceJobLocation | null; services: string[];
  completedByName: string | null; completedDateTime: string | null; revision: string;
  canReady: boolean; canStart: boolean; canComplete: boolean; canReassign: boolean; canReschedule: boolean; canCancel: boolean; cancellationRestriction: string | null;
}
export interface JobsSummary { total: number; upcoming: number; inProgress: number; unassigned: number; completed: number; cancelled: number; overdue: number; totalValue: number | null; }
export interface JobsPage { summary: JobsSummary; jobs: OperationalJob[]; total: number; page: number; pageSize: number; today: OperationalJob[]; todayTotal: number; canManage: boolean; canViewFinancials: boolean; canCreate: boolean; businessCode: string; }
export interface JobEmployeeOption extends JobMember { availability: string; canAssign: boolean; isCurrent: boolean; }
export interface JobOptions { employees: JobEmployeeOption[]; services: Array<{ businessSubServiceId: number; name: string; price: number | null; durationMinutes: number; requiresServiceLocation: boolean }>; customers: Array<{ userId: string; name: string }>; }
export interface JobsFilters { search: string; filter: string; start: string; end: string; employeeId: number | null; }
@Injectable({ providedIn: 'root' })
export class JobsService {
  constructor(private http: HttpClient) {}
  private url(code: string) { return `/api/business/${encodeURIComponent(code)}/jobs`; }
  list(code: string, filters: JobsFilters, page: number) {
    let params = new HttpParams().set('page',page).set('pageSize',25);
    Object.entries(filters).forEach(([k,v]) => { if(v !== null && v !== '') params = params.set(k,String(v)); });
    return this.http.get<JobsPage>(this.url(code), { params });
  }
  options(code: string) { return this.http.get<JobOptions>(this.url(code) + '/options'); }
  schedule(code: string, start: string, end: string, page = 1) {
    return this.http.get<JobsPage>(this.url(code), { params: { start, end, overlap: true, page, pageSize: 100 } });
  }
  teamOptions(code: string, id: number) { return this.http.get<JobEmployeeOption[]>(`${this.url(code)}/${id}/team-options`); }
  action(code: string, job: OperationalJob, action: string, body: object = {}) { return this.http.post<{ message: string }>(`${this.url(code)}/${job.jobId}/${action}`, { ...body, revision: job.revision }); }
  create(code: string, body: object) { return this.http.post<{ message: string }>(this.url(code), body); }
}
