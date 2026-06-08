import { Component, inject, OnInit, signal } from '@angular/core';
import { JobsService } from '../../services/jobs';
import { Job } from '../../models/job';
import { finalize } from 'rxjs';
import { JobsHeaderComponent } from '../common/jobs-header/jobs-header';
import { JobsContentComponent } from '../common/jobs-content/jobs-content';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [JobsHeaderComponent, JobsContentComponent],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly jobsApi = inject(JobsService);

  protected readonly title = signal('Find the next role');
  protected readonly jobs = signal<Job[]>([]);
  protected readonly jobsLoading = signal(false);
  protected readonly jobsError = signal<string | null>(null);
  protected readonly searchText = signal<string>('');

  ngOnInit(): void {
    this.loadJobs();
  }

  protected loadJobs(): void {
    this.jobsError.set(null);
    this.jobsLoading.set(true);
    this.jobsApi
      .getPublicJobs({ search: this.searchText().trim() || undefined })
      .pipe(finalize(() => this.jobsLoading.set(false)))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.jobs.set(res.data ?? []);
          } else {
            this.jobsError.set(res.message ?? 'Could not load jobs');
          }
        },
        error: () => {
          this.jobsError.set('Could not reach the API (check URL and CORS).');
        },
      });
  }

  protected retryLoadJobs(): void {
    this.loadJobs();
  }

  protected onSearchChange(value: string): void {
    this.searchText.set(value);
    this.loadJobs();
  }
}
