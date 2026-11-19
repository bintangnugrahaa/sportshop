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

        // Brands
        services.AddScoped<CreateBrandCommandHandler>();
        services.AddScoped<UpdateBrandCommandHandler>();
        services.AddScoped<GetBrandByIdQueryHandler>();
        services.AddScoped<ListBrandsQueryHandler>();
        services.AddScoped<DeleteBrandCommandHandler>();

        // Products
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<UpdateProductCommandHandler>();
        services.AddScoped<GetProductByIdQueryHandler>();
        services.AddScoped<ListAdminProductsQueryHandler>();
        services.AddScoped<Features.Catalog.Products.List.ListProductsQueryHandler>();
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

        return services;
    }
}
