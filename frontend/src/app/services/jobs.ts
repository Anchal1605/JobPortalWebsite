import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/api-response';
import { CreateJobRequest, Job } from '../models/job';

@Injectable({
  providedIn: 'root',
})
export class JobsService {
  private readonly http = inject(HttpClient);

  getPublicJobs(filters?: {
    search?: string;
    location?: string;
    type?: string;
  }): Observable<ApiResponse<Job[]>> {
    let params = new HttpParams();

    const search = filters?.search?.trim();
    const location = filters?.location?.trim();
    const type = filters?.type?.trim();
    if (search) {
      params = params.set('search', search);
    }
    if (location) {
      params = params.set('location', location);
    }
    if (type) {
      params = params.set('type', type);
    }
    return this.http.get<ApiResponse<Job[]>>(`${environment.apiUrl}/api/Jobs`, { params: params });
  }

  createRecruiterJob(job: CreateJobRequest): Observable<ApiResponse<Job>> {
    return this.http.post<ApiResponse<Job>>(`${environment.apiUrl}/api/Jobs`, job);
  }

  getRecruiterPostedJobs(): Observable<ApiResponse<Job[]>> {
    return this.http.get<ApiResponse<Job[]>>(
      `${environment.apiUrl}/api/Jobs/employer-jobs`
    );
  }

  deleteRecruiterJob(jobId: number): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(
      `${environment.apiUrl}/api/Jobs/${jobId}`
    );
  }

  updateRecruiterJob(job: Job): Observable<ApiResponse<Job>> {
    return this.http.put<ApiResponse<Job>>(
      `${environment.apiUrl}/api/Jobs/${job.id}`,
      job
    );
  }

  getPublicJobById(jobId: number): Observable<ApiResponse<Job>> {
    return this.http.get<ApiResponse<Job>>(`${environment.apiUrl}/api/Jobs/${jobId}`);
  }
}
