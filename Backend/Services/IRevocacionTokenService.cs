namespace ElQuateDePatty.Services
{
    public interface IRevocacionTokenService
    {
        void Revocar(string idSesion, DateTimeOffset expiraEn);

        bool EstaRevocada(string idSesion);
    }
}
