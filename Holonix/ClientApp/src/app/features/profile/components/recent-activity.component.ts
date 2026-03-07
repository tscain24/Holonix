import { Component, Input, OnInit } from '@angular/core';

interface RecentActivityItem {
  title: string;
  detail: string;
  dateLabel: string;
  accent: 'blue' | 'gold' | 'green';
}

@Component({
  selector: 'app-recent-activity',
  templateUrl: './recent-activity.component.html',
  styleUrls: ['./recent-activity.component.css'],
})
export class RecentActivityComponent implements OnInit {
  @Input() firstName = 'User';

  activities: RecentActivityItem[] = [];

  ngOnInit(): void {
    this.activities = this.buildActivities();
  }

  private buildActivities(): RecentActivityItem[] {
    return [
      {
        title: 'Profile updated',
        detail: `${this.firstName} refreshed account details and profile preferences.`,
        dateLabel: this.formatRelativeDate(0),
        accent: 'blue',
      },
      {
        title: 'Photo changed',
        detail: 'A new profile photo was uploaded and saved to your account.',
        dateLabel: this.formatRelativeDate(2),
        accent: 'gold',
      },
      {
        title: 'Signed in from web',
        detail: 'Your account was accessed from the Holonix web app.',
        dateLabel: this.formatRelativeDate(5),
        accent: 'green',
      },
    ];
  }

  private formatRelativeDate(daysAgo: number): string {
    if (daysAgo === 0) {
      return 'Today';
    }

    if (daysAgo === 1) {
      return 'Yesterday';
    }

    const date = new Date();
    date.setDate(date.getDate() - daysAgo);

    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
    }).format(date);
  }
}
