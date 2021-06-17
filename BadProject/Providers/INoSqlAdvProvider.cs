using ThirdParty;

namespace BadProject.Providers
{
    public interface INoSqlAdvProvider
    {
        Advertisement GetAdv(string webId);
    }
}