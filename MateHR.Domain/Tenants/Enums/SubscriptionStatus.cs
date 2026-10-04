using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Domain.Tenants.Enums
{
    public enum SubscriptionStatus
    {
        None = 0,
        Active = 1,
        GracePeriod = 2,
        Expired = 3,
        Cancelled = 4
    }
}