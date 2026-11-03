using System.Security.Cryptography;
using System.Text;

namespace ProyectoFinalBioquimia_G1.Services
{
    public class TokenService
    {
        public static string GenerarToken()
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            StringBuilder token = new StringBuilder();

            byte[] datos = new byte[10];

            using (var rng = RandomNumberGenerator.Create())
            {
                int limite = 256 - (256 % caracteres.Length);

                while (token.Length < 10)
                {
                    rng.GetBytes(datos);

                    foreach (byte dato in datos)
                    {
                        if (dato < limite)
                        {
                            token.Append(caracteres[dato % caracteres.Length]);

                            if (token.Length == 10)
                                break;
                        }
                    }
                }
            }

            return token.ToString();
        }
    }
}