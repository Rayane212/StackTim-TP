namespace StackTim_TP.Model
{
    public class ProjetsEntity
    {
        public int idProjet { get; set; }
        public string codeProjet { get; set; }
        public string nomProjet { get; set; }
        public string? descriptifProjet { get; set; }
        public DateTime? dateCreation { get; set; }
        public string? creerPar { get; set; }
        public string? EtatDuProjet { get; set; }
        public string codeUtilisateur { get; set; }


    }
}
