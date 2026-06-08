import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobsService } from '../../../services/jobs';
import { Job } from '../../../models/job';
import { Router } from '@angular/router';

@Component({
  selector: 'app-recruiter-my-jobs',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './my-jobs.html',
  styleUrl: './my-jobs.css',
})
export class RecruiterMyJobs implements OnInit {
  private readonly jobsService = inject(JobsService);
  private readonly router = inject(Router);

  loading = signal(false);
  message = signal('');
  jobs = signal<Job[]>([]);
  /** Prevents double-delete; disables row delete button while that job is deleting. */
  deletingJobId = signal<number | null>(null);

  ngOnInit(): void {
    const token = localStorage.getItem('token');
    if (!token) {
      this.message.set('Please login to view recruiter jobs');
      return;
    }
    this.loadPostedJobs();
  }

  private loadPostedJobs(): void {
    this.loading.set(true);
    this.message.set('');
    this.jobsService.getRecruiterPostedJobs().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success || !res.data) {
          this.message.set(res.message ?? 'Could not load jobs.');
          return;
        }
        this.jobs.set(res.data);
      },
      error: () => {
        this.loading.set(false);
        this.message.set('Failed to load recruiter jobs');
      },
    });
  }

  editJob(jobId: number): void {
    void this.router.navigate(['/recruiter/jobs', jobId, 'edit']);
  }

  deleteJob(job: Job): void {
    if (
      !confirm(
        `Delete “${job.title ?? 'this job'}”? This cannot be undone and removes the posting from the job board.`,
      )
    ) {
      return;
    }

    const token = localStorage.getItem('token');
    if (!token) {
      this.message.set('Please login to delete a job');
      return;
    }

    this.message.set('');
    this.deletingJobId.set(job.id);

    this.jobsService.deleteRecruiterJob(job.id).subscribe({
      next: (res) => {
        this.deletingJobId.set(null);
        this.message.set(res.message);
        if (res.success) {
          this.jobs.update((list) => list.filter((j) => j.id !== job.id));
        }
      },
      error: () => {
        this.deletingJobId.set(null);
        this.message.set('Failed to delete job');
      },
    });
  }
}
