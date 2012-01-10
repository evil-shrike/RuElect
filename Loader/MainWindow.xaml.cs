using System;
using System.Collections.Generic;
using System.Windows;

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

	public class Poll
	{
		public Guid Id;
		public String Name;
		public IList<Candidate> Candidates;
		//public DateTime Date;
	}
	
	public class Candidate
	{
		public Guid Id;
		public Poll Poll;
		public string Name;
	}

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

	public class PollProtocolImage
	{
		public string Uri;
		public byte[] Image;
	}

	public class Region
	{
		public Guid Id;
		public String Name;
		public bool IsNew;
	}

	public class Comission
	{
		public Guid Id;
		public Region Region;
		public int Number;
	}
}

namespace Elect.Loader
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}
	}
}
