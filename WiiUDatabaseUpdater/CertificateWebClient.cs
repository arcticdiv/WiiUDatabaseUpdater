using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace WiiUDatabaseUpdater
{
    [System.ComponentModel.DesignerCategory("Code")]
    class CertificateWebClient : WebClient
    {
        private readonly X509Certificate2 certificate;

        public CertificateWebClient(X509Certificate2 certificate)
        {
            this.certificate = certificate;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.ClientCertificates.Add(certificate);
            return request;
        }
    }
}
