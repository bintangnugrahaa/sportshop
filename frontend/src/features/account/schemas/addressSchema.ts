import { z } from 'zod';

export const addressSchema = z.object({
  label: z
    .string()
    .min(1, 'Label alamat wajib diisi.')
    .max(50, 'Label alamat maksimal 50 karakter.'),
  recipientName: z
    .string()
    .min(1, 'Nama penerima wajib diisi.')
    .max(150, 'Nama penerima maksimal 150 karakter.'),
  phone: z
    .string()
    .min(1, 'Nomor telepon wajib diisi.')
    .max(20, 'Nomor telepon maksimal 20 karakter.')
    .regex(/^[0-9+() -]+$/, 'Format nomor telepon tidak valid.'),
  provinceCode: z
    .string()
    .min(1, 'Provinsi wajib dipilih.')
    .max(10, 'Kode provinsi maksimal 10 karakter.'),
  city: z
    .string()
    .min(1, 'Kota/kabupaten wajib diisi.')
    .max(100, 'Kota/kabupaten maksimal 100 karakter.'),
  district: z
    .string()
    .max(100, 'Kecamatan maksimal 100 karakter.')
    .optional()
    .or(z.literal('')),
  postalCode: z
    .string()
    .min(1, 'Kode pos wajib diisi.')
    .max(10, 'Kode pos maksimal 10 karakter.')
    .regex(/^[0-9]+$/, 'Kode pos harus berupa angka.'),
  street: z
    .string()
    .min(1, 'Alamat jalan wajib diisi.')
    .max(300, 'Alamat jalan maksimal 300 karakter.'),
  isDefault: z.boolean().default(false),
});

export type AddressFormData = z.infer<typeof addressSchema>;
