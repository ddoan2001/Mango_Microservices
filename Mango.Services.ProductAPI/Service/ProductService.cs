using AutoMapper;
using Mango.Cache.Interface;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Extensions;
using Mango.Services.ProductAPI.Models;
using Mango.Services.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.Models.Dto.Filters;
using Mango.Services.ProductAPI.RequestHelpers;
using Mango.Services.ProductAPI.Service.IService;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Mango.Services.ProductAPI.Service
{
    public class ProductService : IProductService
    {
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor; // Add IHttpContextAccessor for accessing HttpContext
        private readonly ICacheFactory _cacheFactory;
        private const string Cache_Manager_Products = "Cache_Manager_Products";
        private const string Cache_Key_Global_Products = "Cache_Key_Global_Products";

        public ProductService(IMapper mapper, AppDbContext db, IHttpContextAccessor httpContextAccessor, ICacheFactory cacheFactory)
        {
            _mapper = mapper;
            _db = db;
            _httpContextAccessor = httpContextAccessor; // Initialize IHttpContextAccessor
            _cacheFactory = cacheFactory;
        }

        /// <summary>
        /// Create a product in the database.
        /// </summary>
        /// <param name="productDto"></param>
        /// <returns>ProductDto</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Product> Create(CreateProductDto productDto)
        {
            if (productDto == null)
            {
                throw new ArgumentNullException(nameof(productDto), "Product cannot be null");
            }

            // 1. Mapper and save product
            Product product = _mapper.Map<Product>(productDto);
            _db.Products.Add(product);
            _db.SaveChanges();

            // 2. Save image product
            if (productDto.File != null)
            {
                string fileName = product.ProductId + Path.GetExtension(productDto.File.FileName);
                string filePath = @"wwwroot\ProductImages\" + fileName;

                string path = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                {
                    file.Delete();
                }
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await productDto.File.CopyToAsync(stream);
                }

                // Use IHttpContextAccessor to access HttpContext
                var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}{httpContext.Request.PathBase.Value}";

                product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                product.ImageLocalPath = filePath;
            }
            else
            {
                product.ImageUrl = "https://placehold.co/600x400";
            }

            // 3. Update to database
            _db.Products.Update(product);
            _db.SaveChanges();

            return product;
        }

        /// <summary>
        /// Update a product in the database.
        /// </summary>
        /// <param name="productDto"></param>
        /// <returns>ProductDto</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Product> Update(UpdateProductDto productDto)
        {
            if (productDto == null)
            {
                throw new ArgumentNullException(nameof(productDto), "Product cannot be null");
            }

            var existingProduct = await _db.Products.FindAsync(productDto.ProductId) ?? throw new Exception("Product not found");

            // 1. Mapping
            Product product = _mapper.Map(productDto, existingProduct); // This updates only mapped fields

            // 2. Save image product
            if (productDto.File != null)
            {
                // Existing product: fetch from DB
                if (!string.IsNullOrEmpty(product.ImageLocalPath))
                {
                    var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                    FileInfo file = new FileInfo(oldFilePathDirectory);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }

                string fileName = product.ProductId + Path.GetExtension(productDto.File.FileName);
                string filePath = @"wwwroot\ProductImages\" + fileName;

                string path = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await productDto.File.CopyToAsync(stream);
                }

                // Use IHttpContextAccessor to access HttpContext
                var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}{httpContext.Request.PathBase.Value}";

                product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                product.ImageLocalPath = filePath;
            }

            // 3. Update to database
            _db.Products.Update(product);
            _db.SaveChanges();
            return product;
        }

        public async Task<int> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id) ?? throw new KeyNotFoundException($"Product with ID {id} not found.");
            if (!string.IsNullOrEmpty(product.ImageLocalPath))
            {
                var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                FileInfo file = new FileInfo(oldFilePathDirectory);
                if (file.Exists)
                {
                    file.Delete();
                }
            }
            _db.Products.Remove(product);
            return await _db.SaveChangesAsync();
        }

        public async Task<Filter> GetFilters()
        {
            {
                IEnumerable<Category> categories = await _db.Categories
                    .ToListAsync();
                IEnumerable<Brand> brands = await _db.Brands
                    .ToListAsync();
                return new Filter()
                {
                    Categories = _mapper.Map<IEnumerable<CategoryDto>>(categories),
                    Brands = _mapper.Map<IEnumerable<BrandDto>>(brands)
                };
            }
        }

        public async Task<ProductDto> GetProductById(int id)
        {
            var product = await _db.Products.FindAsync(id) ?? throw new KeyNotFoundException($"Product with ID {id} not found.");
            var res = _mapper.Map<ProductDto>(product);
            return res;
        }

        public async Task<IEnumerable<ProductDto>> GetProducts(ProductParams productParams)
        {
            // Get cache manager
            var cacheManager = _cacheFactory.GetCacheManager(Cache_Manager_Products);

            // Create unique cache key based on parameters - handle Brands as string
            string key = $"{Cache_Key_Global_Products}_{productParams.PageNumber}_{productParams.PageSize}_{productParams.OrderBy}" +
                $"_{productParams.SearchTerm}_{productParams.Brands ?? ""}_{productParams.Categories ?? ""}";

            // Use IHttpContextAccessor to add pagination headers
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

            if (cacheManager.Contains(key))
            {
                var cachedResult = (CachedProductResult)cacheManager.GetData(key);
                // Add pagination headers from cached metadata
                httpContext.Response.AddPaginationHeaders(cachedResult.Metadata);
                return cachedResult.Products;
            }

            var query = _db.Products
            .Sort(productParams.OrderBy)
            .Search(productParams.SearchTerm)
            .Filter(productParams.Brands, productParams.Categories)
            .AsQueryable();

            // Fetch paginated list of products
            var products = await PagedList<Product>.ToPagedList(query, productParams.PageNumber, productParams.PageSize);

            // Map Product entities to ProductDto
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

            // Add pagination headers for fresh data
            httpContext.Response.AddPaginationHeaders(products.Metadata);

            // Cache both data and metadata
            var cachedProductResult = new CachedProductResult
            {
                Products = productDtos,
                Metadata = products.Metadata
            };
            cacheManager.AddOrUpdate(key, cachedProductResult);

            return productDtos;
        }

        public async Task<Filter> GetFiltersIncludeProduct()
        {
            {
                IEnumerable<Category> categories = await _db.Categories
                    .Include(x => x.Products)
                    .AsNoTracking()
                    .ToListAsync();
                IEnumerable<Brand> brands = await _db.Brands
                    .Include(x => x.Products)
                    .AsNoTracking()
                    .ToListAsync();
                return new Filter()
                {
                    Categories = _mapper.Map<IEnumerable<CategoryDto>>(categories), // Fix: Map to IEnumerable<CategoryDto>
                    Brands = _mapper.Map<IEnumerable<BrandDto>>(brands) // Ensure brands are mapped correctly
                };
            }
        }

        private async Task RemoveInvalidFilters(FilterInput filterInput)
        {
            var cateInput = JsonConvert.DeserializeObject<IEnumerable<CategoryDto>>(filterInput.Categories);
            var brandInput = JsonConvert.DeserializeObject<IEnumerable<BrandDto>>(filterInput.Brands);

            if (cateInput == null || brandInput == null) return;

            var filter = new Filter()
            {
                Categories = cateInput,
                Brands = brandInput
            };

            var allFilter = await GetFiltersIncludeProduct();

            var listCateRemove = allFilter.Categories.AsEnumerable()
                .Where(c => !filter.Categories.Any(fc => fc.CategoryId == c.CategoryId)); // Get categories that do not exist in the filter

            var listBrandRemove = allFilter.Brands.AsEnumerable()
                .Where(b => !filter.Brands.Any(fb => fb.BrandId == b.BrandId)); // Get brands that do not exist in the filter

            if (listCateRemove == null || listBrandRemove == null) return;

            foreach (var category in listCateRemove)
            {
                if (category.Products.Count != 0)
                {
                    throw new InvalidOperationException($"Category {category.Name} cannot be removed because it is associated with products.");
                }
                _db.Categories.Remove(_mapper.Map<Category>(category)); // Remove categories that do not exist in the filter
            }

            foreach (var brand in listBrandRemove)
            {
                if (brand.Products.Count != 0)
                {
                    throw new InvalidOperationException($"Brand {brand.Name} cannot be removed because it is associated with products.");
                }
                _db.Brands.Remove(_mapper.Map<Brand>(brand)); // Remove brands that do not exist in the filter
            }

            // Save changes to the database
            await _db.SaveChangesAsync();
        }

        private async Task AddOrUpdateFilters(FilterInput filterInput)
        {

            var cateInput = JsonConvert.DeserializeObject<IEnumerable<CategoryDto>>(filterInput.Categories);
            var brandInput = JsonConvert.DeserializeObject<IEnumerable<BrandDto>>(filterInput.Brands);

            if (cateInput == null || brandInput == null) return;

            var filter = new Filter()
            {
                Categories = cateInput,
                Brands = brandInput
            };

            // Add or update categories
            foreach (var category in filter.Categories)
            {
                var existingCategory = _db.Categories.FirstOrDefault(c => c.CategoryId == category.CategoryId);

                // Not existing => create
                if (existingCategory == null)
                {
                    _db.Categories.Add(new Category { Name = category.Name });
                }
                else
                {
                    // Update existing category if needed
                    if (existingCategory.Name != category.Name)
                    {
                        existingCategory.Name = category.Name;
                        _db.Categories.Update(existingCategory);
                    }
                }
            }
            // Add or update brands
            foreach (var brand in filter.Brands)
            {
                var existingBrand = _db.Brands.FirstOrDefault(b => b.BrandId == brand.BrandId);

                // Not existing => create
                if (existingBrand == null)
                {
                    _db.Brands.Add(new Brand { Name = brand.Name });
                }
                else
                {
                    // Update existing brand if needed
                    if (existingBrand.Name != brand.Name)
                    {
                        existingBrand.Name = brand.Name;
                        _db.Brands.Update(existingBrand);
                    }
                }
            }

            // Save changes to the database
            await _db.SaveChangesAsync();
        }
        /// <summary>
        /// Handle edit filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns>filterDto</returns>
        public async Task<bool> EditFilters(FilterInput filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter), "Filter cannot be null");
            }

            // Remove invalid filters
            await RemoveInvalidFilters(filter);

            // Add or update filters
            await AddOrUpdateFilters(filter);

            return true;
        }
    }
}
