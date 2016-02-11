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
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace OptionPosition
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var asset = new Security { Id = "RIU4@FORTS", LastTrade = new Trade { Price = 56000 } };
			var connector = new FakeConnector(new[] { asset });
			var expiryDate = new DateTime(2014, 09, 15);
			Desk.MarketDataProvider = connector;
			Desk.SecurityProvider = connector;
			Desk.CurrentTime = new DateTime(2014, 08, 15);

			Desk.Options = new[]
				{
				CreateStrike(05000, 10, 122, OptionTypes.Call, expiryDate, asset, 100),
				CreateStrike(10000, 10, 110, OptionTypes.Call, expiryDate, asset, 343),
				CreateStrike(15000, 10, 100, OptionTypes.Call, expiryDate, asset, 3454),
				CreateStrike(20000, 78, 85, OptionTypes.Call, expiryDate, asset, null),
				CreateStrike(25000, 32, 65, OptionTypes.Call, expiryDate, asset, 100),
				CreateStrike(30000, 3245, 30, OptionTypes.Call, expiryDate, asset, 55),
				CreateStrike(35000, 3454, 65, OptionTypes.Call, expiryDate, asset, 456),
				CreateStrike(40000, 34, 85, OptionTypes.Call, expiryDate, asset, 4),
				CreateStrike(45000, 3566, 100, OptionTypes.Call, expiryDate, asset, 67),
				CreateStrike(50000, 454, 110, OptionTypes.Call, expiryDate, asset, null),
				CreateStrike(55000, 10, 122, OptionTypes.Call, expiryDate, asset, 334),

				CreateStrike(05000, 10, 122, OptionTypes.Put, expiryDate, asset, 100),
				CreateStrike(10000, 10, 110, OptionTypes.Put, expiryDate, asset, 343),
				CreateStrike(15000, 6788, 100, OptionTypes.Put, expiryDate, asset, 3454),
				CreateStrike(20000, 10, 85, OptionTypes.Put, expiryDate, asset, null),
				CreateStrike(25000, 567, 65, OptionTypes.Put, expiryDate, asset, 100),
				CreateStrike(30000, 4577, 30, OptionTypes.Put, expiryDate, asset, 55),
				CreateStrike(35000, 67835, 65, OptionTypes.Put, expiryDate, asset, 456),
				CreateStrike(40000, 13245, 85, OptionTypes.Put, expiryDate, asset, 4),
				CreateStrike(45000, 10, 100, OptionTypes.Put, expiryDate, asset, 67),
				CreateStrike(50000, 454, 110, OptionTypes.Put, expiryDate, asset, null),
				CreateStrike(55000, 10, 122, OptionTypes.Put, expiryDate, asset, 334)
			};

			Desk.RefreshOptions();
		}

		private static Security CreateStrike(decimal strike, decimal oi, decimal iv, OptionTypes type, DateTime expiryDate, Security asset, decimal? lastTrade)
		{
			var s = new Security
			{
				Code = "RI {0} {1}".Put(type == OptionTypes.Call ? 'C' : 'P', strike),
				Strike = strike,
				OpenInterest = oi,
				ImpliedVolatility = iv,
				HistoricalVolatility = iv,
				OptionType = type,
				ExpiryDate = expiryDate,
				Board = ExchangeBoard.Forts,
				UnderlyingSecurityId = asset.Id,
				LastTrade = lastTrade == null ? null : new Trade { Price = lastTrade.Value },
				Volume = RandomGen.GetInt(10000)
			};

			s.BestBid = new Quote(s, s.StepPrice ?? 1m * RandomGen.GetInt(100), s.VolumeStep ?? 1m * RandomGen.GetInt(100), Sides.Buy);
			s.BestAsk = new Quote(s, s.BestBid.Price.Max(s.StepPrice ?? 1m * RandomGen.GetInt(100)), s.VolumeStep ?? 1m * RandomGen.GetInt(100), Sides.Sell);

			return s;
		}

		private class FakeConnector : Connector, IMarketDataProvider
		{
			private readonly IEnumerable<Security> _securities;

			public FakeConnector(IEnumerable<Security> securities)
			{
				_securities = securities;
			}

			public override IEnumerable<Security> Securities
			{
				get { return _securities; }
			}

			public override DateTimeOffset CurrentTime
			{
				get { return DateTime.Now; }
			}

			object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
			{
				switch (field)
				{
					case Level1Fields.OpenInterest:
						return security.OpenInterest;

					case Level1Fields.ImpliedVolatility:
						return security.ImpliedVolatility;

					case Level1Fields.HistoricalVolatility:
						return security.HistoricalVolatility;

					case Level1Fields.Volume:
						return security.Volume;

					case Level1Fields.LastTradePrice:
						return security.LastTrade == null ? (decimal?)null : security.LastTrade.Price;

					case Level1Fields.LastTradeVolume:
						return security.LastTrade == null ? (decimal?)null : security.LastTrade.Volume;

					case Level1Fields.BestBidPrice:
						return security.BestBid == null ? (decimal?)null : security.BestBid.Price;

					case Level1Fields.BestBidVolume:
						return security.BestBid == null ? (decimal?)null : security.BestBid.Volume;

					case Level1Fields.BestAskPrice:
						return security.BestAsk == null ? (decimal?)null : security.BestAsk.Price;

					case Level1Fields.BestAskVolume:
						return security.BestAsk == null ? (decimal?)null : security.BestAsk.Volume;
				}

				return null;
			}

			IEnumerable<Level1Fields> IMarketDataProvider.GetLevel1Fields(Security security)
			{
				return new[]
				{
					Level1Fields.OpenInterest,
					Level1Fields.ImpliedVolatility,
					Level1Fields.HistoricalVolatility,
					Level1Fields.Volume,
					Level1Fields.LastTradePrice,
					Level1Fields.LastTradeVolume,
					Level1Fields.BestBidPrice,
					Level1Fields.BestAskPrice,
					Level1Fields.BestBidVolume,
					Level1Fields.BestAskVolume
				};
			}
		}
	}
}
