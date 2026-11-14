using MateHR.Application.Recruitment.Areas.Interfaces;
using MateHR.Domain.Recruitment.Exceptions;
using MateHR.Domain.Recruitment.Interfaces;
using MateHR.Domain.Tenants.Interfaces;

namespace MateHR.Application.Recruitment.Areas.Commands
{
    public class DeleteArea : IDeleteArea
    {
        private readonly IAreaRepository _repository;
        private readonly ITenantContext _tenantContext;

        public DeleteArea(
            IAreaRepository repository,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _tenantContext = tenantContext;
        }

        public async Task ExecuteAsync(Guid id, CancellationToken ct = default)
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (tenantId == null)
            {
                throw new UnauthorizedAccessException("No tienes acceso para acceder a esta información");
            }
            await _repository.GetByIdAsync(tenantId.Value, id, ct);
            var inUse = await _repository.IsInUseAsync(tenantId.Value, id, ct);
            if (inUse)
            {
                throw new EntityInUseException("Area", id);
            }
            await _repository.DeleteAsync(tenantId.Value, id, ct);
        }
    }
}
