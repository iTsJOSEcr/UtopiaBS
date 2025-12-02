using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace UtopiaBS.Business.Services
{
    public class EmailService
    {
        public static async Task EnviarCorreoAsync(string para, string asunto, string cuerpoHtml)
        {
            string correoSistema = ConfigurationManager.AppSettings["CorreoSistema"];
            string clave = ConfigurationManager.AppSettings["ClaveCorreo"];

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
