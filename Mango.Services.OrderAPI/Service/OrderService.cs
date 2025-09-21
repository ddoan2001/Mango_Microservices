using AutoMapper;
using log4net;
using Mango.Services.OrderAPI.Data;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto.Cart;
using Mango.Services.OrderAPI.Models.Dto.Order;
using Mango.Services.OrderAPI.Models.Dto.Payment;
using Mango.Services.OrderAPI.Models.Dto.Product;
using Mango.Services.OrderAPI.Service.IService;
using Mango.Services.OrderAPI.Utility;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Mango.Services.OrderAPI.Service
{
    public class OrderService : IOrderService
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IProductService _productService;
        private static readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OrderService(IMapper mapper, AppDbContext db, IProductService productService)
        {
            _mapper = mapper;
            _db = db;
            _productService = productService;
        }


        public async Task<OrderHeader> GetOrderById(int orderHeaderId)
        {
            return await GetOrderHeaderDtoAsync(o => o.OrderHeaderId == orderHeaderId);
        }

        public async Task<OrderHeader> GetOrderById(string paymentIntentId)
        {
            return await GetOrderHeaderDtoAsync(o => o.PaymentIntentId == paymentIntentId);
        }

        private async Task<OrderHeader> GetOrderHeaderDtoAsync(Expression<Func<OrderHeader, bool>> predicate)
        {
            // 1. Get all products
            IEnumerable<ProductDto> productList = await _productService.GetProducts();

            // 2. Get the order header with details
            OrderHeader orderHeader = await _db.OrderHeaders
                .Include(o => o.OrderDetails)
                .FirstAsync(predicate);

            // 3. Attach ProductDto to each OrderDetailsDto
            foreach (var detail in orderHeader.OrderDetails)
            {
                detail.Product = productList.FirstOrDefault(p => p.ProductId == detail.ProductId);
            }

            return orderHeader;
        }

        public async Task<IEnumerable<OrderHeaderDto>> GetOrdersByUserId(OrderStatus? status, string? userId, bool isAdmin)
        {
            IQueryable<OrderHeader> query = _db.OrderHeaders.Include(o => o.OrderDetails);

            if (isAdmin)
            {
                query = query.OrderByDescending(o => o.OrderHeaderId);
            }
            else
            {
                query = query.Where(o => o.UserId == userId);

                if (status != null)
                {
                    query = query.Where(o => o.Status == status);
                }

                query = query.OrderByDescending(o => o.OrderHeaderId);
            }

            var objList = await query.ToListAsync();
            return _mapper.Map<IEnumerable<OrderHeaderDto>>(objList);
        }

        private static double CalculateDeliveryFee(double subtotal)
        {
            return subtotal > 10000 ? 0 : 500;
        }

        public async Task<OrderHeaderDto> CreateOrder(CartHeaderDto cartHeader)
        {
            OrderHeaderDto orderHeaderDto = _mapper.Map<OrderHeaderDto>(cartHeader);
            orderHeaderDto.OrderTime = DateTime.Now;
            orderHeaderDto.DeliveryFee = CalculateDeliveryFee(orderHeaderDto.OrderTotal);
            orderHeaderDto.OrderDetails = _mapper.Map<IEnumerable<OrderDetailsDto>>(cartHeader.CartDetails);
            // Get productList
            IEnumerable<ProductDto> productList = await _productService.GetProducts();

            foreach (var item in orderHeaderDto.OrderDetails)
            {
                var productItem = productList.FirstOrDefault(x => x.ProductId == item.ProductId)
                    ?? throw new Exception("Product not found for order item");

                productItem.QuantityInStock -= item.Quantity;
            }

            // TODO: need to update to database product

            orderHeaderDto.OrderTotal = Math.Round(orderHeaderDto.OrderTotal, 2);

            OrderHeader orderCreated = _db.OrderHeaders.Add(_mapper.Map<OrderHeader>(orderHeaderDto)).Entity;

            await _db.SaveChangesAsync();

            return _mapper.Map<OrderHeaderDto>(orderCreated);
        }

        public async Task<OrderHeaderDto> UpdateOrder(OrderHeaderDto orderHeaderDto)
        {
            if (orderHeaderDto == null) throw new ArgumentNullException(nameof(orderHeaderDto), "OrderHeaderDto cannot be null");

            // Check if the order exists by either OrderHeaderId or PaymentIntentId
            var existingOrder = await _db.OrderHeaders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(x => x.OrderHeaderId == orderHeaderDto.OrderHeaderId ||
                                          x.PaymentIntentId == orderHeaderDto.PaymentIntentId) ??
                                          throw new Exception("Order not found");

            // 1. Mapping
            // 1.1. Mapping for orderHeader
            OrderHeader orderHeader = _mapper.Map(orderHeaderDto, existingOrder); // This updates only mapped fields
            orderHeaderDto.DeliveryFee = CalculateDeliveryFee(orderHeaderDto.OrderTotal);
            // 1.2. Mapping for orderDetails
            orderHeader.OrderDetails = _mapper.Map(orderHeaderDto.OrderDetails, existingOrder.OrderDetails);

            // 2. Update for database
            _db.OrderHeaders.Update(orderHeader);
            await _db.SaveChangesAsync();

            // 3. Return the updated order header
            return _mapper.Map<OrderHeaderDto>(orderHeader);
        }

        public Task<OrderHeaderDto> UpdateOrderStatus(int orderHeaderId, string status)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateOrderStatus(PaymentQueueDto paymentQueueDto)
        {
            try
            {
                var existingOrder = await GetOrderById(paymentQueueDto.PaymentIntentId);

                if ((existingOrder.OrderTotal + existingOrder.DeliveryFee) != paymentQueueDto.Total)
                {
                    existingOrder.Status = OrderStatus.PaymentMismatch;
                }
                else
                {
                    existingOrder.Status = paymentQueueDto.Status;
                }

                _db.OrderHeaders.Update(existingOrder);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating order status: {ex.Message}");
                return false;
            }

        }
    }
}
