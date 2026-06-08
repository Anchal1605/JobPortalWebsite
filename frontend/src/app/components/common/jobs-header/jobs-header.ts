import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-jobs-header',
  standalone: true,
  templateUrl: './jobs-header.html',
  styleUrl: './jobs-header.css',
})
export class JobsHeaderComponent {
  @Input() title = 'Job Portal';
  @Input() jobsCount = 0;
  @Input() jobsLoading = false;
  @Input() searchText = '';

  @Output() readonly refresh = new EventEmitter<void>();
  @Output() readonly searchChange = new EventEmitter<string>();

  onSearchInput(value: string): void {
    this.searchChange.emit(value);
  }
}
