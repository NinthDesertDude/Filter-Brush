
==============================================================================
Features to add:
==============================================================================

1. At the start of each brush stroke, the filter will be applied to a copy of
the bitmap. Alpha will be zero across the copy, which is visually layered over
the source. Drawing will increase the alpha of the copy based on pixel
intensity. The surface is merged when the stroke ends.

==============================================================================
Todo:
==============================================================================
1.  Keep all the original alpha values of the effect so you can use them to
limit the alpha increase of a brush stroke.

2.  Scale and rotate with a matrix at the same time so rotation can work.

3.  I can't draw brushes near the left or top border because of the mouse offset.

4.  Make scrollbars automatically appear in display canvas area for scrolling
it up/down easier when zoomed in.

5.  Can't zoom into the mouse position yet. Would be nice.

6.  Publish extra brushes.

7.  Help, documentation, and tutorials.

8.  Handle localization.

9.  Presets.

==============================================================================
Code Refactoring:
==============================================================================
1.  look for TODO stuff left over and do it.

2.  Move all the fancy customized code out of the Designer file and into an
appropriate model.

3.  Move the Utils to the non-constructor, non-events methods, or move them
to Utils.

4.  If you're bored, look for alternatives to using the entire
PresentationCore dll just to read input keys.

5.  Find all the vital functionality in the cluttered wasteland of gui code
and extract it. You know, mvc or mvvm or stuff like that.

==============================================================================
Adding New Content:
==============================================================================

1.  To add new embedded brushes, add to Resources.Designer.cs and add to the
list in the form's code-behind constructor method.

2.  To add new controls, add to the form designer. Then, set up MouseEnter for
tooltip and ValueChanged for trackbars. Trackbars with text labels will need
to update the text in ValueChanged. All text and tooltips should be registered
through the Globalization resources file.

3. To add a new persistent setting, add to PersistentSettings class and its
constructors. In form's code-behind, add to InitInitialToken(),
InitTokenFromDialog(), and InitDialogFromToken().