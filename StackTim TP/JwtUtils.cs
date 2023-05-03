using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace StackTim_TP
{
    public class JwtUtils
    {
        public static Dictionary<string, string> DecodeJwt(string tokenString, string secret)
        {
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            SecurityToken validatedToken;
            var claims = handler.ValidateToken(tokenString, validationParameters, out validatedToken).Claims;
            var result = new Dictionary<string, string>();
            foreach (var claim in claims)
            {
                result[claim.Type] = claim.Value;
            }

            return result;
        }

    }
}