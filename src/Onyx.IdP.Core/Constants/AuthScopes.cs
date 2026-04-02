namespace Onyx.IdP.Core.Constants
{
    public static class AuthScopes
    {
        public const string OmsApi = "oms_api";
        public const string IdpApi = "idp_api";

        // Helper to easily get the full prefix when seeding
        public const string OmsApiPermission = "scp:oms_api";
    }
}
