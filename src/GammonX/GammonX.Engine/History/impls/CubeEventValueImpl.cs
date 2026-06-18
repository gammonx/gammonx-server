using GammonX.Models.Enums;

namespace GammonX.Engine.History
{
    // <inheritdoc />
    public sealed class CubeEventValueImpl : IHistoryEventValue
    {
        private readonly static CubeAction[] _allowedCubeHistoryValue = new[]
        {
            CubeAction.Take,
            CubeAction.Pass,
            CubeAction.Offer
        };

        private readonly CubeAction _cubeAction;

        public CubeEventValueImpl(CubeAction cubeAction)
        {
            if (!_allowedCubeHistoryValue.Contains(cubeAction))
            {
                throw new ArgumentOutOfRangeException(nameof(cubeAction), cubeAction, "Invalid cube action for history event.");
            }

            _cubeAction = cubeAction;
        }

        // <inheritdoc />
        public object GetValue()
        {
            return _cubeAction;
        }

        /// <summary>
		/// Converts the <see cref="GetValue"/> into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
        public override string ToString()
        {
            return _cubeAction.ToString();
        }
    }
}
