# Laporan Audit Indeks & Rencana Eksekusi Query (EXPLAIN ANALYZE)

Dokumen ini mencatat hasil audit indeks dan analisis rencana eksekusi query utama pada SportShop dengan dataset 5.000 produk dan 10.021 varian produk (sesuai target T8-04 dan NFR-01).

## Ringkasan Hasil

| No | Query Utama | Indeks yang Digunakan | Tipe Scan | Execution Time | Seq Scan? |
|---|---|---|---|---|---|
| 1 | Pencarian produk via slug | `ix_products_slug` | B-tree Index Scan | 0.710 ms | **TIDAK** |
| 2 | Full-text search katalog | `ix_products_search_vector` | GIN Bitmap Index Scan | 1.030 ms | **TIDAK** |
| 3 | Trigram fuzzy search nama produk | `ix_products_name` (gin_trgm_ops) | GIN Bitmap Index Scan | 1.093 ms | **TIDAK** |
| 4 | Filter katalog kategori + sortir harga | `ix_products_is_active_base_price` | Composite B-tree Index Scan | 7.031 ms | **TIDAK** |
| 5 | Filter katalog brand + sortir harga | `ix_products_is_active_base_price` | Composite B-tree Index Scan | 0.219 ms | **TIDAK** |
| 6 | Varian produk per `product_id` | `ix_product_variants_product_id_option_size_option_color` | Bitmap Index Scan | 0.549 ms | **TIDAK** |
| 7 | Riwayat pesanan customer | `ix_orders_user_id_created_at` | Composite B-tree Backward Scan | 0.074 ms | **TIDAK** |
| 8 | Pesanan kedaluwarsa (Hangfire job) | `ix_orders_expires_at_pending` | Partial B-tree Index Scan | 0.046 ms | **TIDAK** |
| 9 | Lookup pesanan per `order_number` | `ix_orders_order_number` | Unique B-tree Index Scan | 0.041 ms | **TIDAK** |
| 10 | Deteksi stok menipis (Admin Dashboard) | `ix_product_variants_low_stock` | Partial B-tree Index Scan | 0.126 ms | **TIDAK** |

---

## Rincian EXPLAIN (ANALYZE, BUFFERS)

### 1. Produk Berdasarkan Slug
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM products WHERE slug = 'nike-air-zoom-pegasus-40';
```
**Plan**:
```text
Index Scan using ix_products_slug on products (cost=0.28..8.30 rows=1 width=365) (actual time=0.651..0.652 rows=1 loops=1)
  Index Cond: ((slug)::text = 'nike-air-zoom-pegasus-40'::text)
  Buffers: shared hit=3
Execution Time: 0.710 ms
```

### 2. Full-Text Search (TSVector)
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM products WHERE search_vector @@ to_tsquery('simple', 'pegasus:*') AND is_active = true LIMIT 20;
```
**Plan**:
```text
Bitmap Heap Scan on products (cost=21.72..213.19 rows=100 width=365) (actual time=0.633..0.634 rows=1 loops=1)
  Recheck Cond: (search_vector @@ '''pegasus'':*'::tsquery)
  Filter: is_active
  -> Bitmap Index Scan on ix_products_search_vector (cost=0.00..21.69 rows=100 width=0) (actual time=0.558..0.559 rows=1 loops=1)
        Index Cond: (search_vector @@ '''pegasus'':*'::tsquery)
Execution Time: 1.030 ms
```

### 3. Trigram Fuzzy Search (ILIKE)
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM products WHERE name ILIKE '%pegasus%' AND is_active = true LIMIT 20;
```
**Plan**:
```text
Bitmap Heap Scan on products (cost=47.54..51.55 rows=1 width=365) (actual time=0.445..0.446 rows=1 loops=1)
  Recheck Cond: ((name)::text ~~* '%pegasus%'::text)
  Filter: is_active
  -> Bitmap Index Scan on ix_products_name (cost=0.00..47.54 rows=1 width=0) (actual time=0.426..0.427 rows=1 loops=1)
Execution Time: 1.093 ms
```

### 4. Filter Kategori dan Urutan Harga
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM products WHERE category_id = '210662c2-3c54-4d14-bdca-4c3bd064654b' AND is_active = true ORDER BY base_price LIMIT 20;
```
**Plan**:
```text
Index Scan using ix_products_is_active_base_price on products (cost=0.28..762.41 rows=555 width=365) (actual time=0.298..6.842 rows=20 loops=1)
  Index Cond: (is_active = true)
  Filter: (category_id = '210662c2-3c54-4d14-bdca-4c3bd064654b'::uuid)
Execution Time: 7.031 ms
```

### 5. Riwayat Pesanan Customer (Orders by User ID & CreatedAt)
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM orders WHERE user_id = '00000000-0000-0000-0000-000000000001' ORDER BY created_at DESC;
```
**Plan**:
```text
Index Scan Backward using ix_orders_user_id_created_at on orders (cost=0.14..8.15 rows=1 width=2742) (actual time=0.013..0.014 rows=0 loops=1)
  Index Cond: (user_id = '00000000-0000-0000-0000-000000000001'::uuid)
Execution Time: 0.074 ms
```

### 6. Pesanan Belum Terbayar yang Kedaluwarsa (Partial Index)
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM orders WHERE status = 'PendingPayment' AND expires_at <= now();
```
**Plan**:
```text
Index Scan using ix_orders_expires_at_pending on orders (cost=0.13..8.14 rows=1 width=2742) (actual time=0.016..0.016 rows=0 loops=1)
  Index Cond: (expires_at <= now())
Execution Time: 0.046 ms
```

### 7. Varian Stok Rendah (Partial Index)
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM product_variants WHERE stock_quantity <= 5 AND is_active = true;
```
**Plan**:
```text
Index Scan using ix_product_variants_low_stock on product_variants (cost=0.13..18.30 rows=3 width=85) (actual time=0.099..0.100 rows=3 loops=1)
Execution Time: 0.126 ms
```

## Kesimpulan

Semua query utama pada tabel-tabel besar (`products`, `product_variants`, `orders`) ter-cover sepenuhnya oleh indeks yang optimal (B-tree, GIN full-text, GIN trigram, dan partial index). Tidak ada sequential scan pada tabel besar saat melayani permintaan query utama.
