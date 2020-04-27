using System;
using cw5.Models;
using cw5.Request;
using Microsoft.AspNetCore.Mvc;
using cw5.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace cw5.Controllers{


    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentsController : ControllerBase{
        private Student student;
        private IStudentDbService dbService;
        private IConfiguration Configuration { get; set; }

        public EnrollmentsController(IStudentDbService dbService,IConfiguration configuration) {
            this.dbService = dbService;
            this.Configuration = configuration;
        }

        [HttpPost]
        [Authorize(Roles = "employee")]
        public IActionResult EnrollStudent(EnrollStudentRequest studentRequest) {
            MapStudent(studentRequest);

            if (dbService.CheckStudiesDb(studentRequest)){
                return Ok(dbService.GetEnrollment());//Created(Url,dbService.GetEnrollment());
            }
            return NotFound();
        }

        [Route("/api/enrollments/promotions")]
        [HttpPost]
        [Authorize(Roles = "employee")]
        public IActionResult PromoteStudents(Promotions promotions) {
            if (dbService.PromoteStudents(promotions)){
                return Ok(dbService.GetEnrollment());//Created(Url,dbService.GetEnrollment());
            }
            else {
                return NotFound();
            }
        }

        [HttpPost]
        public IActionResult Login(LoginRequest loginRequest) {
            Claim [] claims;
            JwtSecurityToken token = new JwtSecurityToken();
            if (dbService.LogUser(loginRequest)){
                claims = new[] {
                new Claim(ClaimTypes.Role, "student")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                token = new JwtSecurityToken(
                    issuer: "Server",
                    audience: "Student",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(10),
                    signingCredentials: creds
                );
            }
            else {
                return NotFound();
            }

            return Ok(new { 
                token = new JwtSecurityTokenHandler().WriteToken(token),
                refereshToken = Guid.NewGuid()
            }); 
        }

        public void MapStudent(EnrollStudentRequest enrollRequest) {
            student = new Student();

            student.IndexNumber = enrollRequest.IndexNumber;
            student.FirstName = enrollRequest.FirstName;
            student.LastName = enrollRequest.LastName;
            student.Birthdate = enrollRequest.Birthdate;

        }
    }
}