using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

				/// <summary>
		/// Only for design-time support. DO NOT USE
		/// </summary>
		public AnalyzeViewModel()
			: base(null)
		{}

		public AnalyzeViewModel(ILogger logger): base(logger)
		{
			m_logger = logger;
			ReloadCommand  = new DelegateCommand(() => reloadAsync());
			PrevComissionProtocol = new DelegateCommand(onPrevComission);
			NextComissionProtocol = new DelegateCommand(onNextComission);
			MaxImageWidth = 100;
			AuxProtocolColumnsWidth = 0;
		}

		public Repository Repository { get; set; }

		public ICommand ReloadCommand { get; set; }

		public ICommand PrevComissionProtocol { get; set; }
		public ICommand NextComissionProtocol { get; set; }

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

		void onPrevComission()
		{
			if (Comissions == null || Comissions.Count == 0)
				return;
			if (SelectedComission == null)
				SelectedComission = Comissions[0];
			else
			{
				int idx = Comissions.IndexOf(SelectedComission);
				if (idx > 0)
				{
					idx--;
					SelectedComission = Comissions[idx];
				}
			}
		}
		void onNextComission()
		{
			if (Comissions == null || Comissions.Count == 0)
				return;
			if (SelectedComission == null)
				SelectedComission = Comissions[0];
			else
			{
				int idx = Comissions.IndexOf(SelectedComission);
				if (idx < Comissions.Count - 1)
				{
					idx++;
					SelectedComission = Comissions[idx];
				}
			}

		}

		private void reloadProtocols(Comission comission)
		{
			Tcs = new CancellationTokenSource();

			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						IsLoading = true;
						IEnumerable<PollProtocol> protocols = Repository.GetComissionProtocols(comission);
						Action action =
							() =>
								{
									Protocols = new ObservableCollection<PollProtocol>(protocols.OrderBy(p => p.Provider.Name));
								};
						if (Application.Current != null)
							Application.Current.Dispatcher.Invoke(action);
						else
							action();
					})
				.ContinueWith(tearDown);
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
					onSelectedProtocolChanged(value);
				}
			}
		}

		private void onSelectedProtocolChanged(PollProtocol protocol)
		{
			
		}

		private int m_maxImageWidth;
		public int MaxImageWidth
		{
			get { return m_maxImageWidth; }
			set
			{
				m_maxImageWidth = value;
				raisePropertyChangedEvent("MaxImageWidth");
			}
		}

		private int m_auxProtocolColumnsWidth;
		public int AuxProtocolColumnsWidth
		{
			get { return m_auxProtocolColumnsWidth; }
			set
			{
				m_auxProtocolColumnsWidth = value;
				raisePropertyChangedEvent("AuxProtocolColumnsWidth");
			}
		}

		private bool m_bShowAuxColumns;
		public bool ShowAuxColumns
		{
			get { return m_bShowAuxColumns; }
			set
			{
				m_bShowAuxColumns = value;
				raisePropertyChangedEvent("ShowAuxColumns");
				AuxProtocolColumnsWidth = value ? 10 : 0;
			}
		}
	}

	public class GridViewColumnVisibilityManager
	{
		static void UpdateListView(ListView lv)
		{
			GridView gridview = lv.View as GridView;
			if (gridview == null || gridview.Columns == null) return;
			List<GridViewColumn> toRemove = new List<GridViewColumn>();
			foreach (GridViewColumn gc in gridview.Columns)
			{
				if (GetIsVisible(gc) == false)
				{
					toRemove.Add(gc);
				}
			}
			foreach (GridViewColumn gc in toRemove)
			{
				gridview.Columns.Remove(gc);
			}
		}

		public static bool GetIsVisible(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsVisibleProperty);
		}

		public static void SetIsVisible(DependencyObject obj, bool value)
		{
			obj.SetValue(IsVisibleProperty, value);
		}

		public static readonly DependencyProperty IsVisibleProperty =
			DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(GridViewColumnVisibilityManager), 
			new UIPropertyMetadata(true/*, new PropertyChangedCallback(OnVisibleChanged)*/));
/*

		private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ListView view = d as ListView;
			if (view != null)
			{
				bool enabled = (bool) e.NewValue;
				if (enabled)
				{
					
				}
			}

		}

*/

		public static bool GetEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnabledProperty);
		}

		public static void SetEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(EnabledProperty, value);
		}

		public static readonly DependencyProperty EnabledProperty =
			DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(GridViewColumnVisibilityManager), new UIPropertyMetadata(false,
				new PropertyChangedCallback(OnEnabledChanged)));

		private static void OnEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			ListView view = obj as ListView;
			if (view != null)
			{
				bool enabled = (bool)e.NewValue;
				if (enabled)
				{
					view.Loaded += (sender, e2) =>
					{
						UpdateListView((ListView)sender);
					};
					view.TargetUpdated += (sender, e2) =>
					{
						UpdateListView((ListView)sender);
					};
					view.DataContextChanged += (sender, e2) =>
					{
						UpdateListView((ListView)sender);
					};
				}
			}
		}
	}

}