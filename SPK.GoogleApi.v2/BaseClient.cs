using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;

namespace SPK.GoogleApi.v2
{
    public abstract class BaseClient
    {
        internal string ClientId { get; set; }
        internal string ClientSecret { get; set; }
        internal string ProjectId { get; set; }

        public BaseClient(string clientId, string clientSecret, string projectId)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            ProjectId = projectId;
        }

        internal abstract UserCredential Authenticated();

        internal abstract DriveService CreateService();
    }
}