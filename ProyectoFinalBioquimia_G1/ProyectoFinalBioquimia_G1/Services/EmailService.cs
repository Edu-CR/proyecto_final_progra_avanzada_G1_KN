
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace ProyectoFinalBioquimia_G1.Services
{
    public class EmailService
    {
        public async Task EnviarCorreo(
            string para,
            string asunto,
            string html)
        {
            // Obtener credenciales desde Windows
            var cuentaCorreo = Environment.GetEnvironmentVariable(
                "BIOQUIMIA_EMAIL");

            var contrasenaCorreo = Environment.GetEnvironmentVariable(
                "BIOQUIMIA_EMAIL_PASSWORD");

            if (string.IsNullOrWhiteSpace(cuentaCorreo) ||
                string.IsNullOrWhiteSpace(contrasenaCorreo))
            {
                throw new InvalidOperationException(
                    "No están configuradas las credenciales del correo.");
            }

            var mensaje = new MimeMessage();

            mensaje.From.Add(
                MailboxAddress.Parse(cuentaCorreo));

            mensaje.To.Add(
                MailboxAddress.Parse(para));

            mensaje.Subject = asunto;

            mensaje.Body = new BodyBuilder
            {
                HtmlBody = html
            }.ToMessageBody();

            using (var smtp = new SmtpClient())
            {
                await smtp.ConnectAsync(
                    "smtp.gmail.com",
                    587,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    cuentaCorreo,
                    contrasenaCorreo);

                await smtp.SendAsync(mensaje);

                await smtp.DisconnectAsync(true);
            }
        }
    }
}
