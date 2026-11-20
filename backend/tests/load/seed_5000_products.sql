DO $$
DECLARE
    v_cat_ids uuid[];
    v_brand_ids uuid[];
    v_now timestamptz := now();
BEGIN
    SELECT array_agg(id) INTO v_cat_ids FROM categories WHERE parent_id IS NOT NULL;
    SELECT array_agg(id) INTO v_brand_ids FROM brands;

    INSERT INTO products (id, category_id, brand_id, name, slug, description, base_price, is_active, created_at)
    SELECT
        gen_random_uuid(),
        v_cat_ids[1 + (i % array_length(v_cat_ids, 1))],
        v_brand_ids[1 + (i % array_length(v_brand_ids, 1))],
        'Perlengkapan Olahraga Model #' || i,
        'perlengkapan-olahraga-model-' || i,
        'Deskripsi lengkap untuk perlengkapan olahraga model performa tinggi #' || i,
        (100000 + (i % 50) * 25000)::numeric(18,2),
        true,
        v_now
    FROM generate_series(31, 5000) AS i;

    INSERT INTO product_variants (id, product_id, sku, price, weight_gram, stock_quantity, option_size, option_color, is_active, created_at)
    SELECT
        gen_random_uuid(),
        p.id,
        'SKU-GEN-' || i || '-A',
        p.base_price,
        500,
        20,
        'M',
        'Hitam',
        true,
        v_now
    FROM products p
    CROSS JOIN LATERAL (SELECT substring(p.slug from '[0-9]+') AS i) s
    WHERE p.slug LIKE 'perlengkapan-olahraga-model-%';

    INSERT INTO product_variants (id, product_id, sku, price, weight_gram, stock_quantity, option_size, option_color, is_active, created_at)
    SELECT
        gen_random_uuid(),
        p.id,
        'SKU-GEN-' || i || '-B',
        p.base_price + 20000,
        520,
        15,
        'L',
        'Putih',
        true,
        v_now
    FROM products p
    CROSS JOIN LATERAL (SELECT substring(p.slug from '[0-9]+') AS i) s
    WHERE p.slug LIKE 'perlengkapan-olahraga-model-%';
END $$;
ANALYZE products;
ANALYZE product_variants;
