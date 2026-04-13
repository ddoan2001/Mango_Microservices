using AutoMapper;
using Mango.Services.OrderAPI.Extensions;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto.Cart;
using Mango.Services.OrderAPI.Models.Dto.Order;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<OrderHeaderDto, CartHeaderDto>()
            .ForMember(dest => dest.CartTotal, u => u.MapFrom(src => src.OrderTotal))
            .ReverseMap();

            CreateMap<CartDetailsDto, OrderDetailsDto>()
            .ForMember(dest => dest.ProductName, u => u.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.Price, u => u.MapFrom(src => src.Product.Price));

            CreateMap<OrderDetailsDto, CartDetailsDto>();

            CreateMap<OrderHeaderDto, OrderHeader>()
           .Ignore(x => x.CreatedAt)
           .Ignore(x => x.UpdatedAt);

            CreateMap<OrderHeader, OrderHeaderDto>();

            CreateMap<OrderDetailsDto, OrderDetails>().ReverseMap();
        }
    }
}
