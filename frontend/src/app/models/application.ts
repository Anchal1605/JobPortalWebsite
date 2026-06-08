export interface JobApplication {
  id: number;
  jobId: number;
  userId: number;
  candidateName?: string | null;
  headline?: string | null;
  status: string | null;
  resumeUrl: string | null;
}

/** Candidate's application rows joined with job fields (`GET .../Applications/mine`). */
export interface MyApplication {
  id: number;
  jobId: number;
  jobTitle: string | null;
  company: string | null;
  location: string | null;
  status: string | null;
  resumeUrl: string | null;
}
