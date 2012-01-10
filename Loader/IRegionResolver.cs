using Elect.DomainObjects;

namespace Elect.Loader
{
	public interface IRegionResolver
	{
		Region GetOrCreate(string name);
		//bool TryGet(string regionName, out Region region);
		bool Contains(string regionName);
	}
}