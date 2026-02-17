using Eshop.Catalog.Dtos;
using Eshop.Catalog.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eshop.Catalog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ILogger<CategoryController> _logger;
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetALLCategories(CancellationToken ct)
        {
            return Ok(await _categoryService.GetAllAsync(ct));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken ct)
        {
            return Ok(await _categoryService.GetByIdAsync(id, ct));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryCreateDto dto, CancellationToken ct)
        {
            var category = await _categoryService.CreateAsync(dto, ct);
            if (category == null)
            {
                return BadRequest("Failed to create category");
            }
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateCategory(int id, CategoryUpdateDto dto, CancellationToken ct)
        {
            var category = await _categoryService.UpdateAsync(id, dto, ct);
            if (category == null)
            {
                return BadRequest("Failed to update category");
            }
            return Ok(category);
        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
        {
            var result = await _categoryService.DeleteAsync(id, ct);
            if (!result)
            {
                return BadRequest("Failed to delete category");
            }
            return Ok();

        }
    }
}
