import React from 'react';
import { Plus, Edit2, Trash2, Loader2, X } from 'lucide-react';
import { useAdminCategories, adminCatalogApi, CreateCategoryInput } from '../api/adminCatalogApi';
import { Category } from '@/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/lib/toast';

export const AdminCategoriesPage: React.FC = () => {
  const { data: categories = [], isLoading, refetch } = useAdminCategories();
  const [isModalOpen, setIsModalOpen] = React.useState(false);
  const [editingCategory, setEditingCategory] = React.useState<Category | null>(null);

  const [formData, setFormData] = React.useState<CreateCategoryInput>({
    name: '',
    slug: '',
    parentId: null,
    sortOrder: 0,
    isActive: true,
  });

  const handleOpenCreate = () => {
    setEditingCategory(null);
    setFormData({
      name: '',
      slug: '',
      parentId: null,
      sortOrder: 0,
      isActive: true,
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (cat: Category) => {
    setEditingCategory(cat);
    setFormData({
      name: cat.name,
      slug: cat.slug,
      parentId: cat.parentId ?? null,
      sortOrder: cat.sortOrder,
      isActive: cat.isActive,
    });
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.slug.trim()) {
      toast.error('Nama dan slug kategori wajib diisi.');
      return;
    }

    try {
      if (editingCategory) {
        await adminCatalogApi.updateCategory(editingCategory.id, formData);
        toast.success('Kategori berhasil diperbarui.');
      } else {
        await adminCatalogApi.createCategory(formData);
        toast.success('Kategori baru berhasil dibuat.');
      }
      setIsModalOpen(false);
      void refetch();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menyimpan kategori.';
      toast.error(msg);
    }
  };

  const handleDelete = async (id: string) => {
    if (window.confirm('Yakin ingin menghapus kategori ini?')) {
      try {
        await adminCatalogApi.deleteCategory(id);
        toast.success('Kategori berhasil dihapus.');
        void refetch();
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Gagal menghapus kategori.';
        toast.error(msg);
      }
    }
  };

  // Only top-level categories can be parents (enforce max 2 levels)
  const rootCategories = categories.filter((c) => !c.parentId);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-foreground">Kelola Kategori</h1>
          <p className="text-xs sm:text-sm text-muted-foreground mt-1">
            Atur struktur kategori produk (maksimal hierarki 2 tingkat).
          </p>
        </div>
        <Button onClick={handleOpenCreate} className="self-start sm:self-auto text-xs sm:text-sm">
          <Plus className="mr-1.5 h-4 w-4" />
          Tambah Kategori
        </Button>
      </div>

      <div className="rounded-xl border bg-card overflow-hidden shadow-sm">
        {isLoading ? (
          <div className="py-20 flex justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        ) : categories.length === 0 ? (
          <div className="py-16 text-center text-muted-foreground text-sm">
            Belum ada kategori yang ditambahkan.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="bg-muted/40 border-b text-xs text-muted-foreground uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Nama Kategori</th>
                  <th className="py-3 px-4">Slug</th>
                  <th className="py-3 px-4">Induk (Parent)</th>
                  <th className="py-3 px-4">Urutan</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4 text-right">Aksi</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {categories.map((cat) => {
                  const parentName = categories.find((c) => c.id === cat.parentId)?.name;
                  return (
                    <tr key={cat.id} className="hover:bg-muted/30">
                      <td className="py-3 px-4 font-semibold text-foreground">
                        {cat.parentId ? `— ${cat.name}` : cat.name}
                      </td>
                      <td className="py-3 px-4 font-mono text-muted-foreground text-xs">{cat.slug}</td>
                      <td className="py-3 px-4 text-xs text-muted-foreground">
                        {parentName ? <span className="font-medium text-foreground">{parentName}</span> : 'Utama'}
                      </td>
                      <td className="py-3 px-4">{cat.sortOrder}</td>
                      <td className="py-3 px-4">
                        <span
                          className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold ${
                            cat.isActive
                              ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                              : 'bg-muted text-muted-foreground'
                          }`}
                        >
                          {cat.isActive ? 'Aktif' : 'Nonaktif'}
                        </span>
                      </td>
                      <td className="py-3 px-4 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleOpenEdit(cat)}
                            className="h-7 w-7 p-0"
                          >
                            <Edit2 className="h-3.5 w-3.5" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(cat.id)}
                            className="h-7 w-7 p-0 text-destructive hover:bg-destructive/10"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Add / Edit Category Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
          <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setIsModalOpen(false)} />
          <div className="relative z-50 w-full max-w-md rounded-xl border bg-background p-6 shadow-2xl">
            <div className="flex items-center justify-between border-b pb-3">
              <h2 className="text-base font-bold">
                {editingCategory ? 'Ubah Kategori' : 'Tambah Kategori'}
              </h2>
              <Button variant="ghost" size="sm" onClick={() => setIsModalOpen(false)} className="h-8 w-8 p-0">
                <X className="h-4 w-4" />
              </Button>
            </div>

            <form onSubmit={handleSave} className="mt-4 space-y-4 text-xs sm:text-sm">
              <div className="space-y-1.5">
                <Label htmlFor="catName">Nama Kategori *</Label>
                <Input
                  id="catName"
                  value={formData.name}
                  onChange={(e) => {
                    const name = e.target.value;
                    const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
                    setFormData({ ...formData, name, slug: editingCategory ? formData.slug : slug });
                  }}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="catSlug">Slug (URL) *</Label>
                <Input
                  id="catSlug"
                  value={formData.slug}
                  onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="catParent">Kategori Induk (Maks. 2 Tingkat)</Label>
                <select
                  id="catParent"
                  value={formData.parentId || ''}
                  onChange={(e) => setFormData({ ...formData, parentId: e.target.value || null })}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-xs"
                >
                  <option value="">Tidak Ada (Kategori Utama)</option>
                  {rootCategories
                    .filter((c) => !editingCategory || c.id !== editingCategory.id)
                    .map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                </select>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="catSort">Urutan Tampil</Label>
                <Input
                  id="catSort"
                  type="number"
                  value={formData.sortOrder}
                  onChange={(e) => setFormData({ ...formData, sortOrder: Number(e.target.value) })}
                />
              </div>

              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="catActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-input text-primary"
                />
                <Label htmlFor="catActive" className="font-normal cursor-pointer text-xs">
                  Kategori Aktif
                </Label>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t">
                <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>
                  Batal
                </Button>
                <Button type="submit">
                  {editingCategory ? 'Simpan Perubahan' : 'Buat Kategori'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminCategoriesPage;
