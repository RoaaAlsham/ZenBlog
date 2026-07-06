using AutoMapper;
using ZenBlog.Application.Features.Users.Commands;
using ZenBlog.Application.Features.Users.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Users.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            // Existing mapping: CreateUserCommand -> AppUser
            CreateMap<CreateUserCommand, AppUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

            // Fixed mapping: AppUser -> GetAllUsersQueryResult
            CreateMap<AppUser, GetAllUsersQueryResult>()
                .ConstructUsing(src => new GetAllUsersQueryResult(
                    src.Id,
                    src.UserName,
                    src.Email,
                    src.FirstName + " " + src.LastName,
                    src.ImageUrl
                ))
                // Ignore all other members to avoid ambiguity
                .ForAllMembers(opt => opt.Ignore());
        }
    }
}