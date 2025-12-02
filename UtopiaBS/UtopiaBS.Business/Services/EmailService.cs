using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace UtopiaBS.Business.Services
{
    public class EmailService
    {
        public static async Task EnviarCorreoAsync(string para, string asunto, string cuerpoHtml)
        {
            // El correo viene del web.config (esto está bien)
            string correoSistema = ConfigurationManager.AppSettings["CorreoSistema"];

            //  La clave ahora viene de la VARIABLE DE ENTORNO (NO del web.config)
            string clave = Environment.GetEnvironmentVariable("UTOPIA_SMTP_PASS");

            if (string.IsNullOrEmpty(clave))
            {
                throw new Exception("No se encontró la variable de entorno UTOPIA_SMTP_PASS.");
            }

            var mensaje = new MailMessage(correoSistema, para, asunto, cuerpoHtml);
            mensaje.IsBodyHtml = true;

            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential(correoSistema, clave);
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(mensaje);
            }
        }
    }
}
