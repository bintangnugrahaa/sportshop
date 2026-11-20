import React from 'react';
import { Plus, Edit2, Trash2, Loader2, X } from 'lucide-react';
import { useAdminBrands, adminCatalogApi, CreateBrandInput } from '../api/adminCatalogApi';
import { Brand } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/lib/toast';

export const AdminBrandsPage: React.FC = () => {
  const { data: brands = [], isLoading, refetch } = useAdminBrands();
  const [isModalOpen, setIsModalOpen] = React.useState(false);
  const [editingBrand, setEditingBrand] = React.useState<Brand | null>(null);

  const [formData, setFormData] = React.useState<CreateBrandInput>({
    name: '',
    slug: '',
    logoUrl: '',
    isActive: true,
  });

  const handleOpenCreate = () => {
    setEditingBrand(null);
    setFormData({
      name: '',
      slug: '',
      logoUrl: '',
      isActive: true,
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (brand: Brand) => {
    setEditingBrand(brand);
    setFormData({
      name: brand.name,
      slug: brand.slug,
      logoUrl: brand.logoUrl || '',
      isActive: brand.isActive,
    });
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.slug.trim()) {
      toast.error('Nama dan slug brand wajib diisi.');
      return;
    }

    try {
      if (editingBrand) {
        await adminCatalogApi.updateBrand(editingBrand.id, formData);
        toast.success('Brand berhasil diperbarui.');
      } else {
        await adminCatalogApi.createBrand(formData);
        toast.success('Brand baru berhasil dibuat.');
      }
      setIsModalOpen(false);
      void refetch();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menyimpan brand.';
      toast.error(msg);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Yakin ingin menghapus brand ini?')) {
      try {
        await adminCatalogApi.deleteBrand(id);
        toast.success('Brand berhasil dihapus.');
        void refetch();
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Gagal menghapus brand.';
        toast.error(msg);
      }
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-foreground">Kelola Brand</h1>
          <p className="text-xs sm:text-sm text-muted-foreground mt-1">
            Daftar merek/brand perlengkapan olahraga yang dijual di SportShop.
          </p>
        </div>
        <Button onClick={handleOpenCreate} className="self-start sm:self-auto text-xs sm:text-sm">
          <Plus className="mr-1.5 h-4 w-4" />
          Tambah Brand
        </Button>
      </div>

      <div className="rounded-xl border bg-card overflow-hidden shadow-sm">
        {isLoading ? (
          <div className="py-20 flex justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        ) : brands.length === 0 ? (
          <div className="py-16 text-center text-muted-foreground text-sm">
            Belum ada brand terdaftar.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="bg-muted/40 border-b text-xs text-muted-foreground uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Nama Brand</th>
                  <th className="py-3 px-4">Slug</th>
                  <th className="py-3 px-4">Logo</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4 text-right">Aksi</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {brands.map((b) => (
                  <tr key={b.id} className="hover:bg-muted/30">
                    <td className="py-3 px-4 font-semibold text-foreground">{b.name}</td>
                    <td className="py-3 px-4 font-mono text-muted-foreground text-xs">{b.slug}</td>
                    <td className="py-3 px-4 text-xs text-muted-foreground">
                      {b.logoUrl ? (
                        <img src={b.logoUrl} alt={b.name} className="h-6 object-contain" />
                      ) : (
                        '-'
                      )}
                    </td>
                    <td className="py-3 px-4">
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold ${
                          b.isActive
                            ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                            : 'bg-muted text-muted-foreground'
                        }`}
                      >
                        {b.isActive ? 'Aktif' : 'Nonaktif'}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleOpenEdit(b)}
                          className="h-7 w-7 p-0"
                        >
                          <Edit2 className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(b.id)}
                          className="h-7 w-7 p-0 text-destructive hover:bg-destructive/10"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Add / Edit Brand Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
          <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setIsModalOpen(false)} />
          <div className="relative z-50 w-full max-w-md rounded-xl border bg-background p-6 shadow-2xl">
            <div className="flex items-center justify-between border-b pb-3">
              <h2 className="text-base font-bold">
                {editingBrand ? 'Ubah Brand' : 'Tambah Brand'}
              </h2>
              <Button variant="ghost" size="sm" onClick={() => setIsModalOpen(false)} className="h-8 w-8 p-0">
                <X className="h-4 w-4" />
              </Button>
            </div>

            <form onSubmit={handleSave} className="mt-4 space-y-4 text-xs sm:text-sm">
              <div className="space-y-1.5">
                <Label htmlFor="brandName">Nama Brand *</Label>
                <Input
                  id="brandName"
                  value={formData.name}
                  onChange={(e) => {
                    const name = e.target.value;
                    const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
                    setFormData({ ...formData, name, slug: editingBrand ? formData.slug : slug });
                  }}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="brandSlug">Slug (URL) *</Label>
                <Input
                  id="brandSlug"
                  value={formData.slug}
                  onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="brandLogo">URL Logo (opsional)</Label>
                <Input
                  id="brandLogo"
                  placeholder="https://example.com/logo.png"
                  value={formData.logoUrl || ''}
                  onChange={(e) => setFormData({ ...formData, logoUrl: e.target.value })}
                />
              </div>

              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="brandActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-input text-primary"
                />
                <Label htmlFor="brandActive" className="font-normal cursor-pointer text-xs">
                  Brand Aktif
                </Label>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t">
                <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>
                  Batal
                </Button>
                <Button type="submit">
                  {editingBrand ? 'Simpan Perubahan' : 'Buat Brand'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminBrandsPage;
