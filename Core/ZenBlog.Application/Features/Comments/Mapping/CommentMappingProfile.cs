// Mapping/CommentMappingProfile.cs
using AutoMapper;
using ZenBlog.Application.DTOs;
using ZenBlog.Application.DTOs.ZenBlog.Application.DTOs;
using ZenBlog.Application.Features.Comments.Commands;
using ZenBlog.Application.Features.Comments.Results;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Features.Comments.Mapping
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CommentResult>();
            CreateMap<AppUser, UserDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName));

            CreateMap<CreateCommentCommand, Comment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Replies, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Blog, opt => opt.Ignore());

            CreateMap<UpdateCommentCommand, Comment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())// You can't change which blog a comment belongs to after creation
                .ForMember(dest => dest.BlogId, opt => opt.Ignore()) // You can't change the author of a comment
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.ParentCommentId, opt => opt.Ignore())
                .ForMember(dest => dest.Replies, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Blog, opt => opt.Ignore());
        }
    }
}