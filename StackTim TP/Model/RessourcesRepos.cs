using Dapper;
using System.Data.SqlClient;

namespace StackTim_TP.Model
{
    public class RessourcesRepos
    {
        readonly IConfiguration? _configuration;

        public RessourcesRepos(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public int InsertRessource(RessourcesEntity Ressource)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Insert into Ressources(codeRessource, nomRessource, datePublication, creerPar, descriptifRessource, codeUtilisateur) values (@codeRessource, @nomRessource, @datePublication, @creerPar, @descriptifRessource, @codeUtilisateur) ", Ressource);
        }


        public List<RessourcesEntity> GetAllRessource(string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<RessourcesEntity>("Select * from Ressources where codeUtilisateur = @CodeUtilisateur", new { CodeUtilisateur = codeUtilisateur }).ToList(); ;
        }

        public RessourcesEntity GetByCodeRessource(string codeRessource, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<RessourcesEntity>("Select * from Ressource where codeRessource = @CodeRessource and codeUtilisateur = @CodeUtilisateur", new { CodeRessource = codeRessource, CodeUtilisateur = codeUtilisateur });

        }
        public RessourcesEntity GetByIdRessource(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<RessourcesEntity>("Select * from Ressources where idRessource = @Id and codeUtilisateur = @CodeUtilisateur", new { Id = id, CodeUtilisateur = codeUtilisateur });

        }
        public int UpdateRessource(RessourcesEntity Ressource)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update Ressources set codeRessource = @CodeRessource, nomRessource = @NomRessource, descriptifRessource = @descriptifRessource where idRessource = @idRessource and codeUtilisateur = @CodeUtilisateur", Ressource);

        }

        public int DeleteRessource(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from Ressources where idRessource = @Id and codeUtilisateur = @CodeUtilisateur", new { Id = id, CodeUtilisateur = codeUtilisateur });

        }

        public async Task<bool> ExistingRessource(string codeRessource, string userId)
        {
            using (var connection = new SqlConnection(_configuration?.GetConnectionString("SQL")))
            {
                await connection.OpenAsync();
                var result = await connection.QuerySingleOrDefaultAsync<int>(
                    "SELECT 1 FROM Ressources WHERE codeRessource = @codeRessource AND  codeUtilisateur = @CodeUtilisateur",
                    new { CodeRessource = codeRessource.ToUpper(), CodeUtilisateur = userId });
                return result != default(int);
            }
        }


    }
}
