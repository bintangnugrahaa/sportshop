import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { resetPasswordSchema, ResetPasswordFormData } from '../schemas';
import { authApi } from '../api';
import { setServerErrors } from '@/lib/serverErrors';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/lib/toast';

export const ResetPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const emailParam = searchParams.get('email') || '';
  const tokenParam = searchParams.get('token') || '';
  const [generalError, setGeneralError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      email: emailParam,
      token: tokenParam,
      newPassword: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data: ResetPasswordFormData) => {
    setGeneralError(null);
    try {
      await authApi.resetPassword({
        email: data.email,
        token: data.token,
        newPassword: data.newPassword,
      });
      toast.success('Kata sandi berhasil diatur ulang! Silakan masuk dengan kata sandi baru.');
      navigate('/masuk');
    } catch (err) {
      setServerErrors(err, setError, setGeneralError);
    }
  };

  return (
    <Card className="shadow-md">
      <CardHeader className="space-y-1">
        <CardTitle className="text-2xl font-bold">Atur Ulang Kata Sandi</CardTitle>
        <CardDescription>
          Buat kata sandi baru yang kuat untuk akun Anda
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
              Email
            </Label>
            <Input
              id="email"
              type="email"
              error={!!errors.email}
              {...register('email')}
            />
            {errors.email && (
              <p className="text-xs text-destructive font-medium">
                {errors.email.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="token" required>
              Token Reset
            </Label>
            <Input
              id="token"
              placeholder="Masukkan token dari email"
              error={!!errors.token}
              {...register('token')}
            />
            {errors.token && (
              <p className="text-xs text-destructive font-medium">
                {errors.token.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="newPassword" required>
              Kata Sandi Baru
            </Label>
            <Input
              id="newPassword"
              type="password"
              placeholder="Min. 8 karakter (huruf besar, kecil, angka)"
              error={!!errors.newPassword}
              {...register('newPassword')}
            />
            {errors.newPassword && (
              <p className="text-xs text-destructive font-medium">
                {errors.newPassword.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword" required>
              Konfirmasi Kata Sandi Baru
            </Label>
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Ulangi kata sandi baru"
              error={!!errors.confirmPassword}
              {...register('confirmPassword')}
            />
            {errors.confirmPassword && (
              <p className="text-xs text-destructive font-medium">
                {errors.confirmPassword.message}
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
            {isSubmitting ? 'Menyimpan...' : 'Atur Ulang Kata Sandi'}
          </Button>

          <p className="text-center text-xs text-muted-foreground">
            Batal atur ulang?{' '}
            <Link to="/masuk" className="font-semibold text-primary hover:underline">
              Kembali ke Masuk
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
};

export default ResetPasswordPage;
