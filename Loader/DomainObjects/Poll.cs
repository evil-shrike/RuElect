using System;
using System.Collections.Generic;

namespace Elect.DomainObjects
{
	public class Poll
	{
		public Guid Id { get; set; }
		public String Name { get; set; }
		public IList<Candidate> Candidates { get; set; }
		//public DateTime Date;
	}
}