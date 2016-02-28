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
		OptionPositionClass _optionPosition = new OptionPositionClass();
		private Security Asset
		{
			get { return _optionPosition?.AssetPosition?.Security; }
		}

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
			dataGridPosition.ItemsSource = _optionPosition.OptionsPosition;
			AssetPositionVolume.DataContext = _optionPosition.AssetPosition;
			AssetCode.DataContext = _optionPosition.AssetPosition;
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
			if (Asset == null)
				return;
			var tmpOptions = Connector.Securities
				.Where(opt => opt.Type == SecurityTypes.Option && opt.UnderlyingSecurityId == Asset.Id)
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
					decimal currentBAPirce = Asset.LastTrade != null ? Asset.LastTrade.Price : 0;
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
				.Where(opt => opt.Type == SecurityTypes.Option && opt.UnderlyingSecurityId == Asset.Id && opt.ExpiryDate.HasValue)
				.Select(opt => opt.ExpiryDate.Value)
				.Distinct()
				.OrderBy(date => date)
				.ForEach(date => ExpirationDates.Items.Add(date.Date));
			if (ExpirationDates.Items.Count > 0)
				ExpirationDates.SelectedIndex = 0;
			ExpirationDates.SelectionChanged += OptionDeskFiltersChanged;
		}

		private void SetAsset(Security asset)
		{
			_optionPosition.SetAsset(asset);
			RegisterSecurities(Enumerable.Repeat(asset, 1).Concat(GetOptions(Connector.Securities, asset)));
			FillExpirationDates();
			SetOptionsToOptionDesk();
		}

		public void RefreshOptionDesk()
		{
			if (Asset != null && Asset.LastTrade != null)
			{
				Desk.AssetPrice = Asset.LastTrade.Price;
			}
			Desk.CurrentTime = DateTime.Now;
			Desk.RefreshOptions();
		}

		private IEnumerable<Security> GetOptions(IEnumerable<Security> securities, Security asset)
		{
			return securities.Where(sec => sec.Type == SecurityTypes.Option && sec.UnderlyingSecurityId == asset.Id);
		}

		private void BtnSelectAsset_Click(object sender, RoutedEventArgs e)
		{
			if (securityPicker.SelectedSecurity != null)
			{
				SetAsset(securityPicker.SelectedSecurity);
				ExpanderAsset.IsExpanded = false;
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
			if (security.UnderlyingSecurityId != Asset.Id)
				return;
			if (_optionPosition.OptionsPosition.Where(sec => sec.Security == security).Any())
				return;
			var newPosition = new SecurityPosition(security)
			{
				IsActive = true,
				Volume = 0,
			};
			_optionPosition.OptionsPosition.Add(newPosition);
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
			if (Asset == null)
				return;
			PosChart.MarketDataProvider = Connector;
			PosChart.SecurityProvider = Connector;
			PosChart.AssetPosition = new Position
			{
				Security = Asset,
				CurrentValue = _optionPosition.AssetPosition.Volume,
			};

			PosChart.Positions.Clear();
			var expDate = Asset.ExpiryDate ?? DateTimeOffset.Now;
			if (_optionPosition.OptionsPosition.Count > 0)
			{
				_optionPosition.OptionsPosition
					.Where(sp => sp.IsActive)
					.ForEach((securityPosition) =>
				{
					PosChart.Positions.Add(new Position { Security = securityPosition.Security, CurrentValue = securityPosition.Volume, });
				});
				expDate = _optionPosition.OptionsPosition.Select(pos => pos.Security.ExpiryDate.Value).Min();
				
			}
			decimal lastPrice = Asset.LastTrade?.Price ?? 0;
			PosChart.Refresh(lastPrice, Asset.PriceStep ?? 1, Connector.CurrentTime, expDate);
		}
	}
}
