using System;

namespace Elect.DomainObjects
{
	public class Comission
	{
		public Guid Id { get; set; }
		public Region Region { get; set; }
		public int Number { get; set; }
	}
}