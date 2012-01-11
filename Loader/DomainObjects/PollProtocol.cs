using System;
using System.Collections.Generic;

namespace Elect.DomainObjects
{
	public class PollProtocol
	{
		public Guid Id { get; set; }
		public ResultProvider Provider { get; set; }
		public Region Region { get; set; }
		public Int32 Comission { get; set; }
		public Int32[] Results { get; set; }
		public List<PollProtocolImage> Images { get; set; }

		public bool EqualsTo(PollProtocol protocol)
		{
			// TODO
			return true;
		}
	}
}