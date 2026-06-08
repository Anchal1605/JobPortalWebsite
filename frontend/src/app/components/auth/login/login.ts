import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Auth, LoginRequest } from '../../../services/auth';
import { ProfileService } from '../../../services/profile';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.css', '../auth-shell.css'],
})
export class Login {
  private readonly authService = inject(Auth);
  private readonly profileService = inject(ProfileService);
  private readonly router = inject(Router);
  loading = signal(false);
  message = signal('');
  readonly showPassword = signal(false);
  model: LoginRequest = {
    email: null,
    password: null
  };

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  submit() {
    if (!this.model.email || !this.model.password) {
      this.message.set('Email and password are required');
      return;
    }

    this.loading.set(true);
    this.authService.login(this.model).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success || !res.data) {
          this.message.set(res.message);
          return;
        }
        this.authService.saveToken(res.data);
        this.profileService.refreshCompletionStatus();
        this.message.set(res.message);
        const role = this.authService.sessionRoleId();
        if (role === this.authService.recruiterRoleId) {
          void this.router.navigateByUrl('/recruiter/jobs');
        } else {
          void this.router.navigateByUrl('/');
        }
      },
      error: () => {
        this.loading.set(false);
        this.message.set('Login failed');
      }
    });

  }
}
