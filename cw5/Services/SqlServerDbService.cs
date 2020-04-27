using cw3.Models;
using cw5.Models;
using cw5.Request;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cw5.Services
{
    public class SqlServerDbService : IStudentDbService{
        private const string Conncetion = "Data Source=db-mssql;Initial Catalog=s18911;Integrated Security=True;MultipleActiveResultSets=True";
        private int id = 0;
        private int IdStudy = 0;
        private string Date = DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day;
        Enrollment enrollment = new Enrollment();

        public bool PromoteStudents(Promotions promotions)
        {
            Console.WriteLine("promotions");
            
            using (SqlConnection conn = new SqlConnection(Conncetion))
            {
                conn.Open();
                bool isProcedure = false;
                using (SqlCommand command1 = new SqlCommand())
                {
                    command1.Connection = conn;

                    command1.CommandText = "select IdEnrollment , Enrollment.IdStudy from Enrollment inner join Studies on Enrollment.IdStudy = Studies.IdStudy where semester = @Semester and Studies.Name = @StudiesName";
                    command1.Parameters.Add(new SqlParameter("Semester", promotions.Semester));
                    command1.Parameters.Add(new SqlParameter("StudiesName", promotions.Studies));

                    SqlDataReader sqlReader = command1.ExecuteReader();

                    if (!sqlReader.Read()){
                        return false;
                    }
                    else{
                        isProcedure = true;
                    }
                }

                if (isProcedure)
                {
                    using (SqlCommand command2 = new SqlCommand("ProcedurePromoteStudents", conn))
                    {
                        command2.CommandType = System.Data.CommandType.StoredProcedure;
                        command2.Parameters.Add(new SqlParameter("@StudiesName", promotions.Studies));
                        command2.Parameters.Add(new SqlParameter("@Semester", promotions.Semester));

                        command2.ExecuteScalar();
                    }

                    using (SqlCommand command3 = new SqlCommand())
                    {


                        command3.Connection = conn;

                        command3.CommandText = "select IdEnrollment, Enrollment.IdStudy ,StartDate from Enrollment inner join Studies on Enrollment.IdStudy = Studies.IdStudy where semester = @Semester and Studies.Name = @StudiesName";
                        command3.Parameters.Add(new SqlParameter("Semester", promotions.Semester + 1));
                        command3.Parameters.Add(new SqlParameter("StudiesName", promotions.Studies));

                        SqlDataReader sqlReader = command3.ExecuteReader();

                        if (sqlReader.Read())
                        {
                            enrollment.IdEnrollment = Int32.Parse(sqlReader["IdEnrolment"].ToString());
                            enrollment.Semester = promotions.Semester + 1;
                            enrollment.IdStudy = Int32.Parse(sqlReader["IdStudy"].ToString());
                            enrollment.StartDate = sqlReader["StartDate"].ToString();
                        }
                    }
                }
            }
            return true;
        }

        public Enrollment GetEnrollment() {
            return enrollment;
        }

        public bool CheckStudiesDb(EnrollStudentRequest enrollRequest){

            using (SqlConnection conn = new SqlConnection(Conncetion))
            {
                conn.Open();

                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    SqlCommand command1 = new SqlCommand();
                    command1.Transaction = transaction;
                    command1.Connection = conn;


                    command1.CommandText = "select IdStudy from Studies where name = @studies;";
                    command1.Parameters.AddWithValue("studies", enrollRequest.Studies);

                    SqlDataReader sqlReader = command1.ExecuteReader();

                    if (!sqlReader.Read())
                    {
                        return false;
                    }
                    IdStudy = Int32.Parse(sqlReader["IdStudy"].ToString());
                    sqlReader.Close();
                    command1.Dispose();



                    SqlCommand command2 = new SqlCommand();
                    command2.Transaction = transaction;
                    command2.Connection = conn;
                    command2.CommandText = "select IdEnrollment from Enrollment where Semester = @Semester and IdStudy = @IdStudy and StartDate = (select max(StartDate) from Enrollment where Semester = @Semester and IdStudy = @IdStudy);";
                    command2.Parameters.Add(new SqlParameter("Semester", 7));
                    command2.Parameters.Add(new SqlParameter("IdStudy", IdStudy));
                    sqlReader = command2.ExecuteReader();


                    if (!sqlReader.Read())
                    {
                        sqlReader.Close();
                        command2.Dispose();
                        SqlCommand command3 = new SqlCommand();
                        command3.Transaction = transaction;
                        command3.Connection = conn;
                        command3.CommandText = "select (max(IdEnrollment) + 1) from Enrollment";
                        sqlReader = command3.ExecuteReader();

                        if (sqlReader.Read())
                        {
                            id = Int32.Parse(sqlReader[0].ToString());
                            sqlReader.Close();
                            command3.Dispose();
                            Console.WriteLine("ID : " + id);
                            SqlCommand command4 = new SqlCommand();
                            command4.Transaction = transaction;
                            command4.Connection = conn;
                            command4.CommandText = "insert into enrollment values(@id,7,@IdStudy,@Date)";
                            command4.Parameters.Add(new SqlParameter("@id", id));
                            command4.Parameters.Add(new SqlParameter("IdStudy", IdStudy));
                            command4.Parameters.Add(new SqlParameter("@Date", Date));

                            command4.ExecuteNonQuery();

                            command4.Dispose();
                        }
                        command3.Dispose();
                    }
                    command2.Dispose();
                    sqlReader.Close();

                    SqlCommand command5 = new SqlCommand();
                    command5.Transaction = transaction;
                    command5.Connection = conn;
                    command5.CommandText = "select indexNumber from Student where indexNumber = @index";
                    command5.Parameters.Add(new SqlParameter("@index", enrollRequest.IndexNumber));
                    sqlReader = command5.ExecuteReader();
                    if (!sqlReader.Read())
                    {
                        sqlReader.Close();
                        command5.Dispose();
                        Console.WriteLine("Ok");
                        SqlCommand command6 = new SqlCommand();
                        command6.Transaction = transaction;
                        command6.Connection = conn;
                        command6.CommandText = "insert into Student values(@index,@First,@Last,@Date,@id)";
                        command6.Parameters.Add(new SqlParameter("@index", enrollRequest.IndexNumber));
                        command6.Parameters.Add(new SqlParameter("@First", enrollRequest.FirstName));
                        command6.Parameters.Add(new SqlParameter("@Last", enrollRequest.LastName));
                        command6.Parameters.Add(new SqlParameter("@Date", Date));
                        command6.Parameters.Add(new SqlParameter("@id", id));

                        command6.ExecuteNonQuery();
                        transaction.Commit();
                        command6.Dispose();

                        using (SqlCommand query = new SqlCommand()){
                            query.Transaction = transaction;
                            query.Connection = conn;

                            query.CommandText = " select IdEnrollment, Semester ,Enrollment.IdStudy ,StartDate from Enrollment inner join Studies on Enrollment.IdStudy = Studies.IdStudy where IdEnrollment = @id";
                            query.Parameters.Add(new SqlParameter("id", id));

                             sqlReader = query.ExecuteReader();

                            if (sqlReader.Read())
                            {
                                enrollment.IdEnrollment = Int32.Parse(sqlReader["IdEnrolment"].ToString());
                                enrollment.Semester = Int32.Parse(sqlReader["Semester"].ToString());
                                enrollment.IdStudy = Int32.Parse(sqlReader["IdStudy"].ToString());
                                enrollment.StartDate = sqlReader["StartDate"].ToString();
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                    sqlReader.Close();
                    command5.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("EXCEPTION");
                    Console.WriteLine(ex);
                    transaction.Rollback();
                }
            }
            return true;
        }

        public Student GetStudent(string index) {
            Regex regex = new Regex(@"^s[0-9]+$");
            Match match = regex.Match(index);
            if (match.Success) {
                Student student = new Student();
                using (SqlConnection conn = new SqlConnection(Conncetion))
                using (SqlCommand command1 = new SqlCommand()) {
                    command1.Connection = conn;
                    command1.CommandText = "select IndexNumber, FirstName, LastName, Birthdate , IdEnrollment from Student where IndexNumber = @index";

                    command1.Parameters.AddWithValue("index", index);

                    conn.Open();
                    var sqlReader = command1.ExecuteReader();

                    if (sqlReader.Read())
                    {
                        student.IndexNumber = sqlReader["IndexNumber"].ToString();
                        student.FirstName = sqlReader["FirstName"].ToString();
                        student.LastName = sqlReader["LastName"].ToString();
                        student.Birthdate = sqlReader["Birthdate"].ToString();
                        student.IdEnrollment = Int32.Parse(sqlReader["IdEnrollment"].ToString());
                        return student;
                    }
                    sqlReader.Close();
                }
            }
            return null;
        }

        public bool LogUser(LoginRequest loginRequest) {
            Student student = new Student();
            using (SqlConnection conn = new SqlConnection(Conncetion))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = conn;
                    command.CommandText = "select IndexNumber, FirstName, LastName, Birthdate , IdEnrollment from Student where IndexNumber = @index and UserPassword = @password";

                    command.Parameters.AddWithValue("index", loginRequest.Login);
                    command.Parameters.AddWithValue("password", loginRequest.Password);

                    conn.Open();
                    var sqlReader = command.ExecuteReader();

                    if (sqlReader.Read()){
                        return true;
                    }
                    sqlReader.Close();
                }
            }
            return false;
        }
    }
}
