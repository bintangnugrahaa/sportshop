import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import {
  User,
  KeyRound,
  MapPin,
  Package,
  Loader2,
} from 'lucide-react';
import {
  useProfile,
  useUpdateProfile,
  useChangePassword,
} from '../api/profileApi';
import {
  profileSchema,
  ProfileFormData,
  changePasswordSchema,
  ChangePasswordFormData,
} from '../schemas/profileSchema';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { toast } from '@/lib/toast';

export const ProfilePage: React.FC = () => {
  const { data: profile, isLoading } = useProfile();
  const updateProfileMutation = useUpdateProfile();
  const changePasswordMutation = useChangePassword();

  // Profile Form
  const {
    register: registerProfile,
    handleSubmit: handleSubmitProfile,
    reset: resetProfile,
    formState: { errors: profileErrors },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      fullName: '',
      phoneNumber: '',
    },
  });

  // Password Form
  const {
    register: registerPassword,
    handleSubmit: handleSubmitPassword,
    reset: resetPassword,
    formState: { errors: passwordErrors },
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmNewPassword: '',
    },
  });

  React.useEffect(() => {
    if (profile) {
      resetProfile({
        fullName: profile.fullName,
        phoneNumber: profile.phoneNumber ?? '',
      });
    }
  }, [profile, resetProfile]);

  const onProfileSubmit = async (data: ProfileFormData) => {
    try {
      await updateProfileMutation.mutateAsync({
        fullName: data.fullName,
        phoneNumber: data.phoneNumber || null,
      });
      toast.success('Profil berhasil diperbarui!');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal memperbarui profil.';
      toast.error(msg);
    }
  };

  const onPasswordSubmit = async (data: ChangePasswordFormData) => {
    try {
      await changePasswordMutation.mutateAsync({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      });
      toast.success('Kata sandi berhasil diubah!');
      resetPassword();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal mengubah kata sandi.';
      toast.error(msg);
    }
  };

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-20 flex justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 sm:px-6 lg:px-8 max-w-4xl">
      {/* Title */}
      <div className="mb-8">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-foreground">
          Profil & Keamanan Akun
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Kelola informasi identitas pribadi, kontak, dan kata sandi akun Anda.
        </p>
      </div>

      {/* Quick Navigation Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
        <Link
          to="/akun/pesanan"
          className="flex items-center gap-4 rounded-xl border bg-card p-4 hover:border-primary/50 hover:shadow-sm transition-all"
        >
          <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <Package className="h-6 w-6" />
          </div>
          <div>
            <h4 className="font-semibold text-sm text-foreground">Pesanan Saya</h4>
            <p className="text-xs text-muted-foreground mt-0.5">
              Lihat status transaksi dan riwayat belanja
            </p>
          </div>
        </Link>

        <Link
          to="/akun/alamat"
          className="flex items-center gap-4 rounded-xl border bg-card p-4 hover:border-primary/50 hover:shadow-sm transition-all"
        >
          <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-primary/10 text-primary">
            <MapPin className="h-6 w-6" />
          </div>
          <div>
            <h4 className="font-semibold text-sm text-foreground">Buku Alamat</h4>
            <p className="text-xs text-muted-foreground mt-0.5">
              Atur alamat pengiriman rumah dan kantor
            </p>
          </div>
        </Link>
      </div>

      <div className="space-y-8">
        {/* Profile Card */}
        <Card>
          <CardHeader className="border-b pb-4">
            <CardTitle className="text-lg flex items-center gap-2">
              <User className="h-5 w-5 text-primary" />
              <span>Informasi Pribadi</span>
            </CardTitle>
            <CardDescription>
              Perbarui nama lengkap dan nomor telepon untuk mempermudah komunikasi pengiriman.
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-6">
            <form onSubmit={handleSubmitProfile(onProfileSubmit)} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="fullName">Nama Lengkap *</Label>
                  <Input
                    id="fullName"
                    {...registerProfile('fullName')}
                    aria-invalid={Boolean(profileErrors.fullName)}
                  />
                  {profileErrors.fullName && (
                    <p className="text-xs text-destructive">{profileErrors.fullName.message}</p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="email">Alamat Email</Label>
                  <Input
                    id="email"
                    type="email"
                    value={profile?.email || ''}
                    disabled
                    className="bg-muted cursor-not-allowed opacity-80"
                  />
                  <p className="text-[11px] text-muted-foreground">
                    Email terdaftar tidak dapat diubah secara langsung.
                  </p>
                </div>
              </div>

              <div className="space-y-1.5 max-w-sm">
                <Label htmlFor="phoneNumber">Nomor Telepon</Label>
                <Input
                  id="phoneNumber"
                  placeholder="08123456789"
                  {...registerProfile('phoneNumber')}
                  aria-invalid={Boolean(profileErrors.phoneNumber)}
                />
                {profileErrors.phoneNumber && (
                  <p className="text-xs text-destructive">{profileErrors.phoneNumber.message}</p>
                )}
              </div>

              <div className="pt-3 flex justify-end">
                <Button type="submit" disabled={updateProfileMutation.isPending}>
                  {updateProfileMutation.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  Simpan Perubahan Profil
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>

        {/* Change Password Card */}
        <Card>
          <CardHeader className="border-b pb-4">
            <CardTitle className="text-lg flex items-center gap-2">
              <KeyRound className="h-5 w-5 text-primary" />
              <span>Ganti Kata Sandi</span>
            </CardTitle>
            <CardDescription>
              Pastikan akun Anda aman dengan menggunakan kata sandi unik minimal 8 karakter.
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-6">
            <form onSubmit={handleSubmitPassword(onPasswordSubmit)} className="space-y-4 max-w-lg">
              <div className="space-y-1.5">
                <Label htmlFor="currentPassword">Kata Sandi Saat Ini *</Label>
                <Input
                  id="currentPassword"
                  type="password"
                  placeholder="••••••••"
                  {...registerPassword('currentPassword')}
                  aria-invalid={Boolean(passwordErrors.currentPassword)}
                />
                {passwordErrors.currentPassword && (
                  <p className="text-xs text-destructive">
                    {passwordErrors.currentPassword.message}
                  </p>
                )}
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="newPassword">Kata Sandi Baru *</Label>
                <Input
                  id="newPassword"
                  type="password"
                  placeholder="••••••••"
                  {...registerPassword('newPassword')}
                  aria-invalid={Boolean(passwordErrors.newPassword)}
                />
                {passwordErrors.newPassword && (
                  <p className="text-xs text-destructive">{passwordErrors.newPassword.message}</p>
                )}
                <p className="text-[11px] text-muted-foreground">
                  Minimal 8 karakter, wajib kombinasi huruf besar (A-Z), huruf kecil (a-z), dan angka (0-9).
                </p>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="confirmNewPassword">Konfirmasi Kata Sandi Baru *</Label>
                <Input
                  id="confirmNewPassword"
                  type="password"
                  placeholder="••••••••"
                  {...registerPassword('confirmNewPassword')}
                  aria-invalid={Boolean(passwordErrors.confirmNewPassword)}
                />
                {passwordErrors.confirmNewPassword && (
                  <p className="text-xs text-destructive">
                    {passwordErrors.confirmNewPassword.message}
                  </p>
                )}
              </div>

              <div className="pt-3 flex justify-end">
                <Button
                  type="submit"
                  variant="outline"
                  disabled={changePasswordMutation.isPending}
                >
                  {changePasswordMutation.isPending && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  Perbarui Kata Sandi
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default ProfilePage;
