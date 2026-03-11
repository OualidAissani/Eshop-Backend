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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory(CategoryCreateDto dto, CancellationToken ct)
        {
            var category = await _categoryService.CreateAsync(dto, ct);
            if (category.IsFailed)
            {
                return BadRequest(category.Errors.First().Message);
            }
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Value.Id }, category.Value);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryUpdateDto dto, CancellationToken ct)
        {
            var category = await _categoryService.UpdateAsync(id, dto, ct);
            if (category.IsFailed)
            {
                return BadRequest(category.Errors.First().Message);
            }
            return Ok(category.Value);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
        {
            var result = await _categoryService.DeleteAsync(id, ct);
            if (result.IsFailed)
            {
                return BadRequest(result.Errors.First().Message);
            }
            return Ok();

        }
    }
}
