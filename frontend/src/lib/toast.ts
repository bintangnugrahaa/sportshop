export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface ToastItem {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
  duration?: number;
}

type ToastListener = (toasts: ToastItem[]) => void;

class ToastManager {
  private toasts: ToastItem[] = [];
  private listeners: Set<ToastListener> = new Set();

  public subscribe(listener: ToastListener) {
    this.listeners.add(listener);
    listener([...this.toasts]);
    return () => {
      this.listeners.delete(listener);
    };
  }

  private notify() {
    for (const listener of this.listeners) {
      listener([...this.toasts]);
    }
  }

  public show(item: Omit<ToastItem, 'id'>) {
    const id = Math.random().toString(36).substring(2, 9);
    const toast: ToastItem = { ...item, id };
    this.toasts = [...this.toasts, toast];
    this.notify();

    const duration = item.duration ?? 4000;
    if (duration > 0) {
      setTimeout(() => {
        this.dismiss(id);
      }, duration);
    }
    return id;
  }

  public dismiss(id: string) {
    this.toasts = this.toasts.filter((t) => t.id !== id);
    this.notify();
  }

  public success(message: string, title?: string) {
    return this.show({ type: 'success', message, title });
  }

  public error(message: string, title?: string) {
    return this.show({ type: 'error', message, title });
  }

  public info(message: string, title?: string) {
    return this.show({ type: 'info', message, title });
  }

  public warning(message: string, title?: string) {
    return this.show({ type: 'warning', message, title });
  }
}

export const toast = new ToastManager();
