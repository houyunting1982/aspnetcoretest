using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Refit;
using Tweetbook.Contracts.V1.Requests;

namespace Tweetbook.Sdk.Sample
{
    class Program
    {
        static async Task Main(string[] args) {
            var cachedToken = string.Empty;
            var identityApi = RestService.For<IIdentityApi>("https://localhost:5001");
            var tweetbookApi = RestService.For<ITweetbookApi>("https://localhost:5001", new RefitSettings {
                AuthorizationHeaderValueGetter = () => Task.FromResult(cachedToken)
            });
            // var registerResponse = await identityApi.RegisterAsync(new UserRegistrationRequest {
            //     Email = "pippi@pippirunner.com",
            //     Password = "Pa$$w0rd"
            // });

            var loginResponse = await identityApi.LoginAsync(new UserLoginRequest {
                Email = "pippi@pippirunner.com",
                Password = "Pa$$w0rd"
            });
            cachedToken = loginResponse.Content.Token;
            var allPosts = await tweetbookApi.GetAllAsync();
            var createPost = await tweetbookApi.CreateAsync(new CreatePostRequest {
                Name = "This is created by the SDK",
                Tags = new[] { "sdk" }
            });
            var retrievedPost = await tweetbookApi.GetAsync(createPost.Content.Id);
            var updatedPost = await tweetbookApi.UpdateAsync(createPost.Content.Id, new UpdatePostRequest {
                Name = "This is updated by SDK"
            });
            var deletedPosts = await tweetbookApi.DeleteAsync(createPost.Content.Id);
        }
    }
}