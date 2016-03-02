using Ecng.Xaml;
using StockSharp.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionPosition
{
	class OptionPositionClass
	{
		public event EventHandler PositionChanged = (s, e) => {};
		private SecurityPosition _assetPosition;
		/// <summary>
		/// Позиция по базовому активу
		/// </summary>
		public SecurityPosition AssetPosition
		{
			get { return _assetPosition; }
		}

		private ObservableCollectionEx<SecurityPosition> _optionsPosition = new ObservableCollectionEx<SecurityPosition>();
		/// <summary>
		/// Позиции по опционам
		/// </summary>
		public ObservableCollectionEx<SecurityPosition> OptionsPosition
		{
			get { return _optionsPosition; }
		}

		public OptionPositionClass(SecurityPosition AssetPosition)
		{
			_optionsPosition.CollectionChanged += (s, e) => PositionChanged(this, EventArgs.Empty);
			_optionsPosition.PropertyChanged += (s, e) => PositionChanged(this, EventArgs.Empty);
			_assetPosition = AssetPosition;
			if (_assetPosition != null)
				_assetPosition.PropertyChanged += (s, e) => PositionChanged(this, EventArgs.Empty);
		}

		public void SetAsset(Security asset)
		{
			_assetPosition = new SecurityPosition(asset);
			_assetPosition.PropertyChanged += (s, e) => PositionChanged(this, EventArgs.Empty);
			_optionsPosition.Clear();
		}
	}
}
