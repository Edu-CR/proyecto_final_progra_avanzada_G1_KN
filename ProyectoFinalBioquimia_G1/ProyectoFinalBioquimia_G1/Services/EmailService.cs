using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading.Tasks;

namespace ProyectoFinalBioquimia_G1.Services
{
    public class EmailService
    {
        public async Task EnviarCorreo(string para, string asunto, string html)
        {
            var cuentaCorreo = "zelayompa@gmail.com";
            var contrasenaCorreo = "zino ssxh uijj asgf";

            if (string.IsNullOrEmpty(contrasenaCorreo))
                return;

            var mensaje = new MimeMessage();
            mensaje.From.Add(MailboxAddress.Parse(cuentaCorreo));
            mensaje.To.Add(MailboxAddress.Parse(para));
            mensaje.Subject = asunto;
            mensaje.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(cuentaCorreo, contrasenaCorreo);
                await smtp.SendAsync(mensaje);
                await smtp.DisconnectAsync(true);
            }
        }
    }
}