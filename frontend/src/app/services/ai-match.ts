import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { MatchScore } from '../models/match-score';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';

@Injectable({
    providedIn: 'root',
})
export class AiMatchService {
    private readonly http = inject(HttpClient);

    matchJobToCandidate(
        jobId: number,
        options?: { resumeFile?: File | null }
    ): Observable<ApiResponse<MatchScore>> {
        const formData = new FormData();
        if (options?.resumeFile) {
            formData.append('resumeFile', options.resumeFile, options.resumeFile.name);
        }
        return this.http.post<ApiResponse<MatchScore>>(
            `${environment.apiUrl}/api/Ai/match/${jobId}`,
            formData
        );
    }
}
