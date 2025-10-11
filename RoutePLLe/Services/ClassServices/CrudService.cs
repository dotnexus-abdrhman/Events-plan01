using EvenDAL.Repositories.InterFace;
using EventPl.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    public class CrudService<TEntity, TDto, TId> : ICrudService<TDto, TId>
     where TEntity : class
    {
        private readonly IRepository<TEntity, TId> _repo;
        private readonly System.Func<TEntity, TDto> _toDto;
        private readonly System.Func<TDto, TEntity> _toEntity;

        public CrudService(IRepository<TEntity, TId> repo,
                           System.Func<TEntity, TDto> toDto,
                           System.Func<TDto, TEntity> toEntity)
        {
            _repo = repo;
            _toDto = toDto;
            _toEntity = toEntity;
        }

        public async Task<TDto> GetByIdAsync(TId id)
            => (await _repo.GetByIdAsync(id)) is var e && e != null ? _toDto(e) : default;

        public async Task<IEnumerable<TDto>> ListAsync()
            => (await _repo.ListAsync()).Select(_toDto);

        public async Task<TDto> CreateAsync(TDto dto)
        {
            var e = _toEntity(dto);
            await _repo.AddAsync(e);
            return _toDto(e);
        }

        public Task<bool> UpdateAsync(TDto dto)
            => _repo.UpdateAsync(_toEntity(dto));

        public Task<bool> DeleteAsync(TId id)
            => _repo.DeleteByIdAsync(id);
    }
}
