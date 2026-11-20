import axios from 'axios';
import { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { ProblemDetails } from '@/types';
import { toast } from '@/lib/toast';

export function setServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  setGeneralError?: (msg: string | null) => void,
) {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as ProblemDetails;

    if (data.errors && typeof data.errors === 'object') {
      let matchedAny = false;
      for (const [key, messages] of Object.entries(data.errors)) {
        if (Array.isArray(messages) && messages.length > 0) {
          const camelKey = (key.charAt(0).toLowerCase() + key.slice(1)) as Path<T>;
          setError(camelKey, {
            type: 'server',
            message: messages[0],
          });
          matchedAny = true;
        }
      }
      if (matchedAny) {
        if (setGeneralError) setGeneralError(null);
        return;
      }
    }

    const message = data.detail || data.title || 'Terjadi kesalahan pada permintaan.';
    if (setGeneralError) {
      setGeneralError(message);
    } else {
      toast.error(message);
    }
    return;
  }

  const fallback = error instanceof Error ? error.message : 'Terjadi kesalahan tidak terduga.';
  if (setGeneralError) {
    setGeneralError(fallback);
  } else {
    toast.error(fallback);
  }
}
