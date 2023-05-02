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
            return oSqlConnection.Execute("Insert into Categorie(codeCategorie, nomCategorie, descriptifCategorie, codeUtilisateur) values (@codecategorie, @nomCategorie, @descriptifCategorie, @codeUtilisateur) ", categorie);
        }


        public List<CategorieEntity> GetAllCategorie(string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<CategorieEntity>("Select * from Categorie where codeUtilisateur = @CodeUtilisateur", new {CodeUtilisateur = codeUtilisateur}).ToList(); ;
        }

        public CategorieEntity GetByCodeCategorie(string codeCategorie, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<CategorieEntity>("Select * from Categorie where codeCategorie = @CodeCategorie and codeUtilisateur = @CodeUtilisateur", new { CodeCategorie = codeCategorie, CodeUtilisateur = codeUtilisateur });

        }
        public CategorieEntity GetByIdCategorie(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<CategorieEntity>("Select * from Categorie where idCategorie = @Id and codeUtilisateur = @CodeUtilisateur", new { Id = id, CodeUtilisateur = codeUtilisateur });

        }
        public int UpdateCategorie(CategorieEntity categorie)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update Categorie set codeCategorie = @CodeCategorie, nomCategorie = @NomCategorie, descriptifCategorie = @descriptifCategorie where idCategorie = @idCategorie and codeUtilisateur = @CodeUtilisateur", categorie);

        }

        public int DeleteCategorie(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from Categorie where idCategorie = @Id and codeUtilisateur = @CodeUtilisateur", new { Id = id, CodeUtilisateur = codeUtilisateur});

        }

        public async Task<bool> ExistingCategorie(string codeCategorie, string userId)
        {
            using (var connection = new SqlConnection(_configuration?.GetConnectionString("SQL")))
            {
                await connection.OpenAsync();
                var result = await connection.QuerySingleOrDefaultAsync<int>(
                    "SELECT 1 FROM Categorie WHERE codeCategorie = @codeCategorie AND  codeUtilisateur = @CodeUtilisateur",
                    new { CodeCategorie = codeCategorie.ToUpper(), CodeUtilisateur = userId });
                return result != default(int);
            }
        }

    }
}
