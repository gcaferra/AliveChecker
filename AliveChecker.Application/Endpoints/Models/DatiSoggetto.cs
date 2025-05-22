namespace AliveChecker.Application.Endpoints.Models;

public class DatiSoggetto
{
    public Generalita generalita { get; set; }
    public Identificativi identificativi { get; set; }
    public InfoSoggettoEnte[] infoSoggettoEnte { get; set; }
}