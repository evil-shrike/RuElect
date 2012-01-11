using System;
using System.Collections.Generic;

namespace Elect.DomainObjects
{
	public class ResultProvider
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public Poll Poll { get; set; }

		public IList<Candidate> GetCandidates()
		{
			return Poll.Candidates;
		}
	}
}