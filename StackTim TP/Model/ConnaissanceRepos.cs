using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using Dapper;

namespace StackTim_TP.Model
{
    public class ConnaissanceRepos
    {
        readonly IConfiguration? _configuration;

        public ConnaissanceRepos(IConfiguration configuration)
        {
            _configuration = configuration; 
        }

        public int Insert(ConnaissanceEntity ce, string codeUtilisateur)
        {
            try
            {
                //var ce = new ConnaissanceEntity();
                var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
                var oSqlParam = new SqlParameter("@Id", ce.idConnaissance); 
                var oSqlParam1 = new SqlParameter("@codeConnaissance", ce.codeConnaissance); 
                var oSqlParam2 = new SqlParameter("@nomConnaissance", ce.nomConnaissance); 
                var oSqlParam3 = new SqlParameter("@descriptifConnaissance", ce.descriptifConnaissance);
                var oSqlParam4 = new SqlParameter("@codeRessource", ce.codeRessource) ;
                var oSqlParam5 = new SqlParameter("@codeUtilisateur", codeUtilisateur);


                var oSqlCommand = new SqlCommand("Insert into Connaissance(codeConnaissance, nomConnaissance, descriptifConnaissance, codeRessource, codeUtlisateur) values (@codeConnaissance, @nomConnaissance, @descriptifConnaissance, @codeRessource, @codeUtlisateur); select @@identity; ");
                //oSqlCommand.Parameters.Add(oSqlParam);
                oSqlCommand.Parameters.Add(oSqlParam1);
                oSqlCommand.Parameters.Add(oSqlParam2);
                oSqlCommand.Parameters.Add(oSqlParam3);
                oSqlCommand.Parameters.Add(oSqlParam4);
                oSqlCommand.Parameters.Add(oSqlParam5);

                oSqlCommand.Connection = oSqlConnection;
                oSqlConnection.Open();
                var Idretour = Convert.ToInt32(oSqlCommand.ExecuteScalar());
                oSqlConnection.Close();

                return 1;
            }catch (Exception)
            {
                return -1;
            }
        }

        public int InsertConnaissance(ConnaissanceEntity connaissance)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Insert into Connaissance(codeConnaissance, nomConnaissance, descriptifConnaissance, codeRessource, codeUtilisateur) values (@codeConnaissance, @nomConnaissance, @descriptifConnaissance, @codeRessource, @codeUtilisateur);", connaissance);

        }

        public List<ConnaissanceEntity> GetAllConnaissance(string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<ConnaissanceEntity>("Select * from Connaissance").ToList(); ;
        }

        public ConnaissanceEntity GetByCodeConnaissance(string codeConnaissance, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ConnaissanceEntity>("Select * from Connaissance where codeConnaissance = @CodeConnaissance and CodeUtilisateur = @codeUtilisateur", new { CodeConnaissance = codeConnaissance, CodeUtilisateur = codeUtilisateur });

        }
        public ConnaissanceEntity GetByIdConnaissance(int id, string codeUtilisateur) 
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ConnaissanceEntity>("Select * from Connaissance where idConnaissance = @Id ", new { Id = id});

        }

        public int UpdateConnaissance(ConnaissanceEntity connaissance)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update Connaissance set codeConnaissance = @CodeConnaissance, nomConnaissance = @NomConnaissance, descriptifConnaissance = @descriptifConnaissance, codeRessource = @CodeRessource where idConnaissance = @idConnaissance and CodeUtilisateur = @CodeUtilisateur", connaissance);

        }

        public int DeleteConnaissance(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from Connaissance where idConnaissance = @Id ", new { Id = id, CodeUtilisateur = codeUtilisateur });

        }

       
        public async Task<bool> ExistingConnaissance(string codeConnaissance, string userId)
        {
            using (var connection = new SqlConnection(_configuration?.GetConnectionString("SQL")))
            {
                await connection.OpenAsync();
                var result = await connection.QuerySingleOrDefaultAsync<int>(
                    "SELECT 1 FROM Connaissance WHERE codeConnaissance = @CodeConnaissance",
                    new { CodeConnaissance = codeConnaissance.ToUpper(), CodeUtilisateur = userId });
                return result != default(int);
            }
        }

        public async Task<bool> RedondanceConnaissance(string codeConnaissance, string userId)
        {
            using (var connection = new SqlConnection(_configuration?.GetConnectionString("SQL")))
            {
                await connection.OpenAsync();
                var result = await connection.QuerySingleOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM Connaissance WHERE codeConnaissance = @CodeConnaissance ",
                    new { CodeConnaissance = codeConnaissance.ToUpper(), CodeUtilisateur = userId });

                return result > 0;
            }
        }


    }
}
