import { Routes } from '@angular/router';
import { Signup } from './components/auth/signup/signup';
import { Login } from './components/auth/login/login';
import { RecruiterCreateJob } from './components/recruiter/create-job/create-job';
import { RecruiterMyJobs } from './components/recruiter/my-jobs/my-jobs';
import { RecruiterApplicants } from './components/recruiter/applicants/applicants';
import { Home } from './components/home/home';
import { JobDetail } from './components/candidate/job-detail/job-detail';
import { MyApplications } from './components/candidate/my-applications/my-applications';
import { candidateOnlyGuard, loggedInGuard, recruiterOnlyGuard } from './guards/role.guards';
import { Profile } from './components/profile/profile';
import { ErrorPage } from './components/error-page/error-page';


export const routes: Routes = [
    { path: '', component: Home, pathMatch: 'full' },
    { path: 'home', redirectTo: '', pathMatch: 'full' },
    { path: 'auth/signup', component: Signup },
    { path: 'auth/login', component: Login },
    { path: 'recruiter/jobs/create', component: RecruiterCreateJob, canActivate: [recruiterOnlyGuard] },
    { path: 'recruiter/jobs', component: RecruiterMyJobs, canActivate: [recruiterOnlyGuard] },
    { path: 'recruiter/jobs/:jobId/applicants', component: RecruiterApplicants, canActivate: [recruiterOnlyGuard] },
    { path: 'signup', redirectTo: 'auth/signup', pathMatch: 'full' },
    { path: 'login', redirectTo: 'auth/login', pathMatch: 'full' },
    { path: 'create-job', redirectTo: 'recruiter/jobs/create', pathMatch: 'full' },
    { path: 'recruiter/jobs/:jobId/edit', component: RecruiterCreateJob, canActivate: [recruiterOnlyGuard] },
    { path: 'jobs/:jobId', component: JobDetail },
    { path: 'my-applications', component: MyApplications, canActivate: [candidateOnlyGuard] },
    { path: 'profile', component: Profile, canActivate: [loggedInGuard] },
    { path: '**', component: ErrorPage },
];
