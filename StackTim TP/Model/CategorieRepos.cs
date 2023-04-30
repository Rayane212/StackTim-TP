using Dapper;
using System.Data.SqlClient;

namespace StackTim_TP.Model
{
    public class CategorieRepos
    {
        readonly IConfiguration? _configuration;

        public CategorieRepos(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public int InsertCategorie(CategorieEntity categorie)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Insert into Categorie(codeCategorie, nomCategorie, descriptifCategorie) values (@codecategorie, @nomCategorie, @descriptifCategorie) ", categorie);
        }


        public List<CategorieEntity> GetAllCategorie()
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<CategorieEntity>("Select * from Categorie").ToList(); ;
        }

        public CategorieEntity GetByCodeCategorie(string codeCategorie)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<CategorieEntity>("Select * from Categorie where codeCategorie = @CodeCategorie", new { CodeCategorie = codeCategorie });

        }
        public CategorieEntity GetByIdCategorie(int id)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<CategorieEntity>("Select * from Categorie where idCategorie = @Id", new { Id = id });

        }
        public int UpdateCategorie(CategorieEntity categorie)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update Categorie set codeCategorie = @CodeCategorie, nomCategorie = @NomCategorie, descriptifCategorie = @descriptifCategorie where idCategorie = @idCategorie", categorie);

        }

        public int DeleteCategorie(int id)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from Categorie where idCategorie = @Id", new { Id = id });

        }

    }
}
