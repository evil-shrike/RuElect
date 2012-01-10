using System;
using System.Collections.Generic;

namespace Elect.DomainObjects
{
	public class Poll
	{
		public Guid Id;
		public String Name;
		public IList<Candidate> Candidates;
		//public DateTime Date;
	}
}