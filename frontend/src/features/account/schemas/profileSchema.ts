import { z } from 'zod';

export const profileSchema = z.object({
  fullName: z
    .string()
    .min(2, 'Nama lengkap minimal 2 karakter.')
    .max(150, 'Nama lengkap maksimal 150 karakter.'),
  phoneNumber: z
    .string()
    .max(20, 'Nomor telepon maksimal 20 karakter.')
    .regex(/^[0-9+() -]*$/, 'Format nomor telepon tidak valid.')
    .optional()
    .or(z.literal('')),
});

export type ProfileFormData = z.infer<typeof profileSchema>;

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Kata sandi saat ini wajib diisi.'),
    newPassword: z
      .string()
      .min(8, 'Kata sandi baru minimal 8 karakter.')
      .regex(/[A-Z]/, 'Kata sandi baru harus mengandung minimal 1 huruf besar.')
      .regex(/[a-z]/, 'Kata sandi baru harus mengandung minimal 1 huruf kecil.')
      .regex(/[0-9]/, 'Kata sandi baru harus mengandung minimal 1 angka.'),
    confirmNewPassword: z.string().min(1, 'Konfirmasi kata sandi baru wajib diisi.'),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: 'Konfirmasi kata sandi baru tidak cocok.',
    path: ['confirmNewPassword'],
  });

export type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;
