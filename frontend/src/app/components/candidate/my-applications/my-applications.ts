import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MyApplication } from '../../../models/application';
import { Applications } from '../../../services/applications';
import { Auth } from '../../../services/auth';

export type ApplicationStatusTone =
  | 'applied'
  | 'under-review'
  | 'shortlisted'
  | 'interview'
  | 'accepted'
  | 'rejected'
  | 'unknown';

@Component({
  selector: 'app-my-applications',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './my-applications.html',
  styleUrl: './my-applications.css',
})
export class MyApplications implements OnInit {
  private readonly applicationsService = inject(Applications);
  private readonly auth = inject(Auth);

  readonly rows = signal<MyApplication[]>([]);
  readonly loading = signal(false);
  readonly message = signal('');

  ngOnInit(): void {
    const token = this.auth.getToken();
    if (!token) {
      this.message.set('Please login to view your applications');
      return;
    }

    if (this.auth.sessionRoleId() !== this.auth.candidateRoleId) {
      this.message.set('This page is for candidates only.');
      return;
    }

    this.loading.set(true);
    this.message.set('');

    this.applicationsService.getMyApplications().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success || !res.data) {
          this.message.set(res.message ?? 'Could not load applications.');
          return;
        }
        this.rows.set(res.data);
        if (res.data.length === 0) {
          this.message.set('You have not applied to any jobs yet.');
        }
      },
      error: (error) => {
        this.loading.set(false);
        this.message.set('Failed to load applications');
        console.error('Failed to load applications', error);
      },
    });
  }

  //this function translates text to tone
  resolveApplicationStatusTone(
    status: string | null | undefined
  ): ApplicationStatusTone {
    switch (status?.trim()) {
      case 'Applied':
        return 'applied';
      case 'Under Review':
        return 'under-review';
      case 'Shortlisted':
        return 'shortlisted';
      case 'Interview':
        return 'interview';
      case 'Accepted':
        return 'accepted';
      case 'Rejected':
        return 'rejected';
      default:
        return 'unknown';
    }
  }
  //builds the class string for html file
  applicationStatusBadgeClass(status: string | null | undefined): string {
    const tone = this.resolveApplicationStatusTone(status);
    return `my-apps-status-badge my-apps-status-badge--${tone}`;
  }
}
