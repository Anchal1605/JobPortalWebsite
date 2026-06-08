import { Component, inject, signal, OnInit } from '@angular/core';
import { Applications } from '../../../services/applications';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Auth } from '../../../services/auth';
import { JobApplication } from '../../../models/application';
import { FormsModule } from '@angular/forms';


@Component({
  selector: 'app-recruiter-applicants',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './applicants.html',
  styleUrl: './applicants.css',
})
export class RecruiterApplicants implements OnInit {
  private readonly applicationsService = inject(Applications);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(Auth);

  readonly applicants = signal<JobApplication[]>([]);
  readonly loading = signal(false);
  readonly message = signal('');
  readonly statusOptions = [
    'Applied',
    'Under Review',
    'Shortlisted',
    'Interview',
    'Rejected',
    'Accepted',
  ] as const;

  readonly updatingId = signal<number | null>(null);

  ngOnInit(): void {
    const routeParams = this.route.snapshot.paramMap;
    const jobId = Number(routeParams.get('jobId'));
    if (isNaN(jobId)) {
      this.message.set('Invalid job ID');
      return;
    }
    const token = this.auth.getToken();
    if (!token) {
      this.message.set('Please login to view applicants');
      return;
    }
    this.loading.set(true);
    this.applicationsService.getJobApplicants(jobId).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success || !res.data) {
          this.message.set(res.message);
          return;
        }
        this.applicants.set(res.data);
        if (res.data.length > 0) {
          this.message.set('Applicants loaded successfully');
        }
        else {
          this.message.set('No applicants yet.');
        }
      },
      error: (error) => {
        this.loading.set(false);
        this.message.set('Failed to load applicants');
        console.error('Failed to load applicants', error);
      }
    });
  }
  onStatusChange(applicationId: number, newStatus: string): void {
    const token = this.auth.getToken();
    if (!token) {
      this.message.set('Please login to update status');
      return;
    }

    this.updatingId.set(applicationId);
    this.applicationsService
      .updateApplicationStatus(applicationId, newStatus)
      .subscribe({
        next: (res) => {
          this.updatingId.set(null);
          if (!res.success) {
            this.message.set(res.message ?? 'Failed to update status');
            return;
          }
          this.applicants.update((list) =>
            list.map((a) =>
              a.id === applicationId ? { ...a, status: newStatus } : a
            )
          );
          this.message.set('Status updated');
        },
        error: () => {
          this.updatingId.set(null);
          this.message.set('Failed to update status');
        },
      });
  }
}
