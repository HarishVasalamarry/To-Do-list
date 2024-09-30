using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace TextWidgetApp
{
    public partial class MainWindow : Window
    {
        private bool _isPenMode = false;
        private bool _isUpdatingText = false; // Flag to prevent infinite recursion

        public MainWindow()
        {
            InitializeComponent();

            // Start the application in edit mode
            Loaded += MainWindow_Loaded;
        }

        // Event handler for the Loaded event
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            EnterEditMode();
        }

        // Enable editing on double-click (optional, since we start in edit mode)
        private void TextWidget_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EnterEditMode();
        }

        // Enter Edit Mode
        private void EnterEditMode()
        {
            TextWidget.IsReadOnly = false;
            TextWidget.Focus();

            // Change background to white when in edit mode
            TextWidgetBorder.Background = Brushes.White;

            // Show border and resize handle when editing
            TextWidgetBorder.BorderThickness = new Thickness(3); // Set border thickness to 3 pixels
            ResizeThumb.Visibility = Visibility.Visible;
            GridLinesCanvas.Visibility = Visibility.Visible;

            // Show controls panel when editing
            ControlsPanel.Visibility = Visibility.Visible;

            // Show Done button
            DoneButton.Visibility = Visibility.Visible;

            // Generate horizontal grid lines
            GenerateHorizontalGridLines();
        }

        // Exit Edit Mode
        private void ExitEditMode()
        {
            TextWidget.IsReadOnly = true;

            // Change background to transparent when not in edit mode
            TextWidgetBorder.Background = Brushes.Transparent;

            // Hide border and resize handle when not editing
            TextWidgetBorder.BorderThickness = new Thickness(0);
            ResizeThumb.Visibility = Visibility.Collapsed;
            GridLinesCanvas.Visibility = Visibility.Collapsed;

            // Hide controls panel when not editing
            ControlsPanel.Visibility = Visibility.Collapsed;

            // Hide Done button
            DoneButton.Visibility = Visibility.Collapsed;
        }

        // Done button event handler
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
        }

        // Color Button Click event handler
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the visibility of the color popup
            ColorPopup.IsOpen = true;
        }

        // Color Rectangle MouseLeftButtonDown event handler
        private void ColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rect && rect.Fill is SolidColorBrush brush)
            {
                TextWidget.Foreground = brush;
                ColorPopup.IsOpen = false;
            }
        }

        // Update font size
        private void FontSizeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(FontSizeInput.Text, out double fontSize))
            {
                TextWidget.FontSize = fontSize;
                // Regenerate grid lines when font size changes
                if (GridLinesCanvas.Visibility == Visibility.Visible)
                {
                    GenerateHorizontalGridLines();
                }
            }
        }

        // Update font style
        private void FontStylePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontStylePicker.SelectedItem is ComboBoxItem selectedItem)
            {
                string fontFamilyName = selectedItem.Content.ToString();
                try
                {
                    TextWidget.FontFamily = new FontFamily(fontFamilyName);
                    // Regenerate grid lines when font changes
                    if (GridLinesCanvas.Visibility == Visibility.Visible)
                    {
                        GenerateHorizontalGridLines();
                    }
                }
                catch
                {
                    // Handle invalid font names
                    TextWidget.FontFamily = new FontFamily("Arial");
                }
            }
        }

        // Close button event handler
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Resize Thumb DragDelta event handler
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width + e.HorizontalChange;
            double newHeight = this.Height + e.VerticalChange;

            if (newWidth >= this.MinWidth)
                this.Width = newWidth;

            if (newHeight >= this.MinHeight)
                this.Height = newHeight;

            // Regenerate grid lines after resizing
            if (GridLinesCanvas.Visibility == Visibility.Visible)
            {
                GenerateHorizontalGridLines();
            }
        }

        // Moving the window
        private void TextWidgetBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                return;

            if (e.Source != TextWidget || TextWidget.IsReadOnly)
            {
                // Begin dragging the window
                try
                {
                    this.DragMove();
                }
                catch
                {
                    // Ignore exceptions that may occur if DragMove is called improperly
                }
            }
        }

        // Pen Button Click event handler
        private void PenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPenMode)
            {
                ExitPenMode();
            }
            else
            {
                ExitEditMode(); // Ensure we're not in edit mode
                EnterPenMode();
            }
        }

        // Enter Pen Mode
        private void EnterPenMode()
        {
            _isPenMode = true;

            // Change cursor to pen
            TextWidget.Cursor = Cursors.Pen; // Use Pen cursor
        }

        // Exit Pen Mode
        private void ExitPenMode()
        {
            _isPenMode = false;

            // Reset cursor
            TextWidget.Cursor = Cursors.IBeam;
        }

        // Handle mouse clicks in RichTextBox when in pen mode
        private void TextWidget_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isPenMode)
            {
                // Get mouse position relative to RichTextBox
                Point mousePosition = e.GetPosition(TextWidget);

                // Get the text position at the mouse click
                TextPointer textPointer = TextWidget.GetPositionFromPoint(mousePosition, true);

                if (textPointer != null)
                {
                    // Apply strikethrough to the existing text in the line at the clicked position
                    TextRange lineRange = GetLineRange(textPointer);

                    if (lineRange != null)
                    {
                        // Only apply to existing text
                        TextRange existingTextRange = new TextRange(lineRange.Start, lineRange.End);

                        // Toggle strikethrough
                        TextDecorationCollection decorations = existingTextRange.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;
                        if (decorations != null && decorations.Contains(TextDecorations.Strikethrough[0]))
                        {
                            // Remove strikethrough
                            TextDecorationCollection newDecorations = decorations.Clone();
                            newDecorations.Remove(TextDecorations.Strikethrough[0]);
                            existingTextRange.ApplyPropertyValue(Inline.TextDecorationsProperty, newDecorations);
                        }
                        else
                        {
                            // Add strikethrough
                            existingTextRange.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
                        }
                    }
                }

                e.Handled = true; // Prevent default handling
            }
        }

        // Get the line range at the specified TextPointer
        private TextRange GetLineRange(TextPointer textPointer)
        {
            TextPointer lineStart = textPointer.GetLineStartPosition(0);
            TextPointer lineEnd = textPointer.GetLineStartPosition(1) ?? TextWidget.Document.ContentEnd;

            if (lineStart != null && lineEnd != null)
            {
                return new TextRange(lineStart, lineEnd);
            }

            return null;
        }

        // Generate horizontal grid lines that match the text lines
        private void GenerateHorizontalGridLines()
        {
            GridLinesCanvas.Children.Clear();

            TextPointer pointer = TextWidget.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);

            if (pointer == null)
                return;

            double width = TextWidgetBorder.ActualWidth;

            while (pointer != null && pointer.CompareTo(TextWidget.Document.ContentEnd) < 0)
            {
                Rect charRect = pointer.GetCharacterRect(LogicalDirection.Forward);

                // Adjust for scrolling
                double y = charRect.Top - TextWidget.VerticalOffset;

                Line line = new Line
                {
                    X1 = 0,
                    Y1 = y + charRect.Height,
                    X2 = width,
                    Y2 = y + charRect.Height,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };

                GridLinesCanvas.Children.Add(line);

                // Move to the next line
                TextPointer nextLine = pointer.GetLineStartPosition(1);
                if (nextLine == null || nextLine.CompareTo(pointer) <= 0)
                {
                    break;
                }
                pointer = nextLine;
            }
        }

        // Update grid lines when text changes
        private void TextWidget_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText)
                return;

            if (GridLinesCanvas.Visibility == Visibility.Visible)
            {
                GenerateHorizontalGridLines();
            }

            RemoveStrikethroughFromEmptyLines();
        }

        // Remove strikethrough from empty lines
        private void RemoveStrikethroughFromEmptyLines()
        {
            _isUpdatingText = true;

            FlowDocument document = TextWidget.Document;
            TextPointer pointer = document.ContentStart;

            while (pointer.CompareTo(document.ContentEnd) < 0)
            {
                TextPointer lineStart = pointer.GetLineStartPosition(0);
                TextPointer lineEnd = lineStart.GetLineStartPosition(1) ?? document.ContentEnd;

                if (lineStart != null && lineEnd != null)
                {
                    TextRange lineRange = new TextRange(lineStart, lineEnd);
                    string text = lineRange.Text;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        // Remove strikethrough if the line is empty
                        lineRange.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
                    }
                }

                pointer = lineEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                if (pointer == null)
                    break;
            }

            _isUpdatingText = false;
        }

        // Selection Changed event handler to adjust strikethrough
        private void TextWidget_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // If we are editing, ensure that new text does not inherit strikethrough
            if (!TextWidget.IsReadOnly)
            {
                TextSelection selection = TextWidget.Selection;
                if (selection != null)
                {
                    // Check if the caret is at an empty position or after a deletion
                    if (selection.IsEmpty)
                    {
                        object deco = selection.GetPropertyValue(Inline.TextDecorationsProperty);
                        if (deco != null && deco is TextDecorationCollection decorations && decorations.Contains(TextDecorations.Strikethrough[0]))
                        {
                            // Remove strikethrough from the current selection
                            TextDecorationCollection newDecorations = decorations.Clone();
                            newDecorations.Remove(TextDecorations.Strikethrough[0]);
                            selection.ApplyPropertyValue(Inline.TextDecorationsProperty, newDecorations);
                        }
                    }
                }
            }
        }
    }
}
