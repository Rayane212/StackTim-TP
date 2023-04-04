using System.Data.SqlClient;

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

       
    }
}
