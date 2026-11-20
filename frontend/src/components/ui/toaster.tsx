import * as React from 'react';
import { CheckCircle2, AlertCircle, Info, XCircle, X } from 'lucide-react';
import { toast, ToastItem } from '@/lib/toast';
import { cn } from '@/lib/utils';

export const Toaster: React.FC = () => {
  const [toasts, setToasts] = React.useState<ToastItem[]>([]);

  React.useEffect(() => {
    return toast.subscribe(setToasts);
  }, []);

  if (toasts.length === 0) return null;

  return (
    <div
      aria-live="polite"
      className="fixed bottom-4 right-4 z-50 flex max-h-screen w-full max-w-sm flex-col gap-2 p-4"
    >
      {toasts.map((t) => {
        const Icon =
          t.type === 'success'
            ? CheckCircle2
            : t.type === 'error'
              ? AlertCircle
              : t.type === 'warning'
                ? XCircle
                : Info;

        const borderClasses =
          t.type === 'success'
            ? 'border-emerald-500 bg-emerald-50 text-emerald-900 dark:bg-emerald-950 dark:text-emerald-100'
            : t.type === 'error'
              ? 'border-rose-500 bg-rose-50 text-rose-900 dark:bg-rose-950 dark:text-rose-100'
              : t.type === 'warning'
                ? 'border-amber-500 bg-amber-50 text-amber-900 dark:bg-amber-950 dark:text-amber-100'
                : 'border-blue-500 bg-blue-50 text-blue-900 dark:bg-blue-950 dark:text-blue-100';

        return (
          <div
            key={t.id}
            role="status"
            className={cn(
              'pointer-events-auto flex items-start gap-3 rounded-lg border p-4 shadow-lg transition-all',
              borderClasses,
            )}
          >
            <Icon className="mt-0.5 h-5 w-5 shrink-0" />
            <div className="flex-1 text-sm">
              {t.title && <div className="font-semibold">{t.title}</div>}
              <div>{t.message}</div>
            </div>
            <button
              onClick={() => toast.dismiss(t.id)}
              className="rounded p-0.5 opacity-70 transition-opacity hover:opacity-100 focus:outline-none"
              aria-label="Tutup"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        );
      })}
    </div>
  );
};
