using System.Linq;
using AutoMapper;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;

namespace Tweetbook.Mapping
{
    public class DomainToResponseProfile : Profile
    {
        public DomainToResponseProfile() {
            CreateMap<Post, PostResponse>()
                .ForMember(dest => dest.Tags, options => {
                    options.MapFrom(source => source.Tags.Select(x => new TagResponse {
                        Name = x.TagName
                    }));
                });
            CreateMap<Tag, TagResponse>();
        }
    }
}