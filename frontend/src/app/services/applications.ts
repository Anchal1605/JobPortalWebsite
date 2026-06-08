import { inject, Injectable } from '@angular/core';
import { ApiResponse } from '../models/api-response';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { JobApplication, MyApplication } from '../models/application';

@Injectable({
  providedIn: 'root',
})
export class Applications {
  private readonly http = inject(HttpClient);

  getJobApplicants(jobId: number): Observable<ApiResponse<JobApplication[]>> {
    return this.http.get<ApiResponse<JobApplication[]>>(
      `${environment.apiUrl}/api/Applications/forJob/${jobId}`
    );
  }

  applyForJob(
    jobId: number,
    options: { resumeFile?: File | null; resumeUrl?: string | null }
  ): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('jobId', String(jobId));
    if (options.resumeFile) {
      formData.append('resumeFile', options.resumeFile, options.resumeFile.name);
    } else if (options.resumeUrl?.trim()) {
      formData.append('resumeUrl', options.resumeUrl.trim());
    }
    return this.http.post<ApiResponse<string>>(
      `${environment.apiUrl}/api/Applications`,
      formData
    );
  }

  getMyApplications(): Observable<ApiResponse<MyApplication[]>> {
    return this.http.get<ApiResponse<MyApplication[]>>(
      `${environment.apiUrl}/api/Applications/mine`
    );
  }

  updateApplicationStatus(
    applicationId: number,
    status: string
  ): Observable<ApiResponse<string>> {
    return this.http.patch<ApiResponse<string>>(
      `${environment.apiUrl}/api/Applications/${applicationId}/status`,
      { status }
    );
  }
}
