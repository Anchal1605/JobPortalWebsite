export interface CompanyProfile {
  id: number;
  name: string | null;
  website: string | null;
  location: string | null;
  description: string | null;
  logoUrl: string | null;
}

export interface CandidateProfile {
  id: number;
  userId: number;
  name: string | null;
  email: string | null;
  headline: string | null;
  skills: string | null;
  experienceYears: number | null;
  resumeUrl: string | null;
  location: string | null;
  avatarUrl: string | null;
}

export interface EmployerProfile {
  id: number;
  userId: number;
  name: string | null;
  email: string | null;
  designation: string | null;
  contactNumber: string | null;
  company: CompanyProfile | null;
}

export interface ProfileMe {
  roleId: number;
  isProfileComplete: boolean;
  completionPercent: number;
  missingFields: string[];
  candidate: CandidateProfile | null;
  employer: EmployerProfile | null;
}

export interface UpdateCandidateProfile {
  name?: string | null;
  headline?: string | null;
  skills?: string | null;
  experienceYears?: number | null;
  resumeUrl?: string | null;
  location?: string | null;
  avatarUrl?: string | null;
}

export interface UpdateEmployerProfile {
  name?: string | null;
  designation?: string | null;
  contactNumber?: string | null;
  companyName?: string | null;
  companyWebsite?: string | null;
  companyLocation?: string | null;
  companyDescription?: string | null;
  companyLogoUrl?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
