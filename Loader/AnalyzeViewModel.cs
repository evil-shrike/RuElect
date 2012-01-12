using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Elect.DomainObjects;
using Elect.Loader.Common;

namespace Elect.Loader
{
	public class AnalyzeViewModel : ImportViewModelBase
	{
		private readonly ILogger m_logger;
		private Region m_selectedRegion;
		private Comission m_selectedComission;
		private PollProtocol m_selectedProtocol;
		private ObservableCollection<Region> m_regions;
		private ObservableCollection<Comission> m_comissions;
		private ObservableCollection<PollProtocol> m_protocols;

		public AnalyzeViewModel(ILogger logger): base(logger)
		{
			m_logger = logger;
			ReloadCommand  = new DelegateCommand(() => reloadAsync());
		}

		public Repository Repository { get; set; }

		public ICommand ReloadCommand { get; set; }

		internal Task reloadAsync()
		{
			SelectedComission = null;
			SelectedRegion = null;

			Tcs = new CancellationTokenSource();

			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						IsLoading = true;
						Repository.Initialize();

						if (Tcs.IsCancellationRequested)
							return;

						IEnumerable<Region> regions = Repository.GetRegions();
						Action action =
							() =>
								{
									Regions = new ObservableCollection<Region>(regions.OrderBy(r => r.Name));
								};
						if (Application.Current != null)
							Application.Current.Dispatcher.Invoke(action);
						else
							action();
					})
				.ContinueWith(tearDown);

			return TaskLoading;
		}

		public ObservableCollection<Region> Regions
		{
			get { return m_regions; }
			set
			{
				m_regions = value;
				raisePropertyChangedEvent("Regions");
			}
		}

		public ObservableCollection<Comission> Comissions
		{
			get { return m_comissions; }
			set
			{
				m_comissions = value;
				raisePropertyChangedEvent("Comissions");
			}
		}

		public Region SelectedRegion
		{
			get { return m_selectedRegion; }
			set
			{
				var old = m_selectedRegion;
				m_selectedRegion = value;
				raisePropertyChangedEvent("SelectedRegion");
				if (old != value)
				{
					if (value == null)
						Comissions.Clear();
					else
						reloadComissions(m_selectedRegion);
				}
			}
		}

		private void reloadComissions(Region region)
		{
			IEnumerable<Comission> comissions = Repository.GetComissions(region.Id);
			Comissions = new ObservableCollection<Comission>(comissions.OrderBy(c => c.Number));
		}

		public Comission SelectedComission
		{
			get { return m_selectedComission; }
			set
			{
				var old = m_selectedComission;
				m_selectedComission = value;
				raisePropertyChangedEvent("SelectedComission");
				if (old != m_selectedComission)
				{
					if (value == null)
						Protocols.Clear();
					else
						reloadProtocols(value);
				}
			}
		}

		private void reloadProtocols(Comission comission)
		{
			IEnumerable<PollProtocol> protocols = Repository.GetComissionProtocols(comission);
			Protocols = new ObservableCollection<PollProtocol>(protocols.OrderBy(p => p.Provider.Name));
		}

		public ObservableCollection<PollProtocol> Protocols
		{
			get { return m_protocols; }
			set
			{
				m_protocols = value;
				raisePropertyChangedEvent("Protocols");
			}
		}


		public PollProtocol SelectedProtocol
		{
			get { return m_selectedProtocol; }
			set
			{
				var old = m_selectedProtocol;
				m_selectedProtocol = value;
				raisePropertyChangedEvent("SelectedProtocol");
				if (old != m_selectedProtocol)
				{
					
				}
			}
		}

	}
}