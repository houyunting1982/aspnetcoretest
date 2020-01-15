using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tweetbook.Contracts.V1;
using Tweetbook.Contracts.V1.Requests;
using Tweetbook.Contracts.V1.Responses;
using Tweetbook.Domain;
using Tweetbook.Extensions;
using Tweetbook.Services;

namespace Tweetbook.Controllers.V1
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        public PostsController(IPostService postService) {
            _postService = postService;
        }

        [HttpGet(ApiRoutes.Posts.GetAll)]
        public async Task<IActionResult> GetAllAsync() {
            return Ok(await _postService.GetPostsAsync());
        }

        [HttpGet(ApiRoutes.Posts.Get)]
        public async Task<IActionResult> GetAsync([FromRoute] Guid postId) {
            var post = await _postService.GetPostbyIdAsync(postId);
            if (post == null) {
                return NotFound();
            }
            return Ok(post);
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
                return Ok(post);
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
        public async Task<IActionResult> Create([FromBody] CreatePostRequest postRequest) {
            var post = new Post {
                Name = postRequest.Name,
                UserId = HttpContext.GetUserId()
            };

            var created = await _postService.CreatePostAsync(post);
            if (!created) {
                return BadRequest();
            }
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";
            var locationUrl = baseUrl + "/" + ApiRoutes.Posts.Get.Replace("{postId}", post.Id.ToString());

            var response = new PostResponse {Id = post.Id};
            return Created(locationUrl, response);
        }
    }
}