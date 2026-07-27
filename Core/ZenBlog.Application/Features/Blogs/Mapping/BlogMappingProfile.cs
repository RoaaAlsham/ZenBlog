
using AutoMapper;
using ZenBlog.Application.DTOs;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Results;

namespace ZenBlog.Application.Features.Blogs.Mapping
{
    public class BlogMappingProfile: Profile
    {
        public BlogMappingProfile()
        {
            CreateMap<Domain.Entities.Blog, GetBlogsQueryResult>().ReverseMap();
            CreateMap<CreateBlogCommand, Domain.Entities.Blog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());
            CreateMap<Domain.Entities.Category, CategoryDto>();

            // For update — map command onto existing entity, ignore fields not in command
            CreateMap<UpdateBlogCommand, Domain.Entities.Blog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())         // never overwrite Id
                .ForMember(dest => dest.UserId, opt => opt.Ignore())     // never overwrite owner
                .ForMember(dest => dest.Comments, opt => opt.Ignore());  // never touch relations
        }
    }
}
