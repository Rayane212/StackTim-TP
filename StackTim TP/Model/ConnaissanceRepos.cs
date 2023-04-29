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

        public int Insert(ConnaissanceEntity ce)
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

                var oSqlCommand = new SqlCommand("Insert into Connaissance(codeConnaissance, nomConnaissance, descriptifConnaissance, codeRessource) values (@codeConnaissance, @nomConnaissance, @descriptifConnaissance, @codeRessource); select @@identity; ");
                //oSqlCommand.Parameters.Add(oSqlParam);
                oSqlCommand.Parameters.Add(oSqlParam1);
                oSqlCommand.Parameters.Add(oSqlParam2);
                oSqlCommand.Parameters.Add(oSqlParam3);
                oSqlCommand.Parameters.Add(oSqlParam4);

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

        public List<ConnaissanceEntity> GetAllConnaissance()
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<ConnaissanceEntity>("Select * from Connaissance").ToList(); ;
        }

        public ConnaissanceEntity GetByCodeConnaissance(string codeConnaissance)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ConnaissanceEntity>("Select * from Connaissance where codeConnaissance = @CodeConnaissance", new { CodeConnaissance = codeConnaissance });

        }
        public ConnaissanceEntity GetByIdConnaissance(int id)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ConnaissanceEntity>("Select * from Connaissance where idConnaissance = @Id", new { Id = id });

        }

        public int UpdateConnaissance(ConnaissanceEntity ce)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update Connaissance set codeConnaissance = @CodeConnaissance, nomConnaissance = @NomConnaissance, descriptifConnaissance = @DConnaissance, codeRessource = @CodeRessource where idConnaissance = @Id", new { Id = ce.idConnaissance, CodeConnaissance = ce.codeConnaissance, NomConnaissance = ce.nomConnaissance, DConnaissance = ce.descriptifConnaissance, CodeRessource = ce.codeRessource });

        }

        public int DeleteConnaissance(int id)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from Connaissance where idConnaissance = @Id", new { Id = id });

        }
    }
}
