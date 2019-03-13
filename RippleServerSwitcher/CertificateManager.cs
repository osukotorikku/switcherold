using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RippleServerSwitcher
{
    class CertificateManager
    {
        public string SubjectName;
        public byte[] Certificate;

        private X509Certificate2Collection FindRippleCertificates(X509Store store)
        {
            // Too bad I can't subclass X509Store...
            return store.Certificates.Find(X509FindType.FindBySubjectName, SubjectName, true);
        }

        public string GetCertificatePublisher() {

            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                X509Certificate2Collection collection = FindRippleCertificates(store);
                if (collection.Count<0) {
                    return "<UNKNOWN>";
                } else {
                    X509Certificate2 lx509 = collection[collection.Count - 1];
                    MessageBox.Show(lx509.Subject);
                    Regex regex = new Regex(@"O=(.+?),");
                    MatchCollection matches = regex.Matches(lx509.Subject.ToString());
                    return matches[0].Groups[1].ToString();
                }
            }
            finally
            {
                store.Close();
            }
        }

        public bool IsCertificateInstalled()
        {
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                return FindRippleCertificates(store).Count > 0;
            }
            finally
            {
                store.Close();
            }
        }

        public void InstallCertificate()
        {
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                if (FindRippleCertificates(store).Count > 0)
                    return;

                X509Certificate2Collection collection = new X509Certificate2Collection();
                collection.Import(Certificate);
                foreach (X509Certificate2 cert in collection)
                    try
                    {
                        store.Add(cert);
                    }
                    catch (CryptographicException)
                    {
                        throw new HumanReadableException(
                            "You must install the certificate.",
                            "The certificate is needed to connect to Ripple through HTTPs. Without it, you won't be able to connect. " +
                            "Click on 'Switch to kurikku', then 'Yes' to install the certificate and switch to Kurikku."
                        );
                    }
            }
            finally
            {
                store.Close();
            }
        }

        public void RemoveCertificates()
        {
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadWrite);
                foreach (X509Certificate2 c in FindRippleCertificates(store))
                    store.Remove(c);
            }
            finally
            {
                store.Close();
            }
        }
    }
}
