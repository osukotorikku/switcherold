using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RippleServerSwitcher
{
    public partial class SettingsForm : Form
    {
        private bool testingHosts = false;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private string uiHostsEntry(HostsEntry e) => String.Format("{0,-12} -> {1}", e.domain, e.ip);

        private void certificateButton_Click(object sender, EventArgs e)
        {
            if (Program.Switcher.CertificateManager.IsCertificateInstalled())
                Program.Switcher.CertificateManager.RemoveCertificates();
            else
                Program.Switcher.CertificateManager.InstallCertificate();
            updateCertificateStatus();
        }

        private void updateCertificateStatus()
        {
            bool installed = Program.Switcher.CertificateManager.IsCertificateInstalled();
            certificateLabel.Text = installed ? "INSTALLED" : "NOT INSTALLED";
            certificateLabel.ForeColor = installed ? Color.Green : Color.Red;
            certificateButton.Text = (installed ? "Uninstall" : "Install") + " certificate";

            certificateInfo.Text = "Certificate by "+Program.Switcher.CertificateManager.GetCertificatePublisher();
        }

        private async Task updateHostsFileStatus()
        {
            HostsFile h = new HostsFile();
            try
            {
                await h.Parse();
                hostsFileReadableLabel.Text = "YES";
                hostsFileReadableLabel.ForeColor = Color.Green;
            }
            catch
            {
                hostsFileReadableLabel.Text = "NO";
                hostsFileReadableLabel.ForeColor = Color.Red;
            }

            ArbitraryHostsEntry testEntry = new ArbitraryHostsEntry(String.Format("# rss test {0}", Guid.NewGuid().ToString()));
            hostsFileWritableLabel.Text = "...";
            hostsFileWritableLabel.ForeColor = Color.Blue;

            try
            {
                h.Entries.Add(testEntry);
                await h.Write();
                await h.Parse();
                if (!h.Entries.Any(x => x is ArbitraryHostsEntry && ((ArbitraryHostsEntry)x).Equals(testEntry)))
                    throw new Exception();
                h.Entries.RemoveAll(x => x.ToString() == testEntry.ToString());
                await h.Write();
                await h.Parse();
                hostsFileWritableLabel.Text = "YES";
                hostsFileWritableLabel.ForeColor = Color.Green;
            }
            catch
            {
                hostsFileWritableLabel.Text = "NO";
                hostsFileWritableLabel.ForeColor = Color.Red;
            }

            bool ripple = Program.Switcher.IsConnectedToRipple();
            hostsFileRippleLabel.Text = ripple ? "YES" : "NO";
            hostsFileRippleLabel.ForeColor = ripple ? Color.Green : Color.Yellow;
            
        }

        private async void SettingsForm_Shown(object sender, EventArgs e)
        {
            foreach (HostsEntry entry in Program.Switcher.RippleHostsEntries)
                currentDomains.Items.Add(uiHostsEntry(entry));
            foreach (HostsEntry entry in Switcher.FallbackOfflineIPs)
                fallbackDomains.Items.Add(uiHostsEntry(entry));
            updateCertificateStatus();
            await updateHostsFileStatus();
            await updateIPServerStatus();
        }

        private async void hostsHelpButton_Click(object sender, EventArgs e)
        {
            if (testingHosts)
                return;

            try
            {
                testingHosts = true;
                hostsHelpButton.Enabled = false;
                await updateHostsFileStatus();
                hostsHelpButton.Enabled = true;
            }
            finally
            {
                testingHosts = false;
            }
        }

        private async Task updateIPServerStatus()
        {
            try
            {
                await Program.Switcher.UpdateIPs();
                ipServerStatusLabel.Text = "ONLINE";
                ipServerStatusLabel.ForeColor = Color.Green;
            }
            catch
            {
                ipServerStatusLabel.Text = "OFFLINE";
                ipServerStatusLabel.ForeColor = Color.Red;
            }
        }

      

        private void closeButton_Click(object sender, EventArgs e) => Close();
    }

    class ConnectionCheckerException : Exception { }
    class NonOkResponse : ConnectionCheckerException
    {
        public HttpStatusCode StatusCode;
        public NonOkResponse(HttpStatusCode code) => StatusCode = code;
    }
    class CertificateException : ConnectionCheckerException { }
    class NoRedirectionException : ConnectionCheckerException { }

    class ConnectionChecker
    {
        public string Endpoint;
        private readonly HttpClient httpClient = new HttpClient();

        public ConnectionChecker(string endpoint)
        {
            this.Endpoint = endpoint;
        }

        public async Task Check()
        {
            try
            {
                using (HttpResponseMessage result = await httpClient.GetAsync(Endpoint))
                {
                    if (result.StatusCode != HttpStatusCode.OK)
                        throw new NonOkResponse(result.StatusCode);
                    if (!((await result.Content.ReadAsStringAsync()).ToLower().Contains("kurikku")))
                        throw new NoRedirectionException();
                }
            }
            catch (HttpRequestException ex) when (ex.InnerException != null && ex.InnerException.InnerException is AuthenticationException)
            {
                throw new CertificateException();
            }
        }
    }
}
