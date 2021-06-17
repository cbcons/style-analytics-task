using ThirdParty;

namespace BadProject.Providers
{
    public class NoSqlAdvProviderProxy : INoSqlAdvProvider
    {
        private readonly NoSqlAdvProvider _noSqlAdvProvider;

        public NoSqlAdvProviderProxy(NoSqlAdvProvider noSqlAdvProvider)
        {
            _noSqlAdvProvider = noSqlAdvProvider;
        }


        public Advertisement GetAdv(string webId)
        {
            return _noSqlAdvProvider.GetAdv(webId);
        }
    }
}