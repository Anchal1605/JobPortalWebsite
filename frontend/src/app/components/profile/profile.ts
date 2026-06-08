import { Component, computed, inject, OnDestroy, OnInit, signal, viewChild, ElementRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../../services/auth';
import { ProfileService } from '../../services/profile';
import { ToastService } from '../../services/toast';
import { CandidateProfile, EmployerProfile, ProfileMe, UpdateCandidateProfile, UpdateEmployerProfile } from '../../models/profile';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit, OnDestroy {
  readonly auth = inject(Auth);
  private readonly profileApi = inject(ProfileService);
  private readonly toast = inject(ToastService);

  // UI state
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly passwordSaving = signal(false);
  readonly loadError = signal('');

  // Profile data from API
  readonly isProfileComplete = signal(false);
  readonly completionPercent = signal(0);
  readonly missingFields = signal<string[]>([]);
  readonly email = signal('');

  readonly candidateForm = signal<UpdateCandidateProfile>({
    name: '', headline: '', skills: '', experienceYears: null,
    resumeUrl: '', location: '', avatarUrl: '',
  });

  readonly employerForm = signal<UpdateEmployerProfile>({
    name: '', designation: '', contactNumber: '',
    companyName: '', companyWebsite: '', companyLocation: '',
    companyDescription: '', companyLogoUrl: '',
  });

  // Password form
  readonly currentPassword = signal('');
  readonly newPassword = signal('');
  readonly confirmPassword = signal('');

  // Photo/logo/resume before save
  private avatarFile: File | null = null;
  private logoFile: File | null = null;
  private resumeFile: File | null = null;
  readonly avatarPreview = signal<string | null>(null);
  readonly logoPreview = signal<string | null>(null);
  readonly selectedResumeFileName = signal('');
  private readonly profileResumeInput = viewChild<ElementRef<HTMLInputElement>>('profileResumeInput');
  private readonly avatarInput = viewChild<ElementRef<HTMLInputElement>>('avatarInput');
  private readonly logoInput = viewChild<ElementRef<HTMLInputElement>>('logoInput');

  readonly hasSavedResume = computed(() => !!this.candidateForm().resumeUrl?.trim());

  // Accordion sections
  readonly expandedSections = signal<Set<string>>(new Set(['details']));

  // Computed values for template
  readonly completionHint = computed(() =>
    this.missingFields().length ? `Still needed: ${this.missingFields().join(', ')}` : ''
  );

  readonly skillTags = computed(() =>
    (this.candidateForm().skills ?? '')
      .split(/[,;|]/).map((s) => s.trim()).filter(Boolean)
  );

  readonly displayAvatarUrl = computed(
    () => this.avatarPreview() ?? this.candidateForm().avatarUrl ?? null
  );

  readonly displayLogoUrl = computed(
    () => this.logoPreview() ?? this.employerForm().companyLogoUrl ?? null
  );

  ngOnInit(): void {
    this.loadProfile();
  }

  ngOnDestroy(): void {
    this.revokePreview(this.avatarPreview());
    this.revokePreview(this.logoPreview());
  }

  // ── Public methods (used by HTML) ──

  toggleSection(id: string): void {
    this.expandedSections.update((set) => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  isExpanded(id: string): boolean {
    return this.expandedSections().has(id);
  }

  experienceLabel(): string {
    const y = this.candidateForm().experienceYears;
    if (y == null) return 'Not set';
    return y === 1 ? '1 year' : `${y} years`;
  }

  updateCandidateField<K extends keyof UpdateCandidateProfile>(field: K, value: UpdateCandidateProfile[K]): void {
    this.candidateForm.update((f) => ({ ...f, [field]: value }));
  }

  updateEmployerField<K extends keyof UpdateEmployerProfile>(field: K, value: UpdateEmployerProfile[K]): void {
    this.employerForm.update((f) => ({ ...f, [field]: value }));
  }

  onAvatarFileSelected(event: Event): void {
    this.pickImage(event, 'avatar');
  }

  onLogoFileSelected(event: Event): void {
    this.pickImage(event, 'logo');
  }

  onResumeFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      this.resumeFile = null;
      this.selectedResumeFileName.set('');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.toast.error('Résumé must be 5 MB or smaller');
      input.value = '';
      return;
    }

    const isPdf =
      !file.type ||
      file.type === 'application/pdf' ||
      file.type === 'application/octet-stream' ||
      file.name.toLowerCase().endsWith('.pdf');
    if (!isPdf) {
      this.toast.error('Only PDF résumés are allowed');
      input.value = '';
      return;
    }

    this.selectedResumeFileName.set(file.name);
    this.uploadResumeFile(file, input);
  }

  private uploadResumeFile(file: File, input: HTMLInputElement): void {
    this.saving.set(true);
    this.profileApi.uploadResume(file).subscribe({
      next: (res) => {
        this.saving.set(false);
        const url = this.extractUploadUrl(res);
        if (!this.isApiSuccess(res) || !url) {
          this.toast.error(this.apiMessage(res) || 'Upload failed');
          input.value = '';
          this.selectedResumeFileName.set('');
          return;
        }
        this.updateCandidateField('resumeUrl', url);
        this.resumeFile = null;
        this.selectedResumeFileName.set('');
        this.toast.success('Résumé uploaded');
        this.reloadProfileSilently();
      },
      error: (err) => {
        this.saving.set(false);
        input.value = '';
        this.selectedResumeFileName.set('');
        this.toast.error(err.error?.message || 'Failed to upload résumé');
      },
    });
  }

  private extractUploadUrl(res: { data?: { url?: string; Url?: string } | null }): string {
    return res.data?.url?.trim() || res.data?.Url?.trim() || '';
  }

  private isApiSuccess(res: { success?: boolean; Success?: boolean }): boolean {
    return res.success === true || res.Success === true;
  }

  private apiMessage(res: { message?: string; Message?: string }): string {
    return (res.message || res.Message || '').trim();
  }

  openResumePicker(): void {
    this.profileResumeInput()?.nativeElement.click();
  }

  openAvatarPicker(): void {
    this.avatarInput()?.nativeElement.click();
  }

  openLogoPicker(): void {
    this.logoInput()?.nativeElement.click();
  }

  viewSavedResume(): void {
    const url = this.candidateForm().resumeUrl?.trim();
    if (url) {
      window.open(url, '_blank', 'noopener,noreferrer');
    }
  }

  resumeDisplayLabel(): string {
    const pending = this.selectedResumeFileName();
    if (pending) {
      return pending.length > 36 ? `${pending.slice(0, 33)}…` : pending;
    }
    if (this.hasSavedResume()) {
      return 'Résumé on file';
    }
    return '';
  }

  hasResumePendingOrSaved(): boolean {
    return this.hasSavedResume() || !!this.selectedResumeFileName();
  }

  loadProfile(): void {
    const token = this.auth.getToken();
    if (!token) {
      this.loadError.set('Please log in to view your profile.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.loadError.set('');
    this.profileApi.getMyProfile().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success || !res.data) {
          this.loadError.set(res.message ?? 'Could not load profile');
          return;
        }
        this.applyProfileData(res.data);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set('Failed to load profile');
      },
    });
  }

  private reloadProfileSilently(): void {
    this.profileApi.getMyProfile().subscribe({
      next: (res) => {
        if (!res.success || !res.data) return;
        this.applyProfileData(res.data);
      },
    });
  }

  private refreshCompletionOnly(): void {
    this.profileApi.getMyProfile().subscribe({
      next: (res) => {
        if (!res.success || !res.data) return;
        this.isProfileComplete.set(res.data.isProfileComplete);
        this.completionPercent.set(res.data.completionPercent);
        this.missingFields.set(res.data.missingFields ?? []);
      },
    });
  }

  private saveCandidateProfile(): void {
    if (this.avatarFile) {
      this.uploadThenSave(this.avatarFile, 'avatar', () => this.saveCandidateText());
      return;
    }

    this.saveCandidateText();
  }

  private buildCandidateUpdateBody(): UpdateCandidateProfile {
    const form = this.candidateForm();
    const body: UpdateCandidateProfile = {
      name: form.name,
      headline: form.headline,
      skills: form.skills,
      experienceYears: form.experienceYears,
      location: form.location,
    };

    const resumeUrl = form.resumeUrl?.trim();
    if (resumeUrl) {
      body.resumeUrl = resumeUrl;
    }

    const avatarUrl = form.avatarUrl?.trim();
    if (avatarUrl) {
      body.avatarUrl = avatarUrl;
    }

    return body;
  }

  private buildEmployerUpdateBody(): UpdateEmployerProfile {
    const form = this.employerForm();
    const body: UpdateEmployerProfile = {
      name: form.name,
      designation: form.designation,
      contactNumber: form.contactNumber,
      companyName: form.companyName,
      companyWebsite: form.companyWebsite,
      companyLocation: form.companyLocation,
      companyDescription: form.companyDescription,
    };

    const logoUrl = form.companyLogoUrl?.trim();
    if (logoUrl) {
      body.companyLogoUrl = logoUrl;
    }

    return body;
  }

  private applyProfileData(d: ProfileMe): void {
    this.isProfileComplete.set(d.isProfileComplete);
    this.completionPercent.set(d.completionPercent);
    this.missingFields.set(d.missingFields ?? []);

    if (d.candidate) {
      this.syncCandidateFromServer(d.candidate);
    } else if (d.employer) {
      this.syncEmployerFromServer(d.employer);
    } else if ((d as ProfileMe & { Candidate?: CandidateProfile }).Candidate) {
      this.syncCandidateFromServer((d as ProfileMe & { Candidate?: CandidateProfile }).Candidate!);
    } else if ((d as ProfileMe & { Employer?: EmployerProfile }).Employer) {
      this.syncEmployerFromServer((d as ProfileMe & { Employer?: EmployerProfile }).Employer!);
    }
  }

  private syncCandidateFromServer(c: CandidateProfile & { ResumeUrl?: string | null }): void {
    this.email.set(c.email ?? '');
    const resumeUrl = (c.resumeUrl ?? c.ResumeUrl ?? '').trim();
    this.candidateForm.set({
      name: c.name ?? '', headline: c.headline ?? '', skills: c.skills ?? '',
      experienceYears: c.experienceYears ?? null, resumeUrl,
      location: c.location ?? '', avatarUrl: c.avatarUrl ?? '',
    });
  }

  private syncEmployerFromServer(e: EmployerProfile): void {
    this.email.set(e.email ?? '');
    this.employerForm.set({
      name: e.name ?? '', designation: e.designation ?? '',
      contactNumber: e.contactNumber ?? '', companyName: e.company?.name ?? '',
      companyWebsite: e.company?.website ?? '', companyLocation: e.company?.location ?? '',
      companyDescription: e.company?.description ?? '', companyLogoUrl: e.company?.logoUrl ?? '',
    });
  }

  save(): void {
    const token = this.auth.getToken();
    if (!token) {
      this.toast.error('Please log in');
      return;
    }

    this.saving.set(true);

    const role = this.auth.sessionRoleId();
    if (role === this.auth.candidateRoleId) {
      this.saveCandidateProfile();
    } else if (role === this.auth.recruiterRoleId) {
      this.logoFile
        ? this.uploadThenSave(this.logoFile, 'logo')
        : this.saveEmployerText();
    } else {
      this.saving.set(false);
      this.toast.error('Profile is not available for your account type');
    }
  }

  changePassword(): void {
    const token = this.auth.getToken();
    if (!token) {
      this.toast.error('Please log in');
      return;
    }

    const current = this.currentPassword();
    const next = this.newPassword();
    if (!current || !next) {
      this.toast.error('Enter current and new password');
      return;
    }
    if (next.length < 6) {
      this.toast.error('New password must be at least 6 characters');
      return;
    }
    if (next !== this.confirmPassword()) {
      this.toast.error('New passwords do not match');
      return;
    }

    this.passwordSaving.set(true);

    this.auth.changePassword({ currentPassword: current, newPassword: next }).subscribe({
      next: (res) => {
        this.passwordSaving.set(false);
        this.toast.success(res.message ?? 'Password updated');
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
      },
      error: (err) => {
        this.passwordSaving.set(false);
        this.toast.error(err.error?.message ?? 'Failed to change password');
      },
    });
  }

  // ── Private helpers ──

  private pickImage(event: Event, kind: 'avatar' | 'logo'): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (file.size > 2 * 1024 * 1024) {
      this.toast.error('Image must be 2 MB or smaller');
      return;
    }

    if (kind === 'avatar') {
      this.avatarFile = file;
      this.revokePreview(this.avatarPreview());
      this.avatarPreview.set(URL.createObjectURL(file));
    } else {
      this.logoFile = file;
      this.revokePreview(this.logoPreview());
      this.logoPreview.set(URL.createObjectURL(file));
    }
  }

  private uploadThenSave(file: File, kind: 'avatar' | 'logo', onSuccess?: () => void): void {
    const upload$ = kind === 'avatar'
      ? this.profileApi.uploadAvatar(file)
      : this.profileApi.uploadCompanyLogo(file);

    upload$.subscribe({
      next: (res) => {
        if (!this.isApiSuccess(res) || !this.extractUploadUrl(res)) {
          this.saving.set(false);
          this.toast.error(this.apiMessage(res) || 'Upload failed');
          return;
        }
        if (kind === 'avatar') {
          this.updateCandidateField('avatarUrl', this.extractUploadUrl(res));
          this.avatarFile = null;
          this.revokePreview(this.avatarPreview());
          this.avatarPreview.set(null);
          if (onSuccess) {
            onSuccess();
          } else {
            this.saveCandidateText();
          }
        } else {
          this.updateEmployerField('companyLogoUrl', this.extractUploadUrl(res));
          this.logoFile = null;
          this.revokePreview(this.logoPreview());
          this.logoPreview.set(null);
          this.saveEmployerText();
        }
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Upload failed');
      },
    });
  }

  private saveCandidateText(): void {
    const body = this.buildCandidateUpdateBody();
    const knownResumeUrl = body.resumeUrl?.trim() || '';

    this.profileApi.updateCandidateProfile(body).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.toast.success(this.apiMessage(res) || 'Profile saved');
        if (this.isApiSuccess(res) && res.data) {
          this.syncCandidateFromServer(res.data);
        }
        if (knownResumeUrl && !this.candidateForm().resumeUrl?.trim()) {
          this.updateCandidateField('resumeUrl', knownResumeUrl);
        }
        this.refreshCompletionOnly();
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Failed to save profile');
      },
    });
  }

  private saveEmployerText(): void {
    this.profileApi.updateEmployerProfile(this.buildEmployerUpdateBody()).subscribe({
      next: (res) => {
        this.saving.set(false);
        this.toast.success(res.message ?? 'Profile saved');
        if (res.success && res.data) {
          this.syncEmployerFromServer(res.data);
        }
        this.refreshCompletionOnly();
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Failed to save profile');
      },
    });
  }

  private revokePreview(url: string | null): void {
    if (url) URL.revokeObjectURL(url);
  }
}