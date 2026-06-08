import { Component, inject, OnInit, signal, SecurityContext, viewChild, ElementRef } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { JobsService } from '../../../services/jobs';
import { Auth } from '../../../services/auth';
import { Job } from '../../../models/job';
import { ApiResponse } from '../../../models/api-response';
import { Applications } from '../../../services/applications';
import { ProfileService } from '../../../services/profile';
import { AiMatchService } from '../../../services/ai-match';
import { MatchScore } from '../../../models/match-score';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-job-detail',
  imports: [RouterLink],
  templateUrl: './job-detail.html',
  styleUrl: './job-detail.css',
  standalone: true,
})
export class JobDetail implements OnInit {

  safeDescription(html: string | null | undefined): string {
    return this.sanitizer.sanitize(SecurityContext.HTML, html ?? '') ?? '';
  }
  private readonly route = inject(ActivatedRoute);
  readonly auth = inject(Auth);
  private readonly jobsService = inject(JobsService);
  private readonly applicationsService = inject(Applications);
  private readonly profileService = inject(ProfileService);
  private readonly aiMatchService = inject(AiMatchService);
  /** Internal only — never shown in the template. */
  private profileResumeUrl = '';
  readonly hasProfileResume = signal(false);
  readonly selectedResumeFile = signal<File | null>(null);
  readonly selectedResumeFileName = signal<string>('');
  private readonly sanitizer = inject(DomSanitizer);
  readonly job = signal<Job | null>(null);
  readonly loading = signal(false);
  readonly applying = signal(false);
  readonly message = signal('');
  readonly hasAppliedToThisJob = signal(false);
  readonly checkingApplicationState = signal(false);
  /** True only right after a successful apply in this session (not on revisit). */
  readonly applicationJustSubmitted = signal(false);
  readonly matchScore = signal<MatchScore | null>(null);
  readonly matchLoading = signal(false);
  readonly matchError = signal('');
  private readonly resumeInput = viewChild<ElementRef<HTMLInputElement>>('resumeInput');

  ngOnInit(): void {
    const routeParams = this.route.snapshot.paramMap;
    const jobId = Number(routeParams.get('jobId'));
    if (isNaN(jobId)) {
      this.message.set('Invalid job ID');
      return;
    }
    this.loading.set(true);
    this.jobsService.getPublicJobById(jobId).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success || !res.data) {
          this.message.set(res.message);
          return;
        }
        this.job.set(res.data);
        this.prefillResumeFromProfile();
        this.loadApplicationStateForJob(jobId);
      },
      error: (error) => {
        this.loading.set(false);
        this.message.set('Failed to load job details');
        console.error('Failed to load job details', error);
      }
    });
  }

  activeResumeLabel(): string {
    const fileName = this.selectedResumeFileName();
    if (fileName) {
      return fileName;
    }
    if (this.hasProfileResume()) {
      return 'Profile résumé';
    }
    return '';
  }

  resumeSourceKind(): 'upload' | 'profile' | 'none' {
    if (this.selectedResumeFile()) {
      return 'upload';
    }
    if (this.hasProfileResume()) {
      return 'profile';
    }
    return 'none';
  }

  displayResumeName(): string {
    const label = this.activeResumeLabel();
    if (!label) {
      return '';
    }
    if (label.length <= 32) {
      return label;
    }
    return `${label.slice(0, 29)}…`;
  }

  openResumePicker(): void {
    this.resumeInput()?.nativeElement.click();
  }

  onResumeFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.selectedResumeFile.set(null);
      this.selectedResumeFileName.set('');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.message.set('Résumé must be 5 MB or smaller');
      input.value = '';
      return;
    }

    const isPdf =
      file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf');
    if (!isPdf) {
      this.message.set('Only PDF résumés are allowed');
      input.value = '';
      return;
    }

    this.message.set('');
    this.selectedResumeFile.set(file);
    this.selectedResumeFileName.set(file.name);
    this.clearMatchResult();
  }

  clearSelectedResume(): void {
    this.selectedResumeFile.set(null);
    this.selectedResumeFileName.set('');
    this.clearMatchResult();
  }

  canSubmitApplication(): boolean {
    return !!this.selectedResumeFile() || this.hasProfileResume();
  }

  private clearMatchResult(): void {
    this.matchScore.set(null);
    this.matchError.set('');
  }

  private loadApplicationStateForJob(jobId: number): void {
    const token = this.auth.getToken();
    if (!token || this.auth.sessionRoleId() !== this.auth.candidateRoleId) {
      this.hasAppliedToThisJob.set(false);
      return;
    }

    this.checkingApplicationState.set(true);
    this.applicationsService.getMyApplications().subscribe({
      next: (res) => {
        this.checkingApplicationState.set(false);
        if (!res.success || !res.data) {
          return;
        }
        const alreadyApplied = res.data.some((row) => row.jobId === jobId);
        if (!this.applicationJustSubmitted()) {
          this.hasAppliedToThisJob.set(alreadyApplied);
        }
      },
      error: () => {
        this.checkingApplicationState.set(false);
      },
    });
  }

  private prefillResumeFromProfile(): void {
    const token = this.auth.getToken();
    if (!token || this.auth.sessionRoleId() !== this.auth.candidateRoleId) {
      return;
    }
    this.profileService.getMyProfile().subscribe({
      next: (res) => {
        const url = res.data?.candidate?.resumeUrl?.trim();
        if (url) {
          this.profileResumeUrl = url;
          this.hasProfileResume.set(true);
        }
      },
    });
  }

  applyForJob() {
    const j = this.job();
    const token = this.auth.getToken();
    const resumeFile = this.selectedResumeFile();
    if (!j) {
      this.message.set('Job not loaded');
      return;
    }
    if (this.hasAppliedToThisJob()) {
      this.message.set(
        'You have already applied for this job. Check My applications for status updates.'
      );
      return;
    }
    if (!token) {
      this.message.set('Log in as a candidate to apply');
      return;
    }
    if (this.auth.sessionRoleId() !== this.auth.candidateRoleId) {
      this.message.set('Only candidates can apply to jobs.');
      return;
    }
    if (!this.canSubmitApplication()) {
      this.message.set('Upload a PDF résumé or add one on your profile first');
      return;
    }

    this.applying.set(true);
    this.message.set('');
    this.applicationsService
      .applyForJob(j.id, {
        resumeFile,
        resumeUrl: resumeFile ? null : this.profileResumeUrl,
      })
      .subscribe({
        next: (res) => {
          this.applying.set(false);
          if (res.success) {
            this.hasAppliedToThisJob.set(true);
            this.applicationJustSubmitted.set(true);
            this.message.set(res.message ?? 'Application submitted successfully');
          } else {
            this.message.set(res.message);
          }
        },
        error: (err: HttpErrorResponse) => {
          this.applying.set(false);
          const body = err.error as ApiResponse<string> | undefined;
          const fromApi =
            body && typeof body.message === 'string' && body.message.length > 0
              ? body.message
              : null;
          this.message.set(fromApi ?? err.message ?? 'Apply failed');
        },
      });
  }

  scoreRingClass(score: number): string {
    if (score >= 80) {
      return 'job-detail-score-ring--high';
    }
    if (score >= 60) {
      return 'job-detail-score-ring--mid';
    }
    return 'job-detail-score-ring--low';
  }

  analyzeJobMatch(): void {
    const jobId = this.job()?.id;
    if (!jobId) {
      return;
    }
    if (this.auth.sessionRoleId() !== this.auth.candidateRoleId) {
      this.matchError.set('Only candidates can analyze job matches.');
      return;
    }
    if (!this.canSubmitApplication()) {
      this.matchError.set('Choose a résumé below before analyzing fit.');
      return;
    }

    this.matchLoading.set(true);
    this.matchError.set('');
    this.matchScore.set(null);
    this.aiMatchService
      .matchJobToCandidate(jobId, { resumeFile: this.selectedResumeFile() })
      .subscribe({
        next: (res) => {
          this.matchLoading.set(false);
          if (!res.success || !res.data) {
            this.matchError.set(res.message || 'Could not analyze fit.');
            return;
          }
          this.matchScore.set(res.data);
        },
        error: (err: HttpErrorResponse) => {
          this.matchLoading.set(false);
          const body = err.error as ApiResponse<string> | undefined;
          this.matchError.set(body?.message ?? 'Failed to analyze job match.');
        },
      });
  }
}
