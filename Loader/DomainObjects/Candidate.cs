using System;

namespace Elect.DomainObjects
{
	public class Candidate
	{
		public Guid Id { get; set; }
		public Poll Poll { get; set; }
		public string Name { get; set; }
	}
}