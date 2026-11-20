import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { forgotPasswordSchema, ForgotPasswordFormData } from '../schemas';
import { authApi } from '../api';
import { setServerErrors } from '@/lib/serverErrors';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle2 } from 'lucide-react';

export const ForgotPasswordPage: React.FC = () => {
  const [isSubmitted, setIsSubmitted] = React.useState(false);
  const [generalError, setGeneralError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: '',
    },
  });

  const onSubmit = async (data: ForgotPasswordFormData) => {
    setGeneralError(null);
    try {
      await authApi.forgotPassword({ email: data.email });
      setIsSubmitted(true);
    } catch (err) {
      setServerErrors(err, setError, setGeneralError);
    }
  };

  if (isSubmitted) {
    return (
      <Card className="shadow-md">
        <CardHeader className="text-center space-y-2">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
            <CheckCircle2 className="h-6 w-6" />
          </div>
          <CardTitle className="text-xl font-bold">Periksa Email Anda</CardTitle>
          <CardDescription>
            Kami telah mengirimkan instruksi dan tautan atur ulang kata sandi ke alamat email Anda.
          </CardDescription>
        </CardHeader>
        <CardFooter className="flex justify-center">
          <Link to="/masuk">
            <Button variant="outline" size="sm">
              Kembali ke Halaman Masuk
            </Button>
          </Link>
        </CardFooter>
      </Card>
    );
  }

  return (
    <Card className="shadow-md">
      <CardHeader className="space-y-1">
        <CardTitle className="text-2xl font-bold">Lupa Kata Sandi</CardTitle>
        <CardDescription>
          Masukkan alamat email Anda untuk menerima tautan atur ulang kata sandi
        </CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <CardContent className="space-y-4">
          {generalError && (
            <div
              role="alert"
              className="rounded-md bg-destructive/15 p-3 text-sm text-destructive font-medium"
            >
              {generalError}
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="email" required>
              Email Akun
            </Label>
            <Input
              id="email"
              type="email"
              placeholder="nama@email.com"
              error={!!errors.email}
              {...register('email')}
            />
            {errors.email && (
              <p className="text-xs text-destructive font-medium">
                {errors.email.message}
              </p>
            )}
          </div>
        </CardContent>

        <CardFooter className="flex flex-col space-y-4">
          <Button
            type="submit"
            className="w-full"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Mengirim...' : 'Kirim Tautan Reset'}
          </Button>

          <p className="text-center text-xs text-muted-foreground">
            Ingat kata sandi Anda?{' '}
            <Link to="/masuk" className="font-semibold text-primary hover:underline">
              Masuk di sini
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
};

export default ForgotPasswordPage;
