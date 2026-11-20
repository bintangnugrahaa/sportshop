declare global {
  interface Window {
    snap?: {
      pay: (
        token: string,
        options?: {
          onSuccess?: (result: unknown) => void;
          onPending?: (result: unknown) => void;
          onError?: (result: unknown) => void;
          onClose?: () => void;
        }
      ) => void;
    };
  }
}

export function loadSnapScript(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (typeof window === 'undefined') {
      resolve();
      return;
    }

    if (window.snap) {
      resolve();
      return;
    }

    const scriptId = 'midtrans-snap-script';
    const existing = document.getElementById(scriptId);
    if (existing) {
      resolve();
      return;
    }

    const isProduction = import.meta.env.VITE_MIDTRANS_IS_PRODUCTION === 'true';
    const clientKey = import.meta.env.VITE_MIDTRANS_CLIENT_KEY || 'SB-Mid-client-placeholder';
    const src = isProduction
      ? 'https://app.midtrans.com/snap/snap.js'
      : 'https://app.sandbox.midtrans.com/snap/snap.js';

    const script = document.createElement('script');
    script.id = scriptId;
    script.src = src;
    script.setAttribute('data-client-key', clientKey);
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('Gagal memuat script pembayaran Midtrans Snap.'));
    document.body.appendChild(script);
  });
}
