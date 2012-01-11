using System;

namespace Elect.DomainObjects
{
	public class Region
	{
		public Guid Id { get; set; }
		public String Name { get; set; }
		public bool IsNew { get; set; }
	}
}