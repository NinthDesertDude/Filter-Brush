using System.Drawing;

namespace BrushFilter
{
    /// <summary>
    /// Stores copies of any number of relevant environment parameters to be
    /// copied to the dialog.
    /// </summary>
    class UserSettings
    {
        #region Fields
        /// <summary>
        /// Stores a copy of the user's primary color to pass to the dialog
        /// when first used.
        /// </summary>
        public static Color UserPrimaryColor
        {
            get;
            set;
        }

        /// <summary>
        /// Stores a copy of the user's secondary color to pass to the dialog
        /// when first used.
        /// </summary>
        public static Color UserSecondaryColor
        {
            get;
            set;
        }

        /// <summary>
        /// Stores a copy of the user's brush width to pass to the dialog
        /// when first used.
        /// </summary>
        public static float UserBrushWidth
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        static UserSettings()
        {
            UserPrimaryColor = Color.Transparent;
            UserSecondaryColor = Color.Transparent;
            UserBrushWidth = 1;
        }
        #endregion
    }
}
