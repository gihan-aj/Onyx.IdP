
namespace Onyx.IdP.Web.Features.Connect
{
    public class ConsentViewModel
    {
        public string ApplicationName { get; set; } = string.Empty;
        public IEnumerable<string> Scopes { get; set; } = new List<string>();
        public IEnumerable<string> ScopeDescriptions { get; set; } = new List<string>();
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
