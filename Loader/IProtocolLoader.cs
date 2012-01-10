using System;
using System.Collections.Generic;
using Elect.DomainObjects;

namespace Elect.Loader
{
	public interface IProtocolLoader: IDisposable
	{
		IEnumerable<PollProtocol> LoadProtocols(IRegionResolver regionResolver);
	}
}