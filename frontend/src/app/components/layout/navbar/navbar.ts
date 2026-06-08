import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Auth } from '../../../services/auth';
import { ProfileService } from '../../../services/profile';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar implements OnInit {
  readonly auth = inject(Auth);
  readonly profileService = inject(ProfileService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.refreshProfileStatus();
  }

  refreshProfileStatus(): void {
    if (this.auth.loggedIn()) {
      this.profileService.refreshCompletionStatus();
    }
  }

  logout(event: Event) {
    event.preventDefault();
    this.auth.logout();
    this.profileService.clearCompletionStatus();
    void this.router.navigateByUrl('/');
  }
}
