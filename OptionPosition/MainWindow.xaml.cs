using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Logging;
using Ecng.Configuration;
using StockSharp.Configuration;
using Ecng.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ecng.Serialization;
using System.IO;
using StockSharp.Localization;
using Ecng.Common;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using MoreLinq;
using System.Collections.ObjectModel;
using System.Threading;

namespace OptionPosition
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public readonly Connector Connector;
		private const string _settingsFile = "connection.xml";
		private bool _isConnected;
		private Security _currentBaseActive;
		private ObservableCollection<SecurityPosition> _currentOptionPosition = new ObservableCollection<SecurityPosition>();

		public MainWindow()
		{
			InitializeComponent();

			var logManager = new LogManager();
			logManager.Listeners.Add(new FileLogListener("OptionPosition.log"));
			var entityRegistry = ConfigManager.GetService<IEntityRegistry>();
			var storageRegistry = ConfigManager.GetService<IStorageRegistry>();
			Connector = new Connector(entityRegistry, storageRegistry);
			logManager.Sources.Add(Connector);			
            InitConnector();
			Desk.MarketDataProvider = Connector;
			Desk.SecurityProvider = Connector;
			StrikesCount.SelectedIndex = 0;
			dataGridPosition.ItemsSource = _currentOptionPosition;
			
		}

		private void InitConnector()
		{
			// subscribe on connection successfully event
			Connector.Connected += () =>
			{
				this.GuiAsync(() => ChangeConnectStatus(true));
			};

			// subscribe on connection error event
			Connector.ConnectionError += error => this.GuiAsync(() =>
			{
				ChangeConnectStatus(false);
				MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
			});

			Connector.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

			// subscribe on error event
			Connector.Error += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, type, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

			Connector.NewSecurities += securities => securityPicker.Securities.AddRange(securities);
			
			// set market data provider
			securityPicker.MarketDataProvider = Connector;

			try
			{
				if (File.Exists(_settingsFile))
					Connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
			}
			catch
			{
			}

			if (Connector.StorageAdapter == null)
				return;

			if (!File.Exists("StockSharp.db"))
				File.WriteAllBytes("StockSharp.db", Properties.Resources.StockSharp);

			Connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(3);
			Connector.StorageAdapter.Load();
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectMenuItem.Header = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			Connector.Configure(this);
			new XmlSerializer<SettingsStorage>().Serialize(Connector.Save(), _settingsFile);
		}

		private void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				Connector.Connect();
			}
			else
			{
				Connector.Disconnect();
			}
		}

		private void SetOptionsToOptionDesk()
		{
			if (_currentBaseActive == null)
				return;
			var tmpOptions = Connector.Securities
				.Where(opt => opt.Type == SecurityTypes.Option && opt.UnderlyingSecurityId == _currentBaseActive.Id)
				.ToList();
			if (tmpOptions.Count >= 2)
			{
				//фильтруем по датам экспираций
				tmpOptions = tmpOptions.Where(opt => opt.ExpiryDate?.Date == (DateTime)ExpirationDates.SelectedItem).ToList();
				//фильтруем по страйкам
				if (((string)((ComboBoxItem)StrikesCount.SelectedItem).Content) != "ALL" && tmpOptions.Count >= 2)
				{
					var strikes = tmpOptions
						.Select(opt => opt.Strike.Value)
						.Distinct()
						.OrderBy(strike => strike)
						.Select((strike, index) => new { strike = strike, index = index})
						.ToList();
					decimal strikeStep = strikes.Join(strikes, s1 => s1.index, s2 => s2.index + 1, (s1, s2) => s1.strike - s2.strike).Min();
					int multiplier = int.Parse((string)(((ComboBoxItem)StrikesCount.SelectedItem).Content)) / 2;
					decimal currentBAPirce = _currentBaseActive.LastTrade != null ? _currentBaseActive.LastTrade.Price : 0;
					tmpOptions = tmpOptions
						.Where(opt =>
							(opt.Strike.Value >= (currentBAPirce - (strikeStep * multiplier))) &&
							(opt.Strike <= (currentBAPirce + (strikeStep * multiplier))))
						.ToList();
				}
			}
			tmpOptions = tmpOptions.OrderBy(opt => opt.Strike.Value).ToList();
			Desk.Options = tmpOptions;
			Desk.RefreshOptions();
		}


		private void RegisterSecurities(IEnumerable<Security> securities)
		{
			foreach(Security security in securities)
			{
				if (Connector.RegisteredSecurities.Contains(security))
					continue;
				Connector.RegisterSecurity(security);
				Connector.SubscribeMarketData(security, MarketDataTypes.Level1);
			}
		}

		private void FillExpirationDates()
		{
			ExpirationDates.SelectionChanged -= OptionDeskFiltersChanged;
            ExpirationDates.Items.Clear();
			Connector.Securities
				.Where(opt => opt.Type == SecurityTypes.Option && opt.UnderlyingSecurityId == _currentBaseActive.Id && opt.ExpiryDate.HasValue)
				.Select(opt => opt.ExpiryDate.Value)
				.Distinct()
				.OrderBy(date => date)
				.ForEach(date => ExpirationDates.Items.Add(date.Date));
			if (ExpirationDates.Items.Count > 0)
				ExpirationDates.SelectedIndex = 0;
			ExpirationDates.SelectionChanged += OptionDeskFiltersChanged;
		}

		private void SelectBaseActive(Security security)
		{
			_currentBaseActive = security;
			RegisterSecurities(Enumerable.Repeat(security, 1).Concat(GetOptions(Connector.Securities, security)));
			FillExpirationDates();
			SetOptionsToOptionDesk();
		}

		public void RefreshOptionDesk()
		{
			if (_currentBaseActive != null && _currentBaseActive.LastTrade != null)
			{
				Desk.AssetPrice = _currentBaseActive.LastTrade.Price;
			}
			Desk.CurrentTime = DateTime.Now;
			Desk.RefreshOptions();
		}

		private IEnumerable<Security> GetOptions(IEnumerable<Security> securities, Security baseActive)
		{
			return securities.Where(sec => sec.Type == SecurityTypes.Option && sec.UnderlyingSecurityId == baseActive.Id);
		}

		private void BtnSelectBaseActive_Click(object sender, RoutedEventArgs e)
		{
			if (securityPicker.SelectedSecurity != null)
			{
				SelectBaseActive(securityPicker.SelectedSecurity);
				ExpanderBaseActive.IsExpanded = false;
			}
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void BtnRefreshOptionDesk_Click(object sender, RoutedEventArgs e)
		{
			RefreshOptionDesk();
		}

		private void OptionDeskFiltersChanged(object sender, SelectionChangedEventArgs e)
		{
			SetOptionsToOptionDesk();
		}

		private void AddSecurityToPosition(Security security)
		{
			if (security != _currentBaseActive && security.UnderlyingSecurityId != _currentBaseActive.Id)
				return;
			if (_currentOptionPosition.Where(sec => sec.SecurityCode == security.Code).Any())
				return;
			var x = new SecurityPosition()
			{
				IsActive = true,
				SecurityCode = security.Code,
				Volume = 0,
			};
			_currentOptionPosition.Add(x);
		}

		private void Desk_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			while ((dep != null) && !(dep is DataGridCell))
			{
				dep = VisualTreeHelper.GetParent(dep);
			}
			if (dep == null)
				return;
			DataGridCell cell = null;
			if (dep is DataGridCell)
			{
				cell = dep as DataGridCell;
			}
			while ((dep != null) && !(dep is DataGridRow))
			{
				dep = VisualTreeHelper.GetParent(dep);
			}
			DataGridRow row = dep as DataGridRow;
			Security sec;
			if (cell.Column.DisplayIndex < 11)
			{
				var x = row.Item.GetPropValue("Call");
				sec = (Security)x.GetPropValue("Option");
			}
			else if (cell.Column.DisplayIndex > 14)
			{
				var x = row.Item.GetPropValue("Put");
				sec = (Security)x.GetPropValue("Option");
			}
			else
				return;
			AddSecurityToPosition(sec);
		}

		private void BtnBuildPositionGraph_Click(object sender, RoutedEventArgs e)
		{
			//PosChart.
		}
	}
}
