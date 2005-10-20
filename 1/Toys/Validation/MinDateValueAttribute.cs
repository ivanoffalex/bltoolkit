using System;

namespace Rsdn.Framework.Validation
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class MinDateValueAttribute : MinValueAttribute
	{
		public MinDateValueAttribute(int year, int month, int day)
			: base(new DateTime(year, month, day))
		{
		}

		public MinDateValueAttribute(int year, int month, int day, bool isExclusive)
			: base(new DateTime(year, month, day), isExclusive)
		{
		}

		public override bool IsValid(ValidationContext context)
		{
			if (context.IsNull(context))
				return true;

			DateTime contextValue = Convert.ToDateTime(context.Value);
			DateTime testValue    = (DateTime)GetValue(context);

			return testValue < contextValue || !IsExclusive && testValue == contextValue;
		}
	}
}
