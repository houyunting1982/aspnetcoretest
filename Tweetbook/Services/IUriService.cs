using System;
using Tweetbook.Contracts.V1.Requests.Queries;
using Tweetbook.Domain;

namespace Tweetbook.Services
{
    public interface IUriService
    {
        Uri GetPostUri(string postId);
        Uri GetAllPostsUri(PaginationQuery pagination = null);
    }
}