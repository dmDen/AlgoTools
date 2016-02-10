using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionPosition
{
	class SecurityPosition
	{
		/// <summary>
		/// Активна ли позиция
		/// </summary>
		public bool IsActive
		{
			get; set;
		}

		/// <summary>
		/// Тикер опциона
		/// </summary>
		public string OptionCode
		{
			get; set;
		}

		/// <summary>
		/// Объем позиции
		/// </summary>
		public decimal Volume
		{
			get; set;
		}


	}
}
