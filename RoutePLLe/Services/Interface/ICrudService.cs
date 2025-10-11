using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    public interface ICrudService<TDto, TId>
    {
        Task<TDto> GetByIdAsync(TId id);
        Task<IEnumerable<TDto>> ListAsync();
        Task<TDto> CreateAsync(TDto dto);
        Task<bool> UpdateAsync(TDto dto);
        Task<bool> DeleteAsync(TId id);
    }
}
