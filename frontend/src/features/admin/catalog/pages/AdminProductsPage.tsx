import React from 'react';
import {
  Plus,
  Edit2,
  Trash2,
  Image as ImageIcon,
  Loader2,
  X,
  Upload,
} from 'lucide-react';
import {
  useAdminProducts,
  useAdminCategories,
  useAdminBrands,
  adminCatalogApi,
  CreateProductInput,
} from '../api/adminCatalogApi';
import { Product, ProductImage } from '@/types';
import { Price } from '@/components/shared/Price';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { toast } from '@/lib/toast';

export const AdminProductsPage: React.FC = () => {
  const [page] = React.useState(1);
  const [selectedProductForImages, setSelectedProductForImages] = React.useState<Product | null>(null);
  const [isProductModalOpen, setIsProductModalOpen] = React.useState(false);
  const [editingProduct, setEditingProduct] = React.useState<Product | null>(null);

  const { data, isLoading, refetch } = useAdminProducts({ page, pageSize: 15 });
  const { data: categories = [] } = useAdminCategories();
  const { data: brands = [] } = useAdminBrands();

  // Product Form State
  const [formData, setFormData] = React.useState<CreateProductInput>({
    name: '',
    slug: '',
    categoryId: '',
    brandId: '',
    basePrice: 0,
    description: '',
    isActive: true,
  });

  const handleOpenCreate = () => {
    setEditingProduct(null);
    setFormData({
      name: '',
      slug: '',
      categoryId: categories[0]?.id || '',
      brandId: brands[0]?.id || '',
      basePrice: 0,
      description: '',
      isActive: true,
      variants: [
        {
          sku: '',
          optionSize: '',
          optionColor: '',
          price: 0,
          weightGram: 500,
          stockQuantity: 10,
          isActive: true,
        },
      ],
    });
    setIsProductModalOpen(true);
  };

  const handleOpenEdit = (p: Product) => {
    setEditingProduct(p);
    setFormData({
      name: p.name,
      slug: p.slug,
      categoryId: p.categoryId,
      brandId: p.brandId,
      basePrice: p.basePrice,
      description: p.description,
      isActive: p.isActive,
    });
    setIsProductModalOpen(true);
  };

  const handleSaveProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim() || !formData.slug.trim()) {
      toast.error('Nama dan slug produk wajib diisi.');
      return;
    }
    if (!formData.categoryId || !formData.brandId) {
      toast.error('Kategori dan brand wajib dipilih.');
      return;
    }

    try {
      if (editingProduct) {
        await adminCatalogApi.updateProduct(editingProduct.id, {
          categoryId: formData.categoryId,
          brandId: formData.brandId,
          name: formData.name,
          slug: formData.slug,
          description: formData.description,
          isActive: formData.isActive,
        });
        toast.success('Produk berhasil diperbarui.');
      } else {
        await adminCatalogApi.createProduct(formData);
        toast.success('Produk baru berhasil dibuat.');
      }
      setIsProductModalOpen(false);
      void refetch();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal menyimpan produk.';
      toast.error(msg);
    }
  };

  const handleDeleteProduct = async (id: string) => {
    if (window.confirm('Yakin ingin menonaktifkan/menghapus produk ini?')) {
      try {
        await adminCatalogApi.deleteProduct(id);
        toast.success('Produk berhasil dihapus.');
        void refetch();
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Gagal menghapus produk.';
        toast.error(msg);
      }
    }
  };

  // Image Upload handler
  const [uploadFile, setUploadFile] = React.useState<File | null>(null);
  const [altText, setAltText] = React.useState('');
  const [isPrimary, setIsPrimary] = React.useState(false);
  const [isUploading, setIsUploading] = React.useState(false);

  const handleUploadImage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProductForImages) return;
    if (!uploadFile) {
      toast.error('Pilih file gambar terlebih dahulu.');
      return;
    }

    // Validation: 2MB and format
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(uploadFile.type)) {
      toast.error('Format gambar harus JPG, PNG, atau WebP.');
      return;
    }
    if (uploadFile.size > 2 * 1024 * 1024) {
      toast.error('Ukuran file gambar maksimal 2 MB.');
      return;
    }

    setIsUploading(true);
    const fd = new FormData();
    fd.append('file', uploadFile);
    fd.append('altText', altText);
    fd.append('isPrimary', String(isPrimary));
    fd.append('sortOrder', '0');

    try {
      await adminCatalogApi.uploadImage(selectedProductForImages.id, fd);
      toast.success('Gambar berhasil diunggah.');
      setUploadFile(null);
      setAltText('');
      // refresh product images
      const updated = await adminCatalogApi.getProduct(selectedProductForImages.id);
      setSelectedProductForImages(updated);
      void refetch();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Gagal mengunggah gambar.';
      toast.error(msg);
    } finally {
      setIsUploading(false);
    }
  };

  const handleDeleteImage = async (imageId: string) => {
    if (!selectedProductForImages) return;
    if (window.confirm('Hapus gambar ini?')) {
      try {
        await adminCatalogApi.deleteImage(selectedProductForImages.id, imageId);
        toast.success('Gambar berhasil dihapus.');
        const updated = await adminCatalogApi.getProduct(selectedProductForImages.id);
        setSelectedProductForImages(updated);
        void refetch();
      } catch (err: unknown) {
        const msg = err instanceof Error ? err.message : 'Gagal menghapus gambar.';
        toast.error(msg);
      }
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-foreground">Kelola Produk</h1>
          <p className="text-xs sm:text-sm text-muted-foreground mt-1">
            Daftar seluruh katalog produk olahraga beserta varian ukuran, warna, dan gambar.
          </p>
        </div>
        <Button onClick={handleOpenCreate} className="self-start sm:self-auto text-xs sm:text-sm">
          <Plus className="mr-1.5 h-4 w-4" />
          Tambah Produk Baru
        </Button>
      </div>

      {/* Products Table */}
      <div className="rounded-xl border bg-card overflow-hidden shadow-sm">
        {isLoading ? (
          <div className="py-20 flex justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        ) : !data || data.items.length === 0 ? (
          <div className="py-16 text-center text-muted-foreground text-sm">
            Belum ada produk terdaftar.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="bg-muted/40 border-b text-xs text-muted-foreground uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Produk</th>
                  <th className="py-3 px-4">Kategori / Brand</th>
                  <th className="py-3 px-4">Harga Dasar</th>
                  <th className="py-3 px-4">Status</th>
                  <th className="py-3 px-4 text-right">Aksi</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {data.items.map((prod) => (
                  <tr key={prod.id} className="hover:bg-muted/30">
                    <td className="py-3 px-4">
                      <div className="flex items-center gap-3">
                        {prod.primaryImageUrl ? (
                          <img
                            src={prod.primaryImageUrl}
                            alt={prod.name}
                            className="h-10 w-10 rounded object-cover border bg-muted"
                          />
                        ) : (
                          <div className="h-10 w-10 rounded border bg-muted flex items-center justify-center text-[10px] text-muted-foreground">
                            No Img
                          </div>
                        )}
                        <div>
                          <p className="font-semibold text-foreground">{prod.name}</p>
                          <p className="text-[11px] font-mono text-muted-foreground">{prod.slug}</p>
                        </div>
                      </div>
                    </td>
                    <td className="py-3 px-4 text-xs">
                      <p className="font-medium text-foreground">{prod.categoryName || '-'}</p>
                      <p className="text-muted-foreground">{prod.brandName || '-'}</p>
                    </td>
                    <td className="py-3 px-4">
                      <Price price={prod.basePrice} className="font-semibold text-sm" />
                    </td>
                    <td className="py-3 px-4">
                      <span
                        className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold ${
                          prod.isActive
                            ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                            : 'bg-muted text-muted-foreground'
                        }`}
                      >
                        {prod.isActive ? 'Aktif' : 'Nonaktif'}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setSelectedProductForImages(prod)}
                          className="h-7 px-2 text-xs"
                          title="Kelola Gambar"
                        >
                          <ImageIcon className="h-3.5 w-3.5 mr-1" />
                          <span>Gambar</span>
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleOpenEdit(prod)}
                          className="h-7 w-7 p-0"
                          title="Ubah Produk"
                        >
                          <Edit2 className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteProduct(prod.id)}
                          className="h-7 w-7 p-0 text-destructive hover:bg-destructive/10"
                          title="Hapus Produk"
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

      {/* Add / Edit Product Modal */}
      {isProductModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
          <div
            className="fixed inset-0 bg-black/60 backdrop-blur-sm"
            onClick={() => setIsProductModalOpen(false)}
          />
          <div className="relative z-50 w-full max-w-lg rounded-xl border bg-background p-6 shadow-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between border-b pb-3">
              <h2 className="text-base font-bold">
                {editingProduct ? 'Ubah Produk' : 'Tambah Produk Baru'}
              </h2>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setIsProductModalOpen(false)}
                className="h-8 w-8 p-0"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            <form onSubmit={handleSaveProduct} className="mt-4 space-y-4 text-xs sm:text-sm">
              <div className="space-y-1.5">
                <Label htmlFor="prodName">Nama Produk *</Label>
                <Input
                  id="prodName"
                  value={formData.name}
                  onChange={(e) => {
                    const name = e.target.value;
                    const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
                    setFormData({ ...formData, name, slug: editingProduct ? formData.slug : slug });
                  }}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="prodSlug">Slug (URL) *</Label>
                <Input
                  id="prodSlug"
                  value={formData.slug}
                  onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <Label htmlFor="prodCategory">Kategori *</Label>
                  <select
                    id="prodCategory"
                    value={formData.categoryId}
                    onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-xs"
                    required
                  >
                    <option value="">Pilih Kategori</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="prodBrand">Brand *</Label>
                  <select
                    id="prodBrand"
                    value={formData.brandId}
                    onChange={(e) => setFormData({ ...formData, brandId: e.target.value })}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-xs"
                    required
                  >
                    <option value="">Pilih Brand</option>
                    {brands.map((b) => (
                      <option key={b.id} value={b.id}>
                        {b.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="prodBasePrice">Harga Dasar (Rp) *</Label>
                <Input
                  id="prodBasePrice"
                  type="number"
                  min="0"
                  value={formData.basePrice}
                  onChange={(e) => setFormData({ ...formData, basePrice: Number(e.target.value) })}
                  required
                />
              </div>

              <div className="space-y-1.5">
                <Label htmlFor="prodDesc">Deskripsi Produk</Label>
                <textarea
                  id="prodDesc"
                  rows={3}
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-xs"
                />
              </div>

              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  id="prodActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4 rounded border-input text-primary"
                />
                <Label htmlFor="prodActive" className="font-normal cursor-pointer text-xs">
                  Produk Aktif (dapat dilihat pelanggan di katalog)
                </Label>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t">
                <Button type="button" variant="outline" onClick={() => setIsProductModalOpen(false)}>
                  Batal
                </Button>
                <Button type="submit">
                  {editingProduct ? 'Simpan Perubahan' : 'Buat Produk'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Image Manager Modal */}
      {selectedProductForImages && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4" role="dialog" aria-modal="true">
          <div
            className="fixed inset-0 bg-black/60 backdrop-blur-sm"
            onClick={() => setSelectedProductForImages(null)}
          />
          <div className="relative z-50 w-full max-w-lg rounded-xl border bg-background p-6 shadow-2xl max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between border-b pb-3">
              <div>
                <h2 className="text-base font-bold">Galeri Gambar Produk</h2>
                <p className="text-xs text-muted-foreground">{selectedProductForImages.name}</p>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSelectedProductForImages(null)}
                className="h-8 w-8 p-0"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            {/* Upload Form */}
            <form onSubmit={handleUploadImage} className="mt-4 p-4 rounded-lg border bg-muted/20 space-y-3">
              <h4 className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Upload className="h-3.5 w-3.5 text-primary" />
                <span>Unggah Gambar Baru (Maks 2 MB, JPG/PNG/WebP)</span>
              </h4>
              <Input
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={(e) => setUploadFile(e.target.files?.[0] || null)}
                className="text-xs"
              />
              <Input
                placeholder="Alt text gambar (opsional)"
                value={altText}
                onChange={(e) => setAltText(e.target.value)}
                className="text-xs"
              />
              <div className="flex items-center justify-between pt-1">
                <label className="flex items-center gap-2 text-xs cursor-pointer">
                  <input
                    type="checkbox"
                    checked={isPrimary}
                    onChange={(e) => setIsPrimary(e.target.checked)}
                    className="h-3.5 w-3.5 text-primary rounded"
                  />
                  <span>Jadikan Gambar Utama</span>
                </label>
                <Button type="submit" size="sm" disabled={isUploading} className="text-xs">
                  {isUploading && <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />}
                  Unggah
                </Button>
              </div>
            </form>

            {/* Existing Images Grid */}
            <div className="mt-5 space-y-2">
              <h4 className="text-xs font-semibold text-muted-foreground">Gambar Tersimpan</h4>
              {!selectedProductForImages.images || selectedProductForImages.images.length === 0 ? (
                <p className="text-xs text-muted-foreground py-4 text-center">
                  Belum ada gambar untuk produk ini.
                </p>
              ) : (
                <div className="grid grid-cols-3 gap-3">
                  {selectedProductForImages.images.map((img: ProductImage) => (
                    <div key={img.id} className="relative rounded-lg border overflow-hidden group bg-muted">
                      <img
                        src={img.url}
                        alt={img.altText || ''}
                        className="h-28 w-full object-cover"
                      />
                      {img.isPrimary && (
                        <span className="absolute top-1 left-1 bg-primary text-white text-[9px] font-bold px-1.5 py-0.5 rounded">
                          Utama
                        </span>
                      )}
                      <button
                        type="button"
                        onClick={() => handleDeleteImage(img.id)}
                        className="absolute top-1 right-1 bg-rose-600 text-white rounded p-1 opacity-0 group-hover:opacity-100 transition-opacity"
                        title="Hapus gambar"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminProductsPage;
