import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response';
import {
  CandidateProfile,
  EmployerProfile,
  ProfileMe,
  UpdateCandidateProfile,
  UpdateEmployerProfile,
} from '../models/profile';

@Injectable({
  providedIn: 'root',
})
export class ProfileService {
  private readonly http = inject(HttpClient);

  readonly isProfileComplete = signal<boolean | null>(null);
  readonly completionPercent = signal<number>(0);

  getMyProfile(): Observable<ApiResponse<ProfileMe>> {
    return this.http
      .get<ApiResponse<ProfileMe>>(`${environment.apiUrl}/api/Profiles/me`)
      .pipe(tap((res) => this.applyCompletionFromResponse(res)));
  }

  refreshCompletionStatus(): void {
    this.getMyProfile().subscribe();
  }

  clearCompletionStatus(): void {
    this.isProfileComplete.set(null);
    this.completionPercent.set(0);
  }

  updateCandidateProfile(
    body: UpdateCandidateProfile
  ): Observable<ApiResponse<CandidateProfile>> {
    return this.http
      .put<ApiResponse<CandidateProfile>>(
        `${environment.apiUrl}/api/Profiles/me/candidate`,
        body
      )
      .pipe(tap(() => this.refreshCompletionStatus()));
  }

  updateEmployerProfile(
    body: UpdateEmployerProfile
  ): Observable<ApiResponse<EmployerProfile>> {
    return this.http
      .put<ApiResponse<EmployerProfile>>(
        `${environment.apiUrl}/api/Profiles/me/employer`,
        body
      )
      .pipe(tap(() => this.refreshCompletionStatus()));
  }

  uploadAvatar(file: File): Observable<ApiResponse<{ url: string }>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http
      .post<ApiResponse<{ url: string }>>(
        `${environment.apiUrl}/api/Profiles/me/avatar`,
        formData
      )
      .pipe(tap(() => this.refreshCompletionStatus()));
  }

  uploadResume(file: File): Observable<ApiResponse<{ url: string }>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http
      .post<ApiResponse<{ url: string }>>(
        `${environment.apiUrl}/api/Profiles/me/resume`,
        formData
      )
      .pipe(tap(() => this.refreshCompletionStatus()));
  }

  uploadCompanyLogo(file: File): Observable<ApiResponse<{ url: string }>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http
      .post<ApiResponse<{ url: string }>>(
        `${environment.apiUrl}/api/Profiles/me/company-logo`,
        formData
      )
      .pipe(tap(() => this.refreshCompletionStatus()));
  }

  private applyCompletionFromResponse(res: ApiResponse<ProfileMe>): void {
    if (res.success && res.data) {
      this.isProfileComplete.set(res.data.isProfileComplete);
      this.completionPercent.set(res.data.completionPercent);
    }
  }
}
