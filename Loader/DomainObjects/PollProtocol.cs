using System;
using System.Collections.Generic;

namespace Elect.DomainObjects
{
	public class PollProtocol
	{
		public Guid Id;
		public ResultProvider Provider;
		public Region Region;
		public Int32 Comission;
		public Int32[] Results;
		public List<PollProtocolImage> Images;

		public bool EqualsTo(PollProtocol protocol)
		{
			return true;
		}
	}
}