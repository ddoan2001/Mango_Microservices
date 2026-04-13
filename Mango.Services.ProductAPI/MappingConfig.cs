using AutoMapper;
using Mango.Services.ProductAPI.Extensions;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.Models.Dto.Filters;

namespace Mango.Services.ProductAPI
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<UpdateProductDto, Product>()
            .Ignore(x => x.CreatedAt)
            .Ignore(x => x.UpdatedAt)
            .Ignore(x => x.ImageLocalPath)
            .ReverseMap();
            CreateMap<CreateProductDto, Product>().ReverseMap();
            CreateMap<ProductDto, Product>().ReverseMap();
            CreateMap<CategoryDto, Category>().ReverseMap();
            CreateMap<BrandDto, Brand>().ReverseMap();
        }
    }
}
