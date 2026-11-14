using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Domain.Recruitment.Common
{
    public interface ICatalogEntity
    {
        Guid Id { get; }
        Guid TenantId { get; }
        string Name { get; }
        bool IsActive { get; }

        void Rename(string name);
        void Activate();
        void Deactivate();
    }
}
