import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { JobsService } from '../../../services/jobs';
import { CreateJobRequest, Job } from '../../../models/job';
import { ActivatedRoute } from '@angular/router';
import { Auth } from '../../../services/auth';
import { QuillModule } from 'ngx-quill';
import { SecurityContext } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';


@Component({
  selector: 'app-recruiter-create-job',
  standalone: true,
  imports: [FormsModule, RouterLink, QuillModule],
  templateUrl: './create-job.html',
  styleUrl: './create-job.css',
})
export class RecruiterCreateJob implements OnInit {
  private readonly jobsService = inject(JobsService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly sanitizer = inject(DomSanitizer);

  safeDescription(html: string | null | undefined): string {
    return this.sanitizer.sanitize(SecurityContext.HTML, html ?? '') ?? '';
  }
  readonly jobTypes = ['Full-time', 'Part-time', 'Contract', 'Internship', 'Remote'];

  loading = signal(false);
  fetching = signal(false);
  message = signal('');
  editJobId = signal<number | null>(null);
  editPostedBy = signal<number | null>(null);

  model: CreateJobRequest = {
    title: null,
    description: '',
    location: null,
    company: null,
    salary: null,
    type: null,
  };

  ngOnInit(): void {
    const jobIdParam = this.route.snapshot.paramMap.get('jobId');
    if (jobIdParam) {
      const id = Number(jobIdParam);
      if (isNaN(id)) {
        this.message.set('Invalid job ID');
        return;
      }
      this.editJobId.set(id);
      this.getPublicJobById(id);
    }
  }

  getPublicJobById(jobId: number) {
    this.fetching.set(true);
    this.jobsService.getPublicJobById(jobId).subscribe({
      next: (res) => {
        this.fetching.set(false);
        if (!res.success || !res.data) {
          this.message.set(res.message);
          return;
        }
        this.model = {
          title: res.data.title,
          description: res.data.description,
          location: res.data.location,
          company: res.data.company,
          salary: res.data.salary,
          type: res.data.type,
        };
        this.editPostedBy.set(res.data.postedBy);
      },
      error: () => {
        this.fetching.set(false);
        this.message.set('Failed to load job details');
      },
    });
  }

  submit() {
    const token = this.auth.getToken();
    if (!token) {
      this.message.set('Please log in to save a job');
      return;
    }
    const desc = this.model.description?.replace(/<[^>]+>/g, '').trim();
    if (!desc) { this.message.set('Description is required'); return; }
    const jobId = this.editJobId();
    if (jobId == null) {
      this.loading.set(true);
      this.jobsService.createRecruiterJob(this.model).subscribe({
        next: (res) => {
          this.loading.set(false);
          this.message.set(res.message);
          if (!res.success) return;
          this.router.navigate(['/recruiter/jobs']);
        },
        error: () => {
          this.loading.set(false);
          this.message.set('Failed to create job');
        },
      });
      return;
    }

    const postedBy = this.editPostedBy();
    if (postedBy == null) {
      this.message.set('Job is still loading; try again in a moment');
      return;
    }

    const job: Job = {
      id: jobId,
      postedBy,
      title: this.model.title,
      description: this.model.description,
      location: this.model.location,
      company: this.model.company,
      salary: this.model.salary,
      type: this.model.type,
    };

    this.loading.set(true);
    this.jobsService.updateRecruiterJob(job).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.message.set(res.message);
      },
      error: () => {
        this.loading.set(false);
        this.message.set('Failed to update job');
      },
    });
  }
  editorModules = {
    toolbar: [
      ['bold', 'italic', 'underline'],
      [{ list: 'ordered' }, { list: 'bullet' }],
      ['clean'],
    ],
  };
}
