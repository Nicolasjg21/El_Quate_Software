namespace ElQuateDePatty.Services
{
    public interface IRastreadorIntentosLogin
    {
        bool EstaBloqueado(string email);

        void RegistrarFallo(string email);

        void Limpiar(string email);
    }
}
