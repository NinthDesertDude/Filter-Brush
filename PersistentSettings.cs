using System.Collections.Generic;
using System.Drawing;

namespace BrushFilter
{
    /// <summary>
    /// Represents the settings used in the dialog so they can be stored and
    /// loaded when applying the effect consecutively for convenience.
    /// </summary>
    public class PersistentSettings : PaintDotNet.Effects.EffectConfigToken
    {
        #region Fields
        /// <summary>
        /// The brush's intensity.
        /// </summary>
        public int BrushIntensity
        {
            get;
            set;
        }

        /// <summary>
        /// The active brush index, as chosen from built-in brushes.
        /// </summary>
        public string BrushName
        {
            get;
            set;
        }

        /// <summary>
        /// The brush's orientation in degrees.
        /// </summary>
        public int BrushRotation
        {
            get;
            set;
        }

        /// <summary>
        /// The brush's radius.
        /// </summary>
        public int BrushSize
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the brush rotates with the mouse direction or not.
        /// </summary>
        public bool DoRotateWithMouse
        {
            get;
            set;
        }

        /// <summary>
        /// Whether to save the settings when the effect is applied.
        /// </summary>
        public bool DoSaveSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized maximum effect intensity.
        /// </summary>
        public int RandMaxIntensity
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized maximum brush size.
        /// </summary>
        public int RandMaxSize
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized minimum brush effect intensity.
        /// </summary>
        public int RandMinIntensity
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized minimum brush size.
        /// </summary>
        public int RandMinSize
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized counter-clockwise rotation.
        /// </summary>
        public int RandRotLeft
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized clockwise rotation.
        /// </summary>
        public int RandRotRight
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized horizontal shifting with respect to canvas size.
        /// </summary>
        public int RandHorzShift
        {
            get;
            set;
        }

        /// <summary>
        /// Randomized vertical shifting with respect to canvas size.
        /// </summary>
        public int RandVertShift
        {
            get;
            set;
        }

        /// <summary>
        /// Doesn't apply brush strokes until the mouse is a certain distance
        /// from its last location.
        /// </summary>
        public int MinDrawDistance
        {
            get;
            set;
        }

        /// <summary>
        /// Increments/decrements the size by an amount after each stroke.
        /// </summary>
        public int SizeChange
        {
            get;
            set;
        }

        /// <summary>
        /// Increments/decrements the rotation by an amount after each stroke.
        /// </summary>
        public int RotChange
        {
            get;
            set;
        }

        /// <summary>
        /// Increments/decrements the effect intensity by an amount after each stroke.
        /// </summary>
        public int IntensityChange
        {
            get;
            set;
        }

        /// <summary>
        /// Sets whether to draw horizontal and/or vertical reflections of the
        /// current image.
        /// </summary>
        public int SymmetryMode
        {
            get;
            set;
        }

        /// <summary>
        /// Contains a list of all custom brushes to reload. The dialog will
        /// attempt to read the paths of each brush and add them if possible.
        /// </summary>
        public List<string> CustomBrushLocations
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Calls and sets up dialog settings to be stored.
        /// </summary>
        public PersistentSettings(
            int brushSize,
            string brushName,
            int brushRotation,
            int brushIntensity,
            int randMaxIntensity,
            int randMinIntensity,
            int randMaxSize,
            int randMinSize,
            int randRotLeft,
            int randRotRight,
            int randHorzShift,
            int randVertShift,
            bool doRotateWithMouse,
            int minDrawDistance,
            int sizeChange,
            int rotChange,
            int intensityChange,
            int symmetryMode,
            List<string> customBrushLocations)
            : base()
        {
            BrushSize = brushSize;
            BrushName = brushName;
            BrushRotation = brushRotation;
            BrushIntensity = brushIntensity;
            RandMaxIntensity = randMaxIntensity;
            RandMinIntensity = randMinIntensity;
            RandMaxSize = randMaxSize;
            RandMinSize = randMinSize;
            RandRotLeft = randRotLeft;
            RandRotRight = randRotRight;
            RandHorzShift = randHorzShift;
            RandVertShift = randVertShift;
            DoRotateWithMouse = doRotateWithMouse;
            MinDrawDistance = minDrawDistance;
            SizeChange = sizeChange;
            RotChange = rotChange;
            IntensityChange = intensityChange;
            SymmetryMode = symmetryMode;
            CustomBrushLocations = new List<string>(customBrushLocations);
        }

        /// <summary>
        /// Copies all settings to another token.
        /// </summary>
        protected PersistentSettings(PersistentSettings other)
            : base(other)
        {
            BrushSize = other.BrushSize;
            BrushName = other.BrushName;
            BrushRotation = other.BrushRotation;
            BrushIntensity = other.BrushIntensity;
            RandMaxIntensity = other.RandMaxIntensity;
            RandMinIntensity = other.RandMinIntensity;
            RandMaxSize = other.RandMaxSize;
            RandMinSize = other.RandMinSize;
            RandRotLeft = other.RandRotLeft;
            RandRotRight = other.RandRotRight;
            RandHorzShift = other.RandHorzShift;
            RandVertShift = other.RandVertShift;
            DoRotateWithMouse = other.DoRotateWithMouse;
            MinDrawDistance = other.MinDrawDistance;
            SizeChange = other.SizeChange;
            RotChange = other.RotChange;
            IntensityChange = other.IntensityChange;
            SymmetryMode = other.SymmetryMode;
            CustomBrushLocations = new List<string>(other.CustomBrushLocations);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Copies all settings to another token.
        /// </summary>
        public override object Clone()
        {
            return new PersistentSettings(this);
        }
        #endregion
    }
}