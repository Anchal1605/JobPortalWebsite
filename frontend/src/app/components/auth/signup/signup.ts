import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth, RegisterRequest } from '../../../services/auth';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './signup.html',
  styleUrls: ['./signup.css', '../auth-shell.css'],
})
export class Signup {
  private readonly authService = inject(Auth);
  private readonly router = inject(Router);

  readonly roleCandidate = 3;
  readonly roleEmployer = 2;

  loading = signal(false);
  message = signal('');
  readonly showPassword = signal(false);

  model: RegisterRequest = {
    name: null,
    email: null,
    password: null,
    roleId: null,
  };

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  submit() {
    if (!this.model.name || !this.model.email || !this.model.password || !this.model.roleId) {
      this.message.set('All fields are required');
      return;
    }

    this.loading.set(true);
    this.authService.register(this.model).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (!res.success) {
          this.message.set(res.message);
          return;
        }

        this.message.set(res.message);
        void this.router.navigateByUrl('/auth/login');
      },
      error: () => {
        this.loading.set(false);
        this.message.set('Failed to register');
      },
    });
  }
}
