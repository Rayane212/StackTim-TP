namespace StackTim_TP.Model
{
    public class ConnaissanceEntity
    {
        public int idConnaissance { get; set; }
        public string codeConnaissance { get; set; }
        public string nomConnaissance { get;set; }
        public string? descriptifConnaissance { get; set; }
        public string? codeRessource { get; set; }
        public string codeUtilisateur { get; set; } 
    }
}
