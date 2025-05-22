namespace AliveChecker.Application.Endpoints.Models;

public class Generalita
{
    public CodiceFiscale codiceFiscale { get; set; }
    public string cognome { get; set; }
    public string dataNascita { get; set; }
    public string idSchedaSoggettoANPR { get; set; }
    public LuogoNascita luogoNascita { get; set; }
    public string nome { get; set; }
    public string sesso { get; set; }
}