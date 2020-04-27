using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Claims;
using System.Data.SqlClient;
using cw5.Models;

namespace cw5.Handlers{
    public class BasicAuthHandler :
        AuthenticationHandler<AuthenticationSchemeOptions>{
        private const string Conncetion = "Data Source=db-mssql;Initial Catalog=s18911;Integrated Security=True;MultipleActiveResultSets=True";

        public BasicAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock
            ) : base(options, logger, encoder, clock) { 
        
        
        }

        public object AuthenticationHandlerValue { get; private set; }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync(){
            if (!Request.Headers.ContainsKey("Authorization")) {
                return AuthenticateResult.Fail("Missing authorization header");
            }

            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
            var credentialsBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialsBytes).Split(":");

            if (credentials.Length != 2) { 
                return AuthenticateResult.Fail("Incorrect header value");
            }

            //zapytanie do bazy danych
            Student student = new Student();
            using (SqlConnection conn = new SqlConnection(Conncetion)) {
                using (SqlCommand command = new SqlCommand()) {
                    command.Connection = conn;
                    command.CommandText = "select IndexNumber, FirstName, LastName, Birthdate , IdEnrollment from Student where IndexNumber = @index and UserPassword = @password";

                    command.Parameters.AddWithValue("index", credentials[0]);
                    command.Parameters.AddWithValue("password", credentials[1]);

                    conn.Open();
                    var sqlReader = command.ExecuteReader();

                    if (sqlReader.Read())
                    {
                        student.IndexNumber = sqlReader["IndexNumber"].ToString();
                        student.FirstName = sqlReader["FirstName"].ToString();
                        student.LastName = sqlReader["LastName"].ToString();
                        student.Birthdate = sqlReader["Birthdate"].ToString();
                        student.IdEnrollment = Int32.Parse(sqlReader["IdEnrollment"].ToString());
                    }
                    else {
                        return AuthenticateResult.Fail("Incorrect header value");
                    }
                    sqlReader.Close();
                }
            }

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, student.IndexNumber),
                new Claim(ClaimTypes.Role, "student")
            };

            var identity = new ClaimsIdentity(claims,Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal,Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}
