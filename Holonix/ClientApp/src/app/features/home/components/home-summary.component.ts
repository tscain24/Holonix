import { Component, Input, OnInit } from '@angular/core';

interface UsageMonth {
  month: string;
  searches: number;
  leads: number;
  bookings: number;
  saves: number;
}

@Component({
  selector: 'app-home-summary',
  templateUrl: './home-summary.component.html',
  styleUrls: ['./home-summary.component.css'],
})
export class HomeSummaryComponent implements OnInit {
  @Input() firstName = 'User';

  usageMonths: UsageMonth[] = [];
  totalSearches = 0;
  totalLeads = 0;
  totalBookings = 0;
  totalSaves = 0;
  avgMonthlySearches = 0;
  bookingRate = 0;
  maxActivity = 1;

  ngOnInit(): void {
    this.usageMonths = this.buildUsageMonths();
    this.totalSearches = this.usageMonths.reduce((sum, month) => sum + month.searches, 0);
    this.totalLeads = this.usageMonths.reduce((sum, month) => sum + month.leads, 0);
    this.totalBookings = this.usageMonths.reduce((sum, month) => sum + month.bookings, 0);
    this.totalSaves = this.usageMonths.reduce((sum, month) => sum + month.saves, 0);
    this.avgMonthlySearches = Math.round(this.totalSearches / this.usageMonths.length);
    this.bookingRate = this.totalSearches > 0
      ? Math.round((this.totalBookings / this.totalSearches) * 100)
      : 0;
    this.maxActivity = Math.max(...this.usageMonths.map((month) => this.activityValue(month)), 1);
  }

  activityValue(month: UsageMonth): number {
    return month.searches + month.leads * 2 + month.bookings * 4 + month.saves;
  }

  activityPercent(month: UsageMonth): number {
    return Math.max(18, Math.round((this.activityValue(month) / this.maxActivity) * 100));
  }

  private buildUsageMonths(): UsageMonth[] {
    const profile = [
      { searches: 18, leads: 6, bookings: 2, saves: 4 },
      { searches: 24, leads: 8, bookings: 3, saves: 5 },
      { searches: 29, leads: 10, bookings: 4, saves: 6 },
      { searches: 33, leads: 12, bookings: 4, saves: 8 },
      { searches: 39, leads: 14, bookings: 5, saves: 9 },
      { searches: 44, leads: 17, bookings: 6, saves: 11 },
    ];

    const formatter = new Intl.DateTimeFormat('en-US', { month: 'short' });

    return profile.map((value, index) => {
      const offset = 5 - index;
      const monthDate = new Date();
      monthDate.setMonth(monthDate.getMonth() - offset);
      return {
        month: formatter.format(monthDate),
        ...value,
      };
    });
  }
}
