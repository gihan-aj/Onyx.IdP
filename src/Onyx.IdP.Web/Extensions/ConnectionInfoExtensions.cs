using System.Net;

namespace Onyx.IdP.Web.Extensions
{
    public static class ConnectionInfoExtensions
    {
        public static bool IsLocal(this ConnectionInfo connection)
        {
            if (connection.RemoteIpAddress != null)
            {
                if (connection.LocalIpAddress != null)
                {
                    return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
                }
                return IPAddress.IsLoopback(connection.RemoteIpAddress);
            }

            // If both are null, it's typically an in-memory test server or local pipe
            return true;
        }
    }
}
