using AutoMapper;
using Mango.Cache.Interface;
using Mango.Message.RabbitMQ.Sender.Interface;
using Mango.Services.ShoppingCartAPI.Data;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Models.Dto.Cart;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Service
{
    public class CartService : ICartService
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IProductService _productService;
        private readonly ICouponService _couponService;
        private readonly IRabbitMQSender _messageBus;
        private readonly IConfiguration _configuration;
        private readonly ICacheFactory _cacheFactory;
        private const string Cache_Manage_Carts = "Cache_Manage_Carts";
        private const string Cache_Key_Unit_ById_Carts = "Cache_Key_Unit_ById_Carts";

        public CartService(IMapper mapper, AppDbContext db, IProductService productService,
            ICouponService couponService, IRabbitMQSender messageBus, IConfiguration configuration
            , ICacheFactory cacheFactory)
        {
            _mapper = mapper;
            _db = db;
            _productService = productService; // Initialize IProductService
            _couponService = couponService; // Initialize ICouponService
            _messageBus = messageBus; // Initialize IMessageBus
            _configuration = configuration;
            _cacheFactory = cacheFactory;
        }

        public async Task<bool> ApplyCoupon(CartHeaderDto cartDto)
        {
            var cartFromDb = await _db.CartHeaders.FirstAsync(u => u.UserId == cartDto.UserId);
            cartFromDb.CouponCode = cartDto.CouponCode;
            _db.CartHeaders.Update(cartFromDb);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<CartHeaderDto> CartUpsert(InputCartDto inputCartDto)
        {
            // get cart
            var cart = await RetrieveCartByUserId(inputCartDto.UserId);

            // create cartHeader if it doesn't exist
            cart ??= CreateCartHeader(inputCartDto.UserId);

            // get product
            var product = await _productService.GetProduct(inputCartDto.ProductId);
            if (product == null) throw new Exception("Product not found: " + inputCartDto.ProductId);

            // add item to cart
            cart.AddItem(product, inputCartDto.Quantity);

            var result = await _db.SaveChangesAsync() > 0;
            if (!result) throw new Exception("Failed to save cart for user: " + inputCartDto.UserId);

            return _mapper.Map<CartHeaderDto>(cart);

        }

        public async Task<bool> EmailCartRequest(CartHeaderDto cartDto)
        {
            string queueName = _configuration["TopicAndQueueNames:EmailShoppingCartQueue"] ??
                throw new InvalidOperationException("TopicAndQueueNames:EmailShoppingCartQueue not found.");

            // publish message to queue
            await _messageBus.PublishMessage(cartDto, queueName);
            return true;
        }

        public async Task<CartHeaderDto?> GetCart(string userId)
        {
            // Get cache manager
            var cacheManager = _cacheFactory.GetCacheManager(Cache_Manage_Carts);

            // Create unique cache key based on userId
            string key = $"{Cache_Key_Unit_ById_Carts}_{userId}";

            if (cacheManager.Contains(key))
            {
                return (CartHeaderDto)cacheManager.GetData(key);
            }

            var cart = _mapper.Map<CartHeaderDto>(await RetrieveCartByUserId(userId));

            if (cart == null || cart.CartDetails == null || cart.CartDetails.Count == 0)
            {
                return cart ?? null;
            }

            var productDtos = await _productService.GetProducts();
            foreach (var item in cart.CartDetails)
            {
                item.Product = productDtos.FirstOrDefault(u => u.ProductId == item.ProductId);
                if (item.Product != null)
                {
                    cart.CartTotal += (item.Quantity * item.Product.Price);
                }
            }

            // apply coupon if any
            if (!string.IsNullOrEmpty(cart.CouponCode))
            {
                CouponDto coupon = await _couponService.GetCoupon(cart.CouponCode);
                if (coupon != null && cart.CartTotal > coupon.MinAmount)
                {
                    cart.CartTotal -= coupon.DiscountAmount;
                    cart.Discount = coupon.DiscountAmount;
                }
            }

            // Adding data to cache
            cacheManager.AddOrUpdate(key, cart);

            return cart;
        }

        public async Task<bool> RemoveCart(InputCartDto inputCartDto)
        {
            var cart = await RetrieveCartByUserId(inputCartDto.UserId) ?? throw new Exception("Cart not found for user: " + inputCartDto.UserId);

            // remove item from cart
            cart.RemoveItem(inputCartDto.ProductId, inputCartDto.Quantity);

            var result = await _db.SaveChangesAsync() > 0;
            if (result) return true;
            return false;
        }

        public async Task<bool> RemoveItems(ListItemsDto listItemsDto)
        {
            var cart = await RetrieveCartByUserId(listItemsDto.UserId) ?? throw new Exception("Cart not found for user: " + listItemsDto.UserId);

            // remove items from cart
            cart.RemoveItems(listItemsDto.Items);

            var result = await _db.SaveChangesAsync() > 0;
            if (result) return true;
            return false;
        }

        private CartHeader CreateCartHeader(string userId)
        {
            // create cart
            CartHeader cartHeader = new()
            {
                UserId = userId,
            };
            // add cart to context
            _db.CartHeaders.Add(cartHeader);
            return cartHeader;
        }

        private async Task<CartHeader?> RetrieveCartByUserId(string userId)
        {
            return await _db.CartHeaders
                .Include(x => x.CartDetails)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}