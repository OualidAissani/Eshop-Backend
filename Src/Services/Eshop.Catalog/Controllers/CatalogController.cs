using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Eshop.Catalog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly IProductRepository _productrepo;
        private readonly IMediaService _mediaService;
        private readonly ILogger<CatalogController> _logger;
        
        public CatalogController(
            IProductRepository productRepository,
            ILogger<CatalogController> logger,
            IMediaService mediaService
            )
        {
            _productrepo = productRepository;
            _logger = logger;
            _mediaService = mediaService;
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto product, List<IFormFile>? formFile)
        {
            //public async Task<ProductMedia> CreateMedia(ProductMedia media, Stream fileStream,string contentType ,string fileName)
            if(formFile == null || formFile.Count == 0)
            {

            }
            var result = await _productrepo.CreateProduct(product);
            foreach (var file in formFile)
            {
                var media = new ProductMedia()
                {
                    ProductId = result.Id,
                    Description = result.Description
                };
                using var stream = file.OpenReadStream();
                await _mediaService.CreateMedia(media,stream,file.ContentType,file.FileName);

            }
            var currentProduct = await _productrepo.GetProductById(result.Id);
            return Ok(currentProduct);
        }
        
        [HttpPost("update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct([FromForm] Products product, IFormFile? formFile)
        {
            if (formFile != null)
            {
                using var stream = formFile.OpenReadStream();
                var result = await _productrepo.UpdateProduct(product, stream, formFile.ContentType, formFile.FileName);
                return Ok(result);
            }
            else
            {
                var result = await _productrepo.UpdateProduct(product, Stream.Null, string.Empty, string.Empty);
                return Ok(result);
            }
        }
        
        [HttpDelete("{Id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int Id)
        {
            var result = await _productrepo.DeleteProduct(Id);
            if (!result)
            {
                return NotFound();
            }
            return Ok(new {message="The Product Has Been Deleted Successfully"});
        }
        
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Products>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = User.Identity.Name;
            }
            var result = await _productrepo.GetProductsAsync(new PaginationParams
            {
                Page = page,
                PageSize = pageSize
            });

            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Products>> GetProductById(int id)
        {
            var product = await _productrepo.GetProductById(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<Products>>> GetProductsByCategory(int categoryId)
        {
            var products = await _productrepo.GetProductsByCategory(categoryId);
            return Ok(products);
        }
        

    }
}
