using BAU_Plagiarism_System.Core.DTOs;
using BAU_Plagiarism_System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BAU_Plagiarism_System.Core.Services
{
    /// <summary>
    /// Service xác thực và phân quyền người dùng (JWT)
    /// </summary>
    public class AuthService
    {
        private readonly BAUDbContext _context;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly int _jwtExpiryMinutes;

        public AuthService(BAUDbContext context, string jwtSecret = "BAU_Plagiarism_System_Secret_Key_2024_Very_Long_And_Secure", 
            string jwtIssuer = "BAU_Plagiarism_System", int jwtExpiryMinutes = 480)
        {
            _context = context;
            _jwtSecret = jwtSecret;
            _jwtIssuer = jwtIssuer;
            _jwtExpiryMinutes = jwtExpiryMinutes;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            // Find user
            var user = await _context.Users
                .Include(u => u.Faculty)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == dto.Username.ToLower() && u.IsActive);

            if (user == null) return null;

            // Verify password
            if (!UserService.VerifyPassword(dto.Password, user.PasswordHash))
                return null;

            // Update last login
            user.LastLoginDate = DateTime.Now;

            // Daily Check Limit Reset handle on login too
            if (user.LastCheckResetDate == null || user.LastCheckResetDate.Value.Date < DateTime.Now.Date)
            {
                user.ChecksUsedToday = 0;
                user.LastCheckResetDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    StudentId = user.StudentId,
                    LecturerId = user.LecturerId,
                    FacultyId = user.FacultyId,
                    FacultyName = user.Faculty?.Name,
                    DepartmentId = user.DepartmentId,
                    DepartmentName = user.Department?.Name,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate,
                    LastLoginDate = user.LastLoginDate,
                    DailyCheckLimit = user.DailyCheckLimit,
                    ChecksUsedToday = user.ChecksUsedToday
                }
            };
        }

        private string GenerateJwtToken(Data.Models.User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            if (!string.IsNullOrEmpty(user.StudentId))
                claims.Add(new Claim("StudentId", user.StudentId));

            if (!string.IsNullOrEmpty(user.LecturerId))
                claims.Add(new Claim("LecturerId", user.LecturerId));

            if (user.FacultyId.HasValue)
                claims.Add(new Claim("FacultyId", user.FacultyId.Value.ToString()));

            if (user.DepartmentId.HasValue)
                claims.Add(new Claim("DepartmentId", user.DepartmentId.Value.ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
                Issuer = _jwtIssuer,
                Audience = _jwtIssuer,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtIssuer,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
