using GammonX.Models.Enums;

namespace GammonX.Engine.History
{
    // <inheritdoc />
    internal sealed class HitEventValueImpl : IHistoryEventValue
    {
        private readonly int _from;
        private readonly int _to;

        public HitEventValueImpl(int from, bool isWhite)
        {
            _from = from;
            _to = isWhite ? BoardPositions.HomeBarWhite : BoardPositions.HomeBarBlack;
        }

        // <inheritdoc />
        public object GetValue()
        {
            return new Tuple<int, int>(_from, _to);
        }

        /// <summary>
		/// Converts the <see cref="GetValue"/> into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
		public override string ToString()
        {
            return $"{Convert(_from)}/{Convert(_to)}";
        }

        private static string Convert(int position)
        {
            if (position == BoardPositions.BearOffWhite || position == BoardPositions.BearOffBlack)
            {
                return "off";
            }
            if (position == BoardPositions.HomeBarWhite || position == BoardPositions.HomeBarBlack)
            {
                return "bar";
            }
            return $"{position}";
        }
    }
}
