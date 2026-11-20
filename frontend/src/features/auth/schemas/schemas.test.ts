import { describe, expect, it } from 'vitest';
import {
  loginSchema,
  registerSchema,
  forgotPasswordSchema,
  resetPasswordSchema,
} from './index';

describe('Auth Zod Schemas', () => {
  describe('loginSchema', () => {
    it('validates correct email and password', () => {
      const result = loginSchema.safeParse({
        email: 'user@sportshop.dev',
        password: 'Password123',
      });
      expect(result.success).toBe(true);
    });

    it('rejects empty or invalid email', () => {
      const empty = loginSchema.safeParse({ email: '', password: '123' });
      expect(empty.success).toBe(false);
      if (!empty.success) {
        expect(empty.error.issues[0]?.message).toBe('Email wajib diisi.');
      }

      const invalid = loginSchema.safeParse({ email: 'not-an-email', password: '123' });
      expect(invalid.success).toBe(false);
      if (!invalid.success) {
        expect(invalid.error.issues[0]?.message).toBe('Format email tidak valid.');
      }
    });
  });

  describe('registerSchema', () => {
    it('validates complete correct registration data', () => {
      const result = registerSchema.safeParse({
        fullName: 'Budi Santoso',
        email: 'budi@sportshop.dev',
        phoneNumber: '081234567890',
        password: 'Password123',
        confirmPassword: 'Password123',
      });
      expect(result.success).toBe(true);
    });

    it('enforces password complexity: min 8, uppercase, lowercase, digit', () => {
      const short = registerSchema.safeParse({
        fullName: 'Budi',
        email: 'budi@test.com',
        password: 'Pass1',
        confirmPassword: 'Pass1',
      });
      expect(short.success).toBe(false);

      const noUpper = registerSchema.safeParse({
        fullName: 'Budi',
        email: 'budi@test.com',
        password: 'password123',
        confirmPassword: 'password123',
      });
      expect(noUpper.success).toBe(false);

      const noDigit = registerSchema.safeParse({
        fullName: 'Budi',
        email: 'budi@test.com',
        password: 'PasswordAbc',
        confirmPassword: 'PasswordAbc',
      });
      expect(noDigit.success).toBe(false);
    });

    it('rejects mismatched password and confirmPassword', () => {
      const mismatch = registerSchema.safeParse({
        fullName: 'Budi',
        email: 'budi@test.com',
        password: 'Password123',
        confirmPassword: 'DifferentPassword123',
      });
      expect(mismatch.success).toBe(false);
      if (!mismatch.success) {
        expect(mismatch.error.issues[0]?.message).toBe('Konfirmasi kata sandi tidak cocok.');
      }
    });
  });

  describe('forgotPasswordSchema', () => {
    it('validates email format', () => {
      expect(forgotPasswordSchema.safeParse({ email: 'valid@mail.com' }).success).toBe(true);
      expect(forgotPasswordSchema.safeParse({ email: 'invalid' }).success).toBe(false);
    });
  });

  describe('resetPasswordSchema', () => {
    it('validates reset credentials and matching passwords', () => {
      const valid = resetPasswordSchema.safeParse({
        email: 'user@mail.com',
        token: 'token-abc',
        newPassword: 'NewPassword123',
        confirmPassword: 'NewPassword123',
      });
      expect(valid.success).toBe(true);

      const mismatch = resetPasswordSchema.safeParse({
        email: 'user@mail.com',
        token: 'token-abc',
        newPassword: 'NewPassword123',
        confirmPassword: 'WrongPassword123',
      });
      expect(mismatch.success).toBe(false);
    });
  });
});
