using System;
using System.Collections.Generic;

namespace Elect.DomainObjects
{
	public class ResultProvider
	{
		public Guid Id;
		public string Name;
		public Poll Poll;

		public IList<Candidate> GetCandidates()
		{
			return Poll.Candidates;
		}
	}
}