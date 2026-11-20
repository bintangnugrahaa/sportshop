import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useNavigate } from 'react-router-dom';
import { registerSchema, RegisterFormData } from '../schemas';
import { useAuthStore } from '@/stores/authStore';
import { setServerErrors } from '@/lib/serverErrors';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/lib/toast';

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { register: registerUser } = useAuthStore();
  const [generalError, setGeneralError] = React.useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
    },
  });

  const onSubmit = async (data: RegisterFormData) => {
    setGeneralError(null);
    try {
      await registerUser({
        fullName: data.fullName,
        email: data.email,
        password: data.password,
        phoneNumber: data.phoneNumber || undefined,
      });
      toast.success('Pendaftaran akun berhasil! Silakan masuk ke akun Anda.');
      navigate('/masuk');
    } catch (err) {
      setServerErrors(err, setError, setGeneralError);
    }
  };

  return (
    <Card className="shadow-md">
      <CardHeader className="space-y-1">
        <CardTitle className="text-2xl font-bold">Daftar Akun Baru</CardTitle>
        <CardDescription>
          Buat akun pelanggan untuk berbelanja perlengkapan olahraga terbaik
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
            <Label htmlFor="fullName" required>
              Nama Lengkap
            </Label>
            <Input
              id="fullName"
              placeholder="Contoh: Budi Santoso"
              error={!!errors.fullName}
              {...register('fullName')}
            />
            {errors.fullName && (
              <p className="text-xs text-destructive font-medium">
                {errors.fullName.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="email" required>
              Alamat Email
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

          <div className="space-y-2">
            <Label htmlFor="phoneNumber">Nomor Telepon (Opsional)</Label>
            <Input
              id="phoneNumber"
              type="tel"
              placeholder="081234567890"
              error={!!errors.phoneNumber}
              {...register('phoneNumber')}
            />
            {errors.phoneNumber && (
              <p className="text-xs text-destructive font-medium">
                {errors.phoneNumber.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="password" required>
              Kata Sandi
            </Label>
            <Input
              id="password"
              type="password"
              placeholder="Min. 8 karakter (huruf besar, kecil, angka)"
              error={!!errors.password}
              {...register('password')}
            />
            {errors.password && (
              <p className="text-xs text-destructive font-medium">
                {errors.password.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword" required>
              Konfirmasi Kata Sandi
            </Label>
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Ulangi kata sandi"
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
            {isSubmitting ? 'Mendaftarkan...' : 'Daftar Sekarang'}
          </Button>

          <p className="text-center text-xs text-muted-foreground">
            Sudah punya akun?{' '}
            <Link to="/masuk" className="font-semibold text-primary hover:underline">
              Masuk di sini
            </Link>
          </p>
        </CardFooter>
      </form>
    </Card>
  );
};

export default RegisterPage;
