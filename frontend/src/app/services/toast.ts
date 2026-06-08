import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'success' | 'error' | 'info';

export interface ToastItem {
  id: number;
  message: string;
  variant: ToastVariant;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly items = signal<ToastItem[]>([]);

  private nextId = 0;

  success(message: string, durationMs = 4000): void {
    this.show(message, 'success', durationMs);
  }

  error(message: string, durationMs = 5000): void {
    this.show(message, 'error', durationMs);
  }

  info(message: string, durationMs = 4000): void {
    this.show(message, 'info', durationMs);
  }

  dismiss(id: number): void {
    this.items.update((items) => items.filter((item) => item.id !== id));
  }

  private show(message: string, variant: ToastVariant, durationMs: number): void {
    const trimmed = message.trim();
    if (!trimmed) return;

    const id = ++this.nextId;
    this.items.update((items) => [...items, { id, message: trimmed, variant }]);

    window.setTimeout(() => this.dismiss(id), durationMs);
  }
}
