import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-workspace-breadcrumb',
  templateUrl: './workspace-breadcrumb.component.html',
  styleUrls: ['./workspace-breadcrumb.component.css'],
})
export class WorkspaceBreadcrumbComponent {
  @Input({ required: true }) businessCode = '';
  @Input({ required: true }) currentLabel = '';

  get isOverview(): boolean {
    return this.currentLabel.toLowerCase() === 'overview';
  }
}
