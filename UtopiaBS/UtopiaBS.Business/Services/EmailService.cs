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
            string correoSistema = ConfigurationManager.AppSettings["CorreoSistema"];

            string clave = Environment.GetEnvironmentVariable("UTOPIA_SMTP_PASS");

            if (string.IsNullOrEmpty(clave))
            {
                throw new Exception("No se encontró la variable de entorno UTOPIA_SMTP_PASS.");
            }

            var mensaje = new MailMessage(correoSistema, para, asunto, cuerpoHtml);
            mensaje.IsBodyHtml = true;

            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.EnableSsl = true;

                // ⭐ ESTA LÍNEA ERA LA QUE FALTABA ⭐
                smtp.UseDefaultCredentials = false;

                smtp.Credentials = new NetworkCredential(correoSistema, clave);

                await smtp.SendMailAsync(mensaje);
            }
        }

    }
}
