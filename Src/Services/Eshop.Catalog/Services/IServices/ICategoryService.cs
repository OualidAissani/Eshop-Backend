using Eshop.Catalog.Dtos;
using Eshop.Catalog.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eshop.Catalog.Services.IServices
{
    public interface ICategoryService
    {
        Task<Categories> CreateAsync(CategoryCreateDto dto, CancellationToken cancellationToken);

        Task<Categories> UpdateAsync(int id, CategoryUpdateDto dto, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

        Task<Categories?> GetByIdAsync(int id, CancellationToken cancellationToken);

        Task<List<Categories>> GetAllAsync(CancellationToken cancellationToken );


    }

}
