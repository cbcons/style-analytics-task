using ThirdParty;

namespace BadProject.Providers
{
    public class SqlAdvProviderProxy : ISqlAdvProvider
    {
        public Advertisement GetAdv(string webId)
        {
            return SQLAdvProvider.GetAdv(webId);
        }
    }
}