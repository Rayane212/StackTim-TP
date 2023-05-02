using Dapper;
using System.Data.SqlClient;

namespace StackTim_TP.Model
{
    public class ProjetsRepos
    {
        readonly IConfiguration? _configuration;

        public ProjetsRepos(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public int InsertProjet(ProjetsEntity Projet)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Insert into Projets(codeProjet, nomProjet, descriptifProjet ,dateCreation, creerPar, EtatDuProjet, codeUtilisateur) values (@codeProjet, @nomProjet, @descriptifProjet, @dateCreation, @creerPar, @EtatDuProjet , @codeUtilisateur) ", Projet);
        }


        public List<ProjetsEntity> GetAllProjet(string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Query<ProjetsEntity>("Select * from Projets where codeUtilisateur = @CodeUtilisateur", new { CodeUtilisateur = codeUtilisateur }).ToList(); ;
        }

        public ProjetsEntity GetByCodeProjet(string codeProjet, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ProjetsEntity>("Select * from Projet where codeProjet = @CodeProjet and codeUtilisateur = @CodeUtilisateur", new { CodeProjet = codeProjet, CodeUtilisateur = codeUtilisateur });

        }
        public ProjetsEntity GetByIdProjet(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.QueryFirstOrDefault<ProjetsEntity>("Select * from Projets where idProjet = @Id and codeUtilisateur = @CodeUtilisateur", new { Id = id, CodeUtilisateur = codeUtilisateur });

        }
        public int UpdateProjet(ProjetsEntity Projet)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("Update Projets set codeProjet = @CodeProjet, nomProjet = @NomProjet, descriptifProjet = @descriptifProjet, EtatDuProjet = @EtatDuProjet where idProjet = @idProjet and codeUtilisateur = @CodeUtilisateur", Projet);

        }

        public int DeleteProjet(int id, string codeUtilisateur)
        {
            var oSqlConnection = new SqlConnection(_configuration?.GetConnectionString("SQL"));
            return oSqlConnection.Execute("delete from Projets where idProjet = @Id and codeUtilisateur = @CodeUtilisateur", new { Id = id, CodeUtilisateur = codeUtilisateur });

        }

        public async Task<bool> ExistingProjet(string codeProjet, string userId)
        {
            using (var connection = new SqlConnection(_configuration?.GetConnectionString("SQL")))
            {
                await connection.OpenAsync();
                var result = await connection.QuerySingleOrDefaultAsync<int>(
                    "SELECT 1 FROM Projets WHERE codeProjet = @codeProjet AND  codeUtilisateur = @CodeUtilisateur",
                    new { CodeProjet = codeProjet.ToUpper(), CodeUtilisateur = userId });
                return result != default(int);
            }
        }


    }
}
