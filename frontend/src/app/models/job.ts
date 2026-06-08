export interface Job {
  id: number;
  title: string | null;
  description: string | null;
  location: string | null;
  company: string | null;
  salary: string | null;
  type: string | null;
  postedBy: number;
}

export interface CreateJobRequest {
  title: string | null;
  description: string | null;
  location: string | null;
  company: string | null;
  salary: string | null;
  type: string | null;
}
