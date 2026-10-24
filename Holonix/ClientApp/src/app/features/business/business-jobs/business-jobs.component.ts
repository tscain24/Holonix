import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { BusinessService, BusinessWorkspace, BusinessWorkspaceJob } from '../../../core/services/business.service';

type AssignmentAvailability = { state: 'available' | 'busy' | 'unavailable'; label: string };

@Component({ selector: 'app-business-jobs', templateUrl: './business-jobs.component.html', styleUrls: ['./business-jobs.component.css'], encapsulation: ViewEncapsulation.None })
export class BusinessJobsComponent implements OnInit {
  workspace: BusinessWorkspace | null = null;
  loading = true;
  filter: 'all' | 'upcoming' | 'completed' = 'all';
  searchTerm = '';
  busy = new Set<number>();
  selectedAssigneeIds: Record<number, number[]> = {};
  assignmentOpenJobId: number | null = null;
  assignmentSearch = '';
  loadingAssignmentAvailability = false;
  assignmentAvailability: Record<number, AssignmentAvailability> = {};
  constructor(private readonly route: ActivatedRoute, private readonly router: Router, private readonly businessService: BusinessService, private readonly snackBar: MatSnackBar) {}
  ngOnInit(): void { const code = this.route.snapshot.paramMap.get('businessCode')?.trim(); if (!code) { this.router.navigate(['/business']); return; } this.load(code); }
  get code(): string { return this.workspace?.businessCode ?? this.route.snapshot.paramMap.get('businessCode')?.trim() ?? ''; }
  get jobs(): BusinessWorkspaceJob[] { const jobs = this.workspace?.jobs ?? []; const now = Date.now(); const scoped = this.filter === 'completed' ? jobs.filter(j => j.status === 'Completed') : this.filter === 'upcoming' ? jobs.filter(j => j.status !== 'Completed' && this.parseJobWallClock(j.endDateTime).getTime() >= now) : jobs; const query = this.searchTerm.trim().toLowerCase(); return query ? scoped.filter(j => `${j.name} ${j.customerName ?? ''} ${j.assignedEmployeeName ?? ''} ${j.status}`.toLowerCase().includes(query)) : scoped; }
  get upcomingCount(): number { return (this.workspace?.jobs ?? []).filter(j => j.status !== 'Completed' && this.parseJobWallClock(j.endDateTime).getTime() >= Date.now()).length; }
  get completedCount(): number { return (this.workspace?.jobs ?? []).filter(j => j.status === 'Completed').length; }
  get inProgressCount(): number { return Math.max((this.workspace?.jobs.length ?? 0) - this.upcomingCount - this.completedCount, 0); }
  get unassignedCount(): number { return (this.workspace?.jobs ?? []).filter(job => !job.assignedEmployeeName && job.status !== 'Completed').length; }
  get employees(): BusinessWorkspace['employees'] { return (this.workspace?.employees ?? []).filter(e => e.isActive); }
  get filteredAssignmentEmployees(): BusinessWorkspace['employees'] { const query = this.assignmentSearch.trim().toLowerCase(); return this.employees.filter(employee => !query || employee.displayName.toLowerCase().includes(query)); }
  get todayJobs(): BusinessWorkspaceJob[] { const today = new Date().toDateString(); return (this.workspace?.jobs ?? []).filter(job => this.parseJobWallClock(job.startDateTime).toDateString() === today); }
  formatJobDate(value: string): string { return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(this.parseJobWallClock(value)); }
  formatJobShortDate(value: string): string { return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' }).format(this.parseJobWallClock(value)); }
  formatJobTime(value: string): string { return new Intl.DateTimeFormat('en-US', { hour: 'numeric', minute: '2-digit' }).format(this.parseJobWallClock(value)); }
  complete(job: BusinessWorkspaceJob): void { if (this.busy.has(job.jobId) || !this.code) return; this.busy.add(job.jobId); this.businessService.completeJob(this.code, job.jobId).pipe(finalize(() => this.busy.delete(job.jobId))).subscribe({ next: r => { this.snackBar.open(r.message, 'Close', { duration: 3000, panelClass: ['snack-success'] }); this.load(this.code); }, error: e => this.snackBar.open(e?.error?.errors?.[0] ?? 'Could not complete this job.', 'Close', { duration: 3500, panelClass: ['snack-error'] }) }); }
  reassign(job: BusinessWorkspaceJob): void { const ids = this.selectedAssigneeIds[job.jobId] ?? []; if (!ids.length || this.busy.has(job.jobId) || !this.code) return; this.busy.add(job.jobId); this.businessService.reassignJob(this.code, job.jobId, ids).pipe(finalize(() => this.busy.delete(job.jobId))).subscribe({ next: r => { this.assignmentOpenJobId = null; this.snackBar.open(r.message, 'Close', { duration: 3000, panelClass: ['snack-success'] }); this.load(this.code); }, error: e => this.snackBar.open(e?.error?.errors?.[0] ?? 'Could not update the assigned team.', 'Close', { duration: 3500, panelClass: ['snack-error'] }) }); }
  openAssignment(job: BusinessWorkspaceJob): void {
    if (!this.workspace?.canManageJobRequests) return;
    const opening = this.assignmentOpenJobId !== job.jobId;
    this.assignmentOpenJobId = opening ? job.jobId : null;
    this.assignmentSearch = '';
    this.assignmentAvailability = {};
    if (opening) { this.selectedAssigneeIds[job.jobId] = this.currentAssignmentIds(job); this.loadAssignmentAvailability(job); }
  }
  toggleAssignee(job: BusinessWorkspaceJob, businessUserId: number): void { const selected = this.selectedAssigneeIds[job.jobId] ?? []; this.selectedAssigneeIds[job.jobId] = selected.includes(businessUserId) ? selected.filter(id => id !== businessUserId) : [...selected, businessUserId]; }
  isAssigneeSelected(job: BusinessWorkspaceJob, businessUserId: number): boolean { return (this.selectedAssigneeIds[job.jobId] ?? []).includes(businessUserId); }
  assignedTeamCount(job: BusinessWorkspaceJob): number { return (job.assignedEmployees ?? []).length || (job.assignedEmployeeName ? 1 : 0); }
  assignedTeamLabel(job: BusinessWorkspaceJob): string { const names = (job.assignedEmployees ?? []).map(employee => employee.displayName); return names.length ? names.join(', ') : job.assignedEmployeeName || 'Unassigned'; }
  profileImageUrl(value?: string | null): string { const raw = value?.trim(); return !raw ? '' : raw.startsWith('data:image') ? raw : `data:image/*;base64,${raw}`; }
  availabilityFor(businessUserId: number): AssignmentAvailability { return this.assignmentAvailability[businessUserId] ?? { state: 'unavailable', label: 'Checking availability' }; }
  canAssignEmployee(businessUserId: number): boolean { return this.availabilityFor(businessUserId).state === 'available'; }
  goOverview(): void { this.router.navigate(['/workspace/overview', this.code]); }
  private load(code: string): void { this.loading = true; this.businessService.getBusinessWorkspace(code).pipe(finalize(() => this.loading = false)).subscribe({ next: w => this.workspace = w, error: () => this.snackBar.open('Could not load jobs.', 'Close', { duration: 3500, panelClass: ['snack-error'] }) }); }

  private loadAssignmentAvailability(job: BusinessWorkspaceJob): void {
    const start = this.parseJobWallClock(job.startDateTime);
    const dateIso = `${start.getFullYear()}-${String(start.getMonth() + 1).padStart(2, '0')}-${String(start.getDate()).padStart(2, '0')}`;
    this.loadingAssignmentAvailability = true;
    this.businessService.getBusinessTeamDailyAvailability(this.code, dateIso)
      .pipe(finalize(() => this.loadingAssignmentAvailability = false))
      .subscribe({
        next: response => {
          const startMinutes = start.getHours() * 60 + start.getMinutes();
          const end = this.parseJobWallClock(job.endDateTime);
          const endMinutes = end.getHours() * 60 + end.getMinutes();
          const availability: Record<number, AssignmentAvailability> = {};
          for (const employee of this.employees) {
            const member = response.members.find(item => item.businessUserId === employee.businessUserId);
            if (!member || member.isDayOff || !member.isAvailable) {
              availability[employee.businessUserId] = { state: 'unavailable', label: member?.isDayOff ? 'Day off' : 'Unavailable' };
              continue;
            }
            const coversJob = member.timeFrames.some(frame => this.timeToMinutes(frame.startTime) <= startMinutes && this.timeToMinutes(frame.endTime) >= endMinutes);
            if (!coversJob) {
              availability[employee.businessUserId] = { state: 'unavailable', label: 'Outside working hours' };
              continue;
            }
            const hasConflict = (this.workspace?.jobs ?? []).some(other => other.jobId !== job.jobId && (other.assignedEmployees ?? []).some(assignment => assignment.businessUserId === employee.businessUserId) && this.parseJobWallClock(other.startDateTime) < end && this.parseJobWallClock(other.endDateTime) > start && other.status !== 'Completed');
            availability[employee.businessUserId] = hasConflict ? { state: 'busy', label: 'Busy at this time' } : { state: 'available', label: 'Available' };
          }
          this.assignmentAvailability = availability;
        },
        error: () => {
          this.assignmentAvailability = Object.fromEntries(this.employees.map(employee => [employee.businessUserId, { state: 'unavailable', label: 'Availability unavailable' }]));
        },
      });
  }

  private timeToMinutes(value: string): number {
    const [hours, minutes] = value.split(':').map(Number);
    return hours * 60 + minutes;
  }

  private currentAssignmentIds(job: BusinessWorkspaceJob): number[] {
    const assignedIds = (job.assignedEmployees ?? []).map(employee => employee.businessUserId);
    if (assignedIds.length || !job.assignedEmployeeName) return assignedIds;
    return this.employees
      .filter(employee => employee.displayName === job.assignedEmployeeName)
      .map(employee => employee.businessUserId);
  }

  private parseJobWallClock(value: string): Date {
    const match = (value ?? '').match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?/);
    if (!match) return new Date(value);
    return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), Number(match[4]), Number(match[5]), Number(match[6] ?? 0));
  }
}
