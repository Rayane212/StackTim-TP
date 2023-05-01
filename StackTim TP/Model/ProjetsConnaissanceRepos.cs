using Dapper;
using System.Data.SqlClient;

namespace StackTim_TP.Model
{
    public class ProjetsConnaissanceRepos
    {
        readonly IConfiguration? _configuration;

        public ProjetsConnaissanceRepos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int InsertProjetConnaissance(ProjetsConnaissanceEntity projetConnaissance)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Insert into ProjetsConnaissance(codeProjet, codeConnaissance) values (@codeProjet, @codeConnaissance) ", projetConnaissance);
        }

        public List<ProjetsConnaissanceEntity> GetAllProjetConnaissance()
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<ProjetsConnaissanceEntity>("Select * from ProjetsConnaissance").ToList(); ;
        }

        public ProjetsConnaissanceEntity GetByCodeProjet(string codeProjet)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ProjetsConnaissanceEntity>("Select * from Categorie where codeProjet = @CodeProjet", new { CodeProjet = codeProjet });

        }
        public ProjetsConnaissanceEntity GetByCodeConnaissance(string codeConnaissance)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ProjetsConnaissanceEntity>("Select * from Categorie where codeConnaissance = @CodeConnaissance", new { CodeConnaissance = codeConnaissance });

        }
        public ProjetsConnaissanceEntity GetByIdProjetsConnaissance(int id)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ProjetsConnaissanceEntity>("Select * from ProjetsConnaissance where idProjetConnaissance = @Id", new { Id = id });

        }
        public int UpdateProjetsConnaissance(ProjetsConnaissanceEntity projetsConnaissance)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update ProjetsConnaissance set codeProjet = @CodeProjet, codeConnaissance = @CodeConnaissance where idProjetConnaissance = @idProjetConnaissance", projetsConnaissance);

        }

        public int DeleteProjetsConnaissance(int id)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from ProjetsConnaissance where idProjetConnaissance = @Id", new { Id = id });

        }

    }
}
