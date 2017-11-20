using PaintDotNet.Effects;
using System.Collections.Generic;

namespace BrushFilter
{
    /// <summary>
    /// Represents the settings used in the dialog so they can be stored and
    /// loaded when applying the effect consecutively for convenience.
    /// </summary>
    public class PersistentSettings : EffectConfigToken
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
        /// A custom effect, if any.
        /// </summary>
        public Effect CustomEffect;

        /// <summary>
        /// The config details of a property-based custom effect.
        /// </summary>
        public PropertyBasedEffectConfigToken CustomEffectTokenP;

        /// <summary>
        /// The config details of a non-property-based custom effect.
        /// </summary>
        public EffectConfigToken CustomEffectToken;

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
        /// Whether affected pixels blend or overwrite original pixels while
        /// drawing.
        /// </summary>
        public bool OverwriteMode
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
        /// Sets the effect to be applied during filter drawing.
        /// </summary>
        public int EffectMode
        {
            get;
            set;
        }

        /// <summary>
        /// The value for the first property of the effect chosen.
        /// </summary>
        public int EffectProperty1
        {
            get;
            set;
        }

        /// <summary>
        /// The value for the second property of the effect chosen.
        /// </summary>
        public int EffectProperty2
        {
            get;
            set;
        }

        /// <summary>
        /// The value for the third property of the effect chosen.
        /// </summary>
        public int EffectProperty3
        {
            get;
            set;
        }

        /// <summary>
        /// The value for the fourth property of the effect chosen.
        /// </summary>
        public int EffectProperty4
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
            bool overwriteMode,
            int symmetryMode,
            int effectMode,
            int valProperty1,
            int valProperty2,
            int valProperty3,
            int valProperty4,
            List<string> customBrushLocations,
            Effect customEffect,
            EffectConfigToken customEffectToken,
            PropertyBasedEffectConfigToken customEffectTokenP)
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
            OverwriteMode = overwriteMode;
            SymmetryMode = symmetryMode;
            EffectMode = effectMode;
            EffectProperty1 = valProperty1;
            EffectProperty2 = valProperty2;
            EffectProperty3 = valProperty3;
            EffectProperty4 = valProperty4;
            CustomBrushLocations = new List<string>(customBrushLocations);
            CustomEffect = customEffect;
            CustomEffectToken = customEffectToken;
            CustomEffectTokenP = customEffectTokenP;
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
            OverwriteMode = other.OverwriteMode;
            SymmetryMode = other.SymmetryMode;
            EffectMode = other.EffectMode;
            EffectProperty1 = other.EffectProperty1;
            EffectProperty2 = other.EffectProperty2;
            EffectProperty3 = other.EffectProperty3;
            EffectProperty4 = other.EffectProperty4;
            CustomBrushLocations = new List<string>(other.CustomBrushLocations);
            CustomEffect = other.CustomEffect;
            CustomEffectToken = other.CustomEffectToken;
            CustomEffectTokenP = other.CustomEffectTokenP;
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