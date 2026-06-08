import { Component, inject } from '@angular/core';
import { ToastService } from '../../../services/toast';

@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.html',
  styleUrl: './toast.css',
})
export class Toast {
  readonly toast = inject(ToastService);

  dismiss(id: number): void {
    this.toast.dismiss(id);
  }
}
