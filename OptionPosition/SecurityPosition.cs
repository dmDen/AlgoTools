using StockSharp.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionPosition
{
	class SecurityPosition : INotifyPropertyChanged
	{
		private bool _isActive;
		/// <summary>
		/// Активна ли позиция
		/// </summary>
		public bool IsActive
		{
			get { return _isActive; }
			set
			{
				if (_isActive != value)
				{
					_isActive = value;
					NotifyPropertyChanged();
				}
			}
		}

		private Security _security;
		/// <summary>
		/// Инструмента
		/// </summary>
		public Security Security
		{
			get { return _security; }
		}

		private decimal _volume;
		/// <summary>
		/// Объем позиции
		/// </summary>
		public decimal Volume
		{
			get { return _volume; }
			set
			{
				if (_volume != value)
				{
					_volume = value;
					NotifyPropertyChanged();
				}
			}
		}

		public SecurityPosition(Security security)
		{
			_security = security;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
