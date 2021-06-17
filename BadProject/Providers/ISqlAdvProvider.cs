using ThirdParty;

namespace BadProject.Providers
{
    public interface ISqlAdvProvider
    {
        Advertisement GetAdv(string webId);
    }
}