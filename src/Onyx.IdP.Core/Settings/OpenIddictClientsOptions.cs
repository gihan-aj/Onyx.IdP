namespace Onyx.IdP.Core.Settings
{
    public class OpenIddictClientsOptions
    {
        public const string SectionName = "OpenIddictClients";

        public ClientConfig OmsClient { get; set; } = new();
        public ClientConfig OmsApi { get; set; } = new();
    }

    public class ClientConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public List<string> RedirectUris { get; set; } = new();
    }
}
