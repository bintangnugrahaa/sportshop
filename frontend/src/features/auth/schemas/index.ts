import { z } from 'zod';

export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Email wajib diisi.')
    .email('Format email tidak valid.'),
  password: z.string().min(1, 'Kata sandi wajib diisi.'),
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const registerSchema = z
  .object({
    fullName: z
      .string()
      .min(1, 'Nama lengkap wajib diisi.')
      .max(150, 'Nama lengkap maksimal 150 karakter.'),
    email: z
      .string()
      .min(1, 'Email wajib diisi.')
      .email('Format email tidak valid.')
      .max(256, 'Email maksimal 256 karakter.'),
    phoneNumber: z
      .string()
      .max(20, 'Nomor telepon maksimal 20 karakter.')
      .optional()
      .or(z.literal('')),
    password: z
      .string()
      .min(8, 'Kata sandi minimal 8 karakter.')
      .regex(/[A-Z]/, 'Kata sandi harus mengandung huruf besar.')
      .regex(/[a-z]/, 'Kata sandi harus mengandung huruf kecil.')
      .regex(/[0-9]/, 'Kata sandi harus mengandung angka.'),
    confirmPassword: z.string().min(1, 'Konfirmasi kata sandi wajib diisi.'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Konfirmasi kata sandi tidak cocok.',
    path: ['confirmPassword'],
  });

export type RegisterFormData = z.infer<typeof registerSchema>;

export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, 'Email wajib diisi.')
    .email('Format email tidak valid.'),
});

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    email: z
      .string()
      .min(1, 'Email wajib diisi.')
      .email('Format email tidak valid.'),
    token: z.string().min(1, 'Token reset wajib diisi.'),
    newPassword: z
      .string()
      .min(8, 'Kata sandi baru minimal 8 karakter.')
      .regex(/[A-Z]/, 'Kata sandi baru harus mengandung huruf besar.')
      .regex(/[a-z]/, 'Kata sandi baru harus mengandung huruf kecil.')
      .regex(/[0-9]/, 'Kata sandi baru harus mengandung angka.'),
    confirmPassword: z
      .string()
      .min(1, 'Konfirmasi kata sandi baru wajib diisi.'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Konfirmasi kata sandi tidak cocok.',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;
