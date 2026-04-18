import { Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ToastService } from './toast.service';
import type { ToastItem } from './toast.model';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.scss',
})
export class ToastContainerComponent {
  readonly toast = inject(ToastService);

  trackById(_: number, item: ToastItem): string {
    return item.id;
  }

  liveRole(item: ToastItem): 'alert' | 'status' {
    return item.kind === 'error' || item.kind === 'warning' ? 'alert' : 'status';
  }

  livePoliteness(item: ToastItem): 'assertive' | 'polite' {
    return item.kind === 'error' || item.kind === 'warning' ? 'assertive' : 'polite';
  }
}
