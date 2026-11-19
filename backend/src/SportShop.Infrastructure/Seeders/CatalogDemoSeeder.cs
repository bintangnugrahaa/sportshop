using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportShop.Application.Common.Interfaces;
using SportShop.Domain.Catalog;
using SportShop.Domain.Inventory;
using SportShop.Infrastructure.Persistence;

namespace SportShop.Infrastructure.Seeders;

public sealed partial class CatalogDemoSeeder : ICatalogDemoSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IClock _clock;
    private readonly ILogger<CatalogDemoSeeder> _logger;

    public CatalogDemoSeeder(
        AppDbContext dbContext,
        IConfiguration configuration,
        IClock clock,
        ILogger<CatalogDemoSeeder> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var isEnabled = _configuration.GetValue<bool>("SEED_DEMO_DATA")
            || _configuration.GetValue<bool>("SeedDemoData")
            || string.Equals(_configuration["SEED_DEMO_DATA"], "true", StringComparison.OrdinalIgnoreCase);

        if (!isEnabled)
        {
            return;
        }

        var existingCount = await _dbContext.Products.CountAsync(cancellationToken);
        if (existingCount >= 30)
        {
            LogAlreadySeeded(_logger, existingCount);
            return;
        }

        LogStartingSeeding(_logger);
        var now = _clock.UtcNow;

        // 1. Root & Sub Categories
        var rootCategories = new List<Category>
        {
            new(Guid.NewGuid(), "Sepatu", "sepatu", null, 1, true, now),
            new(Guid.NewGuid(), "Pakaian Olahraga", "pakaian-olahraga", null, 2, true, now),
            new(Guid.NewGuid(), "Peralatan Olahraga", "peralatan-olahraga", null, 3, true, now)
        };

        foreach (var rc in rootCategories)
        {
            var existing = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == rc.Slug, cancellationToken);
            if (existing is null)
            {
                await _dbContext.Categories.AddAsync(rc, cancellationToken);
            }
            else
            {
                rc.Id = existing.Id;
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        var subCategories = new List<Category>
        {
            new(Guid.NewGuid(), "Sepatu Lari", "sepatu-lari", rootCategories[0].Id, 1, true, now),
            new(Guid.NewGuid(), "Sepatu Futsal", "sepatu-futsal", rootCategories[0].Id, 2, true, now),
            new(Guid.NewGuid(), "Sepatu Basket", "sepatu-basket", rootCategories[0].Id, 3, true, now),

            new(Guid.NewGuid(), "Jersey Olahraga", "jersey-olahraga", rootCategories[1].Id, 1, true, now),
            new(Guid.NewGuid(), "Celana Training", "celana-training", rootCategories[1].Id, 2, true, now),
            new(Guid.NewGuid(), "Jaket Olahraga", "jaket-olahraga", rootCategories[1].Id, 3, true, now),

            new(Guid.NewGuid(), "Raket Badminton", "raket-badminton", rootCategories[2].Id, 1, true, now),
            new(Guid.NewGuid(), "Bola Olahraga", "bola-olahraga", rootCategories[2].Id, 2, true, now),
            new(Guid.NewGuid(), "Aksesoris Olahraga", "aksesoris-olahraga", rootCategories[2].Id, 3, true, now)
        };

        foreach (var sc in subCategories)
        {
            var existing = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Slug == sc.Slug, cancellationToken);
            if (existing is null)
            {
                await _dbContext.Categories.AddAsync(sc, cancellationToken);
            }
            else
            {
                sc.Id = existing.Id;
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 2. 8 Brands
        var brands = new List<Brand>
        {
            new(Guid.NewGuid(), "Nike", "nike", "/images/brands/nike.png", true, now),
            new(Guid.NewGuid(), "Adidas", "adidas", "/images/brands/adidas.png", true, now),
            new(Guid.NewGuid(), "Puma", "puma", "/images/brands/puma.png", true, now),
            new(Guid.NewGuid(), "Under Armour", "under-armour", "/images/brands/under-armour.png", true, now),
            new(Guid.NewGuid(), "Yonex", "yonex", "/images/brands/yonex.png", true, now),
            new(Guid.NewGuid(), "Li-Ning", "li-ning", "/images/brands/li-ning.png", true, now),
            new(Guid.NewGuid(), "Specs", "specs", "/images/brands/specs.png", true, now),
            new(Guid.NewGuid(), "Mills", "mills", "/images/brands/mills.png", true, now)
        };

        foreach (var b in brands)
        {
            var existing = await _dbContext.Brands.FirstOrDefaultAsync(x => x.Slug == b.Slug, cancellationToken);
            if (existing is null)
            {
                await _dbContext.Brands.AddAsync(b, cancellationToken);
            }
            else
            {
                b.Id = existing.Id;
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Category mapping helpers
        var catLari = subCategories[0].Id;
        var catFutsal = subCategories[1].Id;
        var catBasket = subCategories[2].Id;
        var catJersey = subCategories[3].Id;
        var catCelana = subCategories[4].Id;
        var catJaket = subCategories[5].Id;
        var catRaket = subCategories[6].Id;
        var catBola = subCategories[7].Id;
        var catAksesoris = subCategories[8].Id;

        var bNike = brands[0].Id;
        var bAdidas = brands[1].Id;
        var bPuma = brands[2].Id;
        var bUA = brands[3].Id;
        var bYonex = brands[4].Id;
        var bLiNing = brands[5].Id;
        var bSpecs = brands[6].Id;
        var bMills = brands[7].Id;

        // 3. 30 Products data definitions (Name, Slug, Desc, CatId, BrandId, BasePrice, Variants(size, color, price, weight, stock))
        var productSeeds = new (string Name, string Slug, string Desc, Guid CatId, Guid BrandId, decimal Price, (string? Size, string? Color, decimal Price, int Weight, int Stock)[] Variants)[]
        {
            ("Nike Air Zoom Pegasus 40", "nike-air-zoom-pegasus-40", "Sepatu lari responsif dengan bantalan empuk untuk lari sehari-hari.", catLari, bNike, 1_999_000m,
                [("40", "Hitam", 1_999_000m, 500, 15), ("41", "Hitam", 1_999_000m, 510, 20), ("42", "Putih", 1_999_000m, 520, 10), ("43", "Biru", 2_099_000m, 530, 8)]),

            ("Adidas Ultraboost Light", "adidas-ultraboost-light", "Sepatu lari ternyaman dengan teknologi bantalan Boost generasi terbaru.", catLari, bAdidas, 2_400_000m,
                [("41", "Core Black", 2_400_000m, 540, 12), ("42", "Core Black", 2_400_000m, 550, 18), ("43", "Cloud White", 2_500_000m, 560, 5)]),

            ("Puma Deviate Nitro 2", "puma-deviate-nitro-2", "Sepatu lari marathon dengan pelat karbon INNOPLATE untuk kecepatan maksimal.", catLari, bPuma, 2_199_000m,
                [("41", "Fireglow", 2_199_000m, 480, 8), ("42", "Fireglow", 2_199_000m, 490, 14), ("43", "Hitam", 2_199_000m, 500, 10)]),

            ("Specs Lightspeed 4 Pro Futsal", "specs-lightspeed-4-pro-futsal", "Sepatu futsal legendaris buatan lokal untuk kelincahan dan kontrol bola.", catFutsal, bSpecs, 599_000m,
                [("39", "Merah Putih", 599_000m, 450, 25), ("40", "Merah Putih", 599_000m, 460, 30), ("41", "Biru Emas", 599_000m, 470, 20), ("42", "Hitam", 599_000m, 480, 15)]),

            ("Adidas Samba Classic Indoor", "adidas-samba-classic-indoor", "Sepatu futsal klasik dengan sol karet gum ikonik dan kulit sintetis tahan lama.", catFutsal, bAdidas, 1_100_000m,
                [("40", "Black/White", 1_100_000m, 550, 10), ("41", "Black/White", 1_100_000m, 560, 12), ("42", "Black/White", 1_100_000m, 570, 8)]),

            ("Nike React Gato Futsal", "nike-react-gato-futsal", "Bantalan busa React dan pod kontrol sentuhan bola presisi di lapangan datar.", catFutsal, bNike, 1_799_000m,
                [("41", "Crimson", 1_799_000m, 500, 7), ("42", "Crimson", 1_799_000m, 510, 11), ("43", "Hitam", 1_799_000m, 520, 6)]),

            ("Nike LeBron Witness 8", "nike-lebron-witness-8", "Sepatu basket dengan unit Zoom Air di tumit untuk lompatan eksplosif.", catBasket, bNike, 1_650_000m,
                [("42", "Lakers Purple", 1_650_000m, 680, 9), ("43", "Lakers Purple", 1_650_000m, 700, 12), ("44", "Triple Black", 1_650_000m, 720, 6)]),

            ("Under Armour Curry Flow 10", "under-armour-curry-flow-10", "Daya cengkeram luar biasa tanpa karet luar untuk penembak jitu Stephen Curry.", catBasket, bUA, 2_399_000m,
                [("42", "Sour Patch", 2_399_000m, 620, 5), ("43", "Sour Patch", 2_399_000m, 640, 8), ("44", "Iron Sharpens", 2_499_000m, 660, 4)]),

            ("Puma Clyde Court Disrupt", "puma-clyde-court-disrupt", "Gaya retro dengan teknologi modern untuk kestabilan pergelangan kaki pemain basket.", catBasket, bPuma, 1_499_000m,
                [("41", "Orange/Yellow", 1_499_000m, 650, 7), ("42", "Orange/Yellow", 1_499_000m, 670, 10), ("43", "Hitam", 1_499_000m, 690, 8)]),

            ("Mills Timnas Indonesia Home Jersey", "mills-timnas-indonesia-home-jersey", "Jersey resmi kandang tim nasional Indonesia dengan teknologi jacquard bernapas.", catJersey, bMills, 499_000m,
                [("S", "Merah", 499_000m, 200, 30), ("M", "Merah", 499_000m, 210, 45), ("L", "Merah", 499_000m, 220, 50), ("XL", "Merah", 499_000m, 230, 25)]),

            ("Mills Timnas Indonesia Away Jersey", "mills-timnas-indonesia-away-jersey", "Jersey tandang resmi Garuda dengan warna putih elegan dan aksen hijau emas.", catJersey, bMills, 499_000m,
                [("M", "Putih", 499_000m, 210, 35), ("L", "Putih", 499_000m, 220, 40), ("XL", "Putih", 499_000m, 230, 20)]),

            ("Nike Dri-FIT Park VII Jersey", "nike-dri-fit-park-vii-jersey", "Bahan poliester 100% daur ulang cepat menyerap keringat untuk kenyamanan maksimal.", catJersey, bNike, 279_000m,
                [("S", "Biru Royal", 279_000m, 180, 20), ("M", "Biru Royal", 279_000m, 190, 25), ("L", "Hitam", 279_000m, 200, 30)]),

            ("Adidas Entrada 22 Jersey", "adidas-entrada-22-jersey", "Kaos latihan sepak bola dengan desain bersih dan teknologi AEROREADY pengontrol kelembapan.", catJersey, bAdidas, 290_000m,
                [("M", "Putih Garis Hitam", 290_000m, 190, 22), ("L", "Putih Garis Hitam", 290_000m, 200, 28), ("XL", "Hitam", 290_000m, 210, 18)]),

            ("Puma individualLIGA Warm Up Shirt", "puma-individualliga-warm-up-shirt", "Jersey pemanasan dengan motif grafis dinamis dan kain dryCELL berteknologi tinggi.", catJersey, bPuma, 359_000m,
                [("M", "Merah Neon", 359_000m, 200, 15), ("L", "Merah Neon", 359_000m, 210, 20), ("XL", "Hitam", 359_000m, 220, 12)]),

            ("Under Armour Tech 2.0 Short Sleeve", "under-armour-tech-20-short-sleeve", "Kain UA Tech cepat kering, ultra-lembut, dan memiliki sensasi alami di kulit.", catJersey, bUA, 399_000m,
                [("S", "Abu-Abu", 399_000m, 180, 16), ("M", "Abu-Abu", 399_000m, 190, 24), ("L", "Navy", 399_000m, 200, 20)]),

            ("Specs Swervo Pants Training", "specs-swervo-pants-training", "Celana panjang latihan futsal dan sepak bola dengan resleting di ujung pergelangan kaki.", catCelana, bSpecs, 249_000m,
                [("M", "Hitam", 249_000m, 320, 30), ("L", "Hitam", 249_000m, 340, 35), ("XL", "Hitam", 249_000m, 360, 20)]),

            ("Adidas Tiro 23 League Pants", "adidas-tiro-23-league-pants", "Celana training slim fit klasik sepak bola dengan 3 stripes di sisi samping.", catCelana, bAdidas, 650_000m,
                [("S", "Black/White", 650_000m, 330, 15), ("M", "Black/White", 650_000m, 350, 25), ("L", "Black/White", 650_000m, 370, 20)]),

            ("Nike Academy Pro Track Pants", "nike-academy-pro-track-pants", "Panel jala di bagian lutut untuk aliran udara sejuk saat intensitas latihan meningkat.", catCelana, bNike, 599_000m,
                [("M", "Navy", 599_000m, 340, 18), ("L", "Navy", 599_000m, 360, 22), ("XL", "Hitam", 599_000m, 380, 14)]),

            ("Nike Windrunner Running Jacket", "nike-windrunner-running-jacket", "Jaket lari tahan angin dan air ringan dengan ikon desain chevron 26 derajat legendaris.", catJaket, bNike, 1_199_000m,
                [("S", "Hitam/Putih", 1_199_000m, 380, 10), ("M", "Hitam/Putih", 1_199_000m, 400, 15), ("L", "Biru Langit", 1_199_000m, 420, 12)]),

            ("Adidas Marathon Jacket", "adidas-marathon-jacket", "Jaket lari tahan cuaca dengan saku ponsel anti keringat dan detail reflektif 360 derajat.", catJaket, bAdidas, 950_000m,
                [("M", "Kuning Terang", 950_000m, 390, 14), ("L", "Kuning Terang", 950_000m, 410, 18), ("XL", "Hitam", 950_000m, 430, 8)]),

            ("Puma teamFINAL Casuals Jacket", "puma-teamfinal-casuals-jacket", "Jaket olahraga kasual berbahan katun poliester premium untuk perjalanan tim dan istirahat.", catJaket, bPuma, 799_000m,
                [("M", "Abu Gelap", 799_000m, 450, 12), ("L", "Abu Gelap", 799_000m, 470, 16)]),

            ("Yonex Astrox 88D Pro", "yonex-astrox-88d-pro", "Raket bulutangkis serang untuk pemain ganda belakang dengan teknologi Rotational Generator System.", catRaket, bYonex, 2_450_000m,
                [("3U-G5", "Camel Gold", 2_450_000m, 88, 12), ("4U-G5", "Camel Gold", 2_450_000m, 83, 20)]),

            ("Yonex Nanoflare 1000Z", "yonex-nanoflare-1000z", "Raket bulutangkis pemecah rekor smash tercepat di dunia dengan profil rangka super tipis.", catRaket, bYonex, 2_650_000m,
                [("4U-G5", "Lightning Yellow", 2_650_000m, 83, 10), ("3U-G5", "Lightning Yellow", 2_650_000m, 88, 8)]),

            ("Li-Ning Axforce 80", "li-ning-axforce-80", "Raket andalan Chen Long dengan titik ayun kepala berat untuk daya smash yang mematikan.", catRaket, bLiNing, 2_150_000m,
                [("4U-G5", "Black Gold", 2_150_000m, 84, 15), ("5U-G6", "Black Gold", 2_150_000m, 79, 10)]),

            ("Li-Ning Windstorm 72", "li-ning-windstorm-72", "Raket bulutangkis super ringan 72 gram untuk pemain yang mengutamakan kecepatan reaksi dan manuver.", catRaket, bLiNing, 1_350_000m,
                [("6U-G6", "Ungu Silver", 1_350_000m, 72, 18), ("6U-G6", "Biru Tosca", 1_350_000m, 72, 14)]),

            ("Nike Flight Football Official Match Ball", "nike-flight-football-official-match-ball", "Bola sepak resmi FIFA Quality Pro dengan alur Aerowsculpt revolusioner.", catBola, bNike, 1_899_000m,
                [("Size 5", "Putih/Biru/Laser", 1_899_000m, 440, 15)]),

            ("Adidas Tiro League TB Football", "adidas-tiro-league-tb-football", "Bola sepak jahitan thermal bonded tanpa sambungan benang untuk sentuhan bola presisi.", catBola, bAdidas, 480_000m,
                [("Size 4", "Putih/Hitam", 440_000m, 390, 20), ("Size 5", "Putih/Hitam", 480_000m, 430, 30)]),

            ("Puma Orbita Serie A Match Ball", "puma-orbita-serie-a-match-ball", "Bola sepak resmi kompetisi Serie A Italia dengan konstruksi 12 panel unik bertekanan stabil.", catBola, bPuma, 1_399_000m,
                [("Size 5", "Puma White", 1_399_000m, 440, 10)]),

            ("Under Armour Steph Curry Basketball 7", "under-armour-steph-curry-basketball-7", "Bola basket komposit premium untuk penggunaan indoor dan outdoor dengan cengkeraman dalam.", catBola, bUA, 549_000m,
                [("Size 7", "Oranye/Biru", 549_000m, 620, 25)]),

            ("Yonex Super Grap Grip AC102EX", "yonex-super-grap-grip-ac102ex", "Overgrip raket badminton paling populer di dunia dengan daya serap keringat tinggi.", catAksesoris, bYonex, 45_000m,
                [("Isi 3", "Putih", 45_000m, 40, 100), ("Isi 3", "Hitam", 45_000m, 40, 120), ("Isi 3", "Kuning Neon", 45_000m, 40, 80)])
        };

        var prodIndex = 1;
        foreach (var pData in productSeeds)
        {
            var currentProdIndex = prodIndex++;
            var existingProd = await _dbContext.Products.FirstOrDefaultAsync(p => p.Slug == pData.Slug, cancellationToken);
            if (existingProd is not null)
            {
                continue;
            }

            var minPrice = pData.Variants.Min(v => v.Price);
            var prodId = Guid.NewGuid();
            var product = new Product(
                prodId,
                pData.CatId,
                pData.BrandId,
                pData.Name,
                pData.Slug,
                pData.Desc,
                minPrice,
                true,
                now);

            await _dbContext.Products.AddAsync(product, cancellationToken);

            var primaryImg = new ProductImage(
                Guid.NewGuid(),
                prodId,
                $"/images/products/{pData.Slug}-primary.jpg",
                pData.Name,
                0,
                true);
            await _dbContext.ProductImages.AddAsync(primaryImg, cancellationToken);

            var skuIndex = 1;
            foreach (var vData in pData.Variants)
            {
                var variantId = Guid.NewGuid();
                var sku = $"DEMO-{currentProdIndex:D2}-{skuIndex++:D2}";
                var variant = new ProductVariant(
                    variantId,
                    prodId,
                    sku,
                    vData.Price,
                    vData.Weight,
                    vData.Stock,
                    vData.Size,
                    vData.Color,
                    true,
                    now);

                await _dbContext.ProductVariants.AddAsync(variant, cancellationToken);

                if (vData.Stock > 0)
                {
                    var movement = new InventoryMovement(
                        Guid.NewGuid(),
                        variantId,
                        vData.Stock,
                        StockReason.Initial,
                        "Inisialisasi data katalog demo",
                        null,
                        null,
                        now);
                    await _dbContext.InventoryMovements.AddAsync(movement, cancellationToken);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        LogSeededSuccessfully(_logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Catalog demo seeding skipped. Products table already contains {Count} items.")]
    private static partial void LogAlreadySeeded(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting catalog demo data seeding (30 products)...")]
    private static partial void LogStartingSeeding(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Catalog demo data (30 products, categories, brands, variants, stock) seeded successfully.")]
    private static partial void LogSeededSuccessfully(ILogger logger);
}
