using AutoMapper;
using MateHR.Application.Recruitment.Areas.DTOs;
using MateHR.Domain.Common;
using MateHR.Domain.Recruitment.Entities;

namespace MateHR.Application.Recruitment.Areas.Mappings
{
    public class AreaMapping : Profile
    {
        public AreaMapping()
        {
            CreateMap<Area, AreaResponse>();
            CreateMap<PagedResult<Area>, PagedResult<AreaResponse>>();
        }
    }
}
