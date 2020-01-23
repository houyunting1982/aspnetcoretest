using AutoMapper;
using Tweetbook.Contracts.V1.Requests.Queries;
using Tweetbook.Domain;

namespace Tweetbook.Mapping
{
    public class RequestToDomainProfile : Profile
    {
        public RequestToDomainProfile() {
            CreateMap<PaginationQuery, PaginationFilter>();
            CreateMap<GetAllPostsQuery, GetAllPostsFilter>();
        }
    }
}