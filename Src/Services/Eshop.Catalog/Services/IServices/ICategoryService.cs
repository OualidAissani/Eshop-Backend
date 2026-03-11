using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using FluentResults;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eshop.Catalog.Services.IServices
{
    public interface ICategoryService
    {
        Task<Result<Categories>> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken);

        Task<Result<Categories>> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken);

        Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken);

        Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

        Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken );


    }

}
