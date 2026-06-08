import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { Navbar } from './components/layout/navbar/navbar';
import { Toast } from './components/common/toast/toast';

function isAuthPath(url: string): boolean {
  const path = url.split(/[?#]/)[0];
  return path === '/auth/login' || path === '/auth/signup';
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Navbar, Toast],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly router = inject(Router);
  /** Full-bleed auth pages hide the main nav. */
  readonly showNavbar = signal(!isAuthPath(this.router.url));

  constructor() {
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.showNavbar.set(!isAuthPath(this.router.url)));
  }
}
