using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tweetbook.Data;
using Tweetbook.Domain;
using Tweetbook.Options;

namespace Tweetbook.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly DataContext _dataContext;

        public IdentityService(UserManager<IdentityUser> userManager,
            JwtSettings jwtSettings,
            TokenValidationParameters tokenValidationParameters, DataContext dataContext) {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
            _tokenValidationParameters = tokenValidationParameters;
            _dataContext = dataContext;
        }

        public async Task<AuthenticationResult> RegisterAsync(string email, string password) {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null) {
                return new AuthenticationResult {
                    Errors = new [] {
                        "Use with this email address already exists"
                    }
                };
            }
            var newUser = new IdentityUser {
                Email = email,
                UserName = email
            };
            var createdUser = await _userManager.CreateAsync(newUser, password);
            if (!createdUser.Succeeded) {
                return new AuthenticationResult {
                    Errors = createdUser.Errors.Select(x => x.Description)
                };
            }

            return await GenerateAuthenticationResultForUser(newUser);
        }

        public async Task<AuthenticationResult> LoginAsync(string email, string password) {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) {
                return new AuthenticationResult {
                    Errors = new [] {
                        "User does not exist"
                    }
                };
            }
            var userHasValidPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!userHasValidPassword) {
                return new AuthenticationResult {
                    Errors = new [] {
                        "User/password combination is wrong"
                    }
                };
            }

            return await GenerateAuthenticationResultForUser(user);
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshtoken) {
            var validatedToken = GetPrincipalFromToken(token);
            if (validatedToken == null) {
                return new AuthenticationResult {
                    Errors = new[] {"Invalid Token"}
                };
            }

            var expiryDateUnix =
                long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow) {
                return new AuthenticationResult {
                    Errors = new[] {"This token hasn't expired yet"}
                };
            }

            var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            var storedRefreshToken =
                await _dataContext.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshtoken);
            if (storedRefreshToken == null) {
                return new AuthenticationResult {
                    Errors = new[] {"This refresh token doesn't exist"}
                };
            }

            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate) {
                return new AuthenticationResult {
                    Errors = new[] {"This refresh token has been expired"}
                };
            }

            if (storedRefreshToken.Invalidated) {
                return new AuthenticationResult {
                    Errors = new[] {"This refresh token has been invalidated"}
                };
            }

            if (storedRefreshToken.Used) {
                return new AuthenticationResult {
                    Errors = new[] {"This refresh token has been used"}
                };
            }

            if (storedRefreshToken.JwtId != jti) {
                return new AuthenticationResult {
                    Errors = new[] {"This refresh token does not match this JWT"}
                };
            }

            storedRefreshToken.Used = true;
            _dataContext.RefreshTokens.Update((storedRefreshToken));
            await _dataContext.SaveChangesAsync();
            var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type == "id").Value);
            return await GenerateAuthenticationResultForUser(user);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token) {
            var tokenHandler = new JwtSecurityTokenHandler();
            try {
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                return !IsJwtWithValidSecurityAlgorithm(validatedToken) ? null : principal;
            }
            catch (Exception) {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken) {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                       StringComparison.InvariantCultureIgnoreCase);
        }
        private async Task<AuthenticationResult> GenerateAuthenticationResultForUser(IdentityUser user) {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim("id", user.Id)
                }),
                Expires = DateTime.UtcNow.Add(_jwtSettings.TokenLifetime),
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var refreshToken = new RefreshToken {
                JwtId = token.Id,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            await _dataContext.RefreshTokens.AddAsync(refreshToken);
            await _dataContext.SaveChangesAsync();
            return new AuthenticationResult {
                Success = true,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }
    }
}