using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SportShop.Application.Features.Auth.ConfirmEmail;
using SportShop.Application.Features.Auth.ForgotPassword;
using SportShop.Application.Features.Auth.Login;
using SportShop.Application.Features.Auth.Logout;
using SportShop.Application.Features.Auth.Refresh;
using SportShop.Application.Features.Auth.Register;
using SportShop.Application.Features.Auth.ResetPassword;
using SportShop.Application.Features.Catalog.Brands.CreateBrand;
using SportShop.Application.Features.Catalog.Brands.DeleteBrand;
using SportShop.Application.Features.Catalog.Brands.GetBrand;
using SportShop.Application.Features.Catalog.Brands.ListBrands;
using SportShop.Application.Features.Catalog.Brands.UpdateBrand;
using SportShop.Application.Features.Catalog.Categories.CreateCategory;
using SportShop.Application.Features.Catalog.Categories.DeleteCategory;
using SportShop.Application.Features.Catalog.Categories.GetCategory;
using SportShop.Application.Features.Catalog.Categories.ListCategories;
using SportShop.Application.Features.Catalog.Categories.UpdateCategory;
using SportShop.Application.Features.Catalog.Products.CreateProduct;
using SportShop.Application.Features.Catalog.Products.DeleteProduct;
using SportShop.Application.Features.Catalog.Products.GetProduct;
using SportShop.Application.Features.Catalog.Products.ListProducts;
using SportShop.Application.Features.Catalog.Products.UpdateProduct;
using SportShop.Application.Features.Catalog.Variants.CreateVariant;
using SportShop.Application.Features.Catalog.Variants.DeleteVariant;
using SportShop.Application.Features.Catalog.Variants.UpdateVariant;
using SportShop.Application.Features.Users.ChangePassword;
using SportShop.Application.Features.Users.GetProfile;
using SportShop.Application.Features.Users.UpdateProfile;

namespace SportShop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetProfileQueryHandler>();
        services.AddScoped<UpdateProfileCommandHandler>();
        services.AddScoped<ChangePasswordCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();
        services.AddScoped<ConfirmEmailCommandHandler>();

        // Categories
        services.AddScoped<CreateCategoryCommandHandler>();
        services.AddScoped<UpdateCategoryCommandHandler>();
        services.AddScoped<GetCategoryByIdQueryHandler>();
        services.AddScoped<ListCategoriesQueryHandler>();
        services.AddScoped<DeleteCategoryCommandHandler>();
        services.AddScoped<Features.Catalog.Categories.GetCategoryTree.GetCategoryTreeQueryHandler>();

        // Brands
        services.AddScoped<CreateBrandCommandHandler>();
        services.AddScoped<UpdateBrandCommandHandler>();
        services.AddScoped<GetBrandByIdQueryHandler>();
        services.AddScoped<ListBrandsQueryHandler>();
        services.AddScoped<DeleteBrandCommandHandler>();
        services.AddScoped<Features.Catalog.Brands.GetActiveBrands.GetActiveBrandsQueryHandler>();

        // Products
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<UpdateProductCommandHandler>();
        services.AddScoped<GetProductByIdQueryHandler>();
        services.AddScoped<ListAdminProductsQueryHandler>();
        services.AddScoped<Features.Catalog.Products.List.ListProductsQueryHandler>();
        services.AddScoped<Features.Catalog.Products.GetBySlug.GetProductBySlugQueryHandler>();
        services.AddScoped<DeleteProductCommandHandler>();

        // Variants
        services.AddScoped<CreateVariantCommandHandler>();
        services.AddScoped<UpdateVariantCommandHandler>();
        services.AddScoped<DeleteVariantCommandHandler>();

        // Images
        services.AddScoped<Features.Catalog.Images.UploadImage.UploadProductImageCommandHandler>();
        services.AddScoped<Features.Catalog.Images.DeleteImage.DeleteProductImageCommandHandler>();

        // Inventory
        services.AddScoped<Features.Catalog.Inventory.AdjustStock.AdjustStockCommandHandler>();
        services.AddScoped<Features.Catalog.Inventory.LowStock.GetLowStockVariantsQueryHandler>();

        // Cart
        services.AddScoped<Features.Cart.GetCart.GetCartQueryHandler>();
        services.AddScoped<Features.Cart.AddItem.AddCartItemCommandHandler>();
        services.AddScoped<Features.Cart.UpdateItem.UpdateCartItemCommandHandler>();
        services.AddScoped<Features.Cart.RemoveItem.RemoveCartItemCommandHandler>();
        services.AddScoped<Features.Cart.Clear.ClearCartCommandHandler>();

        // Shipping
        services.AddScoped<Features.Shipping.ListRates.ListShippingRatesQueryHandler>();
        services.AddScoped<Features.Shipping.GetRate.GetShippingRateQueryHandler>();
        services.AddScoped<Features.Shipping.GetActiveRates.GetActiveShippingRatesQueryHandler>();
        services.AddScoped<Features.Shipping.UpdateRate.UpdateShippingRateCommandHandler>();
        services.AddScoped<Features.Shipping.GetQuote.GetShippingQuoteQueryHandler>();

        // Addresses
        services.AddScoped<Features.Addresses.ListAddresses.ListAddressesQueryHandler>();
        services.AddScoped<Features.Addresses.GetAddress.GetAddressByIdQueryHandler>();
        services.AddScoped<Features.Addresses.CreateAddress.CreateAddressCommandHandler>();
        services.AddScoped<Features.Addresses.UpdateAddress.UpdateAddressCommandHandler>();
        services.AddScoped<Features.Addresses.DeleteAddress.DeleteAddressCommandHandler>();
        services.AddScoped<Features.Addresses.SetDefault.SetDefaultAddressCommandHandler>();

        // Orders
        services.AddScoped<Features.Orders.Checkout.CheckoutOrderCommandHandler>();

        return services;
    }
}
