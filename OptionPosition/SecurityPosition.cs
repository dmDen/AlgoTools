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

		private string _securityCode;
		/// <summary>
		/// Тикер инструмента
		/// </summary>
		public string SecurityCode
		{
			get { return _securityCode; }
			set
			{
				if (_securityCode != value)
				{
					_securityCode = value;
					NotifyPropertyChanged();
				}
			}
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

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

	}

	class MyCol : ObservableCollection<SecurityPosition>
	{
		public void TT()
		{
			this.PropertyChanged += MyCol_PropertyChanged;
		}

		private void MyCol_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}
