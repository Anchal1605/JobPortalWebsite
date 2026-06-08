import { Component, EventEmitter, inject, Input, Output, SecurityContext } from '@angular/core';
import { Job } from '../../../models/job';
import { RouterLink } from '@angular/router';
import { stripHtml } from '../../../utils/html-text';
import { DomSanitizer } from '@angular/platform-browser';


@Component({
  selector: 'app-jobs-content',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './jobs-content.html',
  styleUrl: './jobs-content.css',
})
export class JobsContentComponent {
  @Input() jobsLoading = false;
  @Input() jobsError: string | null = null;
  @Input() jobs: Job[] = [];
  @Input() searchText: string = '';
  @Output() readonly retry = new EventEmitter<void>();

  protected readonly stripHtml = stripHtml;
  private readonly sanitizer = inject(DomSanitizer);

  safeDescription(html: string | null | undefined): string {
    return this.sanitizer.sanitize(SecurityContext.HTML, html ?? '') ?? '';
  }
}
