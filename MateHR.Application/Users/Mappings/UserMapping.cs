using AutoMapper;
using MateHR.Application.Users.DTOs;
using MateHR.Domain.Users.Entities;

namespace MateHR.Application.Users.Mappings
{
    public class UserMapping : Profile
    {
        public UserMapping()
        {
            CreateMap<User, UserResponse>();
        }
    }
}