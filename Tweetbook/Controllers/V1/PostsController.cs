using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tweetbook.Cache;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Requests.Queries;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;
using Tweetbook.Extensions;
using Tweetbook.Helper;
using Tweetbook.Services;

namespace Tweetbook.Controllers.V1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly IMapper _mapper;
        private readonly IUriService _uriService;

        public PostsController(IPostService postService, IMapper mapper, IUriService uriService) {
            _postService = postService;
            _mapper = mapper;
            _uriService = uriService;
        }

        [HttpGet(ApiRoutes.Posts.GetAll)]
        //[Cached(600)]
        public async Task<IActionResult> GetAllAsync([FromQuery]PaginationQuery query) {
            var pagination = _mapper.Map<PaginationFilter>(query);
            var posts = await _postService.GetPostsAsync(pagination);
            var postResponse = _mapper.Map<List<PostResponse>>(posts);
            if (pagination == null || pagination.PageNumber < 1 || pagination.PageSize < 1) {
                return Ok(new PagedResponse<PostResponse>(postResponse));
            }

            var paginationResponse = PaginationHelpers.CreatePaginatedResponse(_uriService, pagination, postResponse);
            return Ok(paginationResponse);
        }

        [HttpGet(ApiRoutes.Posts.Get)]
        [Cached(600)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid postId) {
            var post = await _postService.GetPostbyIdAsync(postId);
            if (post == null) {
                return NotFound();
            }
            return Ok(new Response<PostResponse>(_mapper.Map<PostResponse>(post)));
        }

        [HttpPut(ApiRoutes.Posts.Update)]
        public async Task<IActionResult> UpdateAsync([FromRoute] Guid postId, [FromBody] UpdatePostRequest postRequest) {
            var userOwnsPost = await _postService.UserOwnsPostAsync(postId, HttpContext.GetUserId());
            if (!userOwnsPost) {
                return BadRequest(new {
                    error = "You do not own this post"
                });
            }

            var post = await _postService.GetPostbyIdAsync(postId);
            post.Name = postRequest.Name;

            var updated = await _postService.UpdatePostAsync(post);

            if (updated) {
                return Ok(new Response<PostResponse>(_mapper.Map<PostResponse>(post)));
            }
            return NotFound();
        }

        [HttpDelete(ApiRoutes.Posts.Delete)]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid postId) {
            var userOwnsPost = await _postService.UserOwnsPostAsync(postId, HttpContext.GetUserId());
            if (!userOwnsPost) {
                return BadRequest(new {
                    error = "You do not own this post"
                });
            }
            var deleted = await _postService.DeletePostAsync(postId);
            if (deleted) {
                return NoContent();
            }
            return NotFound();
        }

        [HttpPost(ApiRoutes.Posts.Create)]
        public async Task<IActionResult> CreateAsync([FromBody] CreatePostRequest postRequest) {
            var newPostId = Guid.NewGuid();
            var post = new Post {
                Id = newPostId,
                Name = postRequest.Name,
                UserId = HttpContext.GetUserId(),
                Tags = postRequest.Tags.Select(x => new PostTag {
                    PostId = newPostId,
                    TagName = x
                }).ToList()
            };

            var created = await _postService.CreatePostAsync(post);
            if (!created) {
                return BadRequest();
            }
            var locationUrl = _uriService.GetPostUri(post.Id.ToString());
            return Created(locationUrl, new Response<PostResponse>(_mapper.Map<PostResponse>(post)));
        }
    }
}
