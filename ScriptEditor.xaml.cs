using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LCDPix
{
    /// <summary>
    /// Interaction logic for ScriptEditor.xaml
    /// </summary>
    public partial class ScriptEditor : Window
    {
        bool syntaxHighlight = true;
        internal bool FollowScript = false;
        public ScriptEditor(string fileName = "")
        {
            InitializeComponent();
            richTextBox.AcceptsTab = true;
            richTextBox.AppendText("[LCDIPT]\n// Start of the script\n");
            richTextBox.AppendText("\n\n// End of the script\n[END_LCDIPT]\n");
            if(fileName != "")
            {
                filePath = fileName;
                OpenScript();
            }
            SyntaxHiglightCheck.IsChecked = syntaxHighlight;
            FollowScriptCheck.IsChecked = FollowScript;
        }
        internal string filePath = "";
        private void richTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (richTextBox.CanUndo && Title[Title.Length-1] != '*')
                Title += "*";
            else if(Title[Title.Length - 1] == '*' && !richTextBox.CanUndo)
                Title = Title.Remove(Title.Length - 1);
            richTextBox.TextChanged -= richTextBox_TextChanged;
            if(syntaxHighlight)
                ColorText(richTextBox);
            richTextBox.TextChanged += richTextBox_TextChanged;
        }
        static List<string> functions = new List<string>()
        {
            "Draw",
            "Create",
            "Grid",
            "Zoom",
            "Draw",
            "Rectangle",
            "Ellipse",
            "Line",
            "Color",
            "Wait",
            "Undo",
            "Redo",
            "Fill"
        };
        static List<string> specials = new List<string>()
        {
            "LCDIPT",
            "END_LCDIPT",
        };
        static List<string> bools = new List<string>()
        {
            "true",
            "false",
        };
        static void ColorText(RichTextBox richTextBox)
        {
            IEnumerable<TextRange> wordRanges = GetAllWordRanges(richTextBox.Document);
            foreach (TextRange wordRange in wordRanges)
            {
                if (functions.Contains(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Purple);
                    wordRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.DemiBold);
                }
                else if (specials.Contains(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                    wordRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                }
                else if (bools.Contains(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
                    wordRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                }
                else if (IsDigit(wordRange.Text))
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Olive);
                    wordRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.SemiBold);
                }
                else
                {
                    wordRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                    wordRange.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }
            }
        }
        void ClearUndo()
        {
            richTextBox.IsUndoEnabled = false;
            richTextBox.IsUndoEnabled = true;
            if (Title[Title.Length-1] == '*')
                Title = Title.Remove(Title.Length - 1);
        }
        internal void NewScript()
        {
            filePath = "";
            Title = "untitled";
            richTextBox.Document.Blocks.Clear();
            richTextBox.AppendText("[LCDIPT]\n// Start of the script\n");
            richTextBox.AppendText("\n\n// End of the script\n[END_LCDIPT]\n");
            ClearUndo();
            UpdateCounters();
        }
        internal void OpenScript() 
        {
            string[] lines = File.ReadAllLines(filePath);
            Paragraph para = new Paragraph();
            foreach (string line in lines)
            {
                Run run = new Run(line + "\n");
                para.Inlines.Add(run);
            }
            FlowDocument fd = new FlowDocument(para);
            richTextBox.Document = fd;
            Title = $"{System.IO.Path.GetFileName(filePath)} - {filePath} (LCDPix)";
            UpdateCounters();
            ClearUndo();
        }
        void PromptForSave(bool runAfterSave = false)
        {
            if (runAfterSave)
            {
                MessageBoxResult result = MessageBox.Show("Source Must Be Saved\n   OK to Save?", "Save Before Run", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                    return;
             }
            bool? fileResult = false;
            if (!File.Exists(filePath))
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = Title.Split('\\')[Title.Split('\\').Length - 1].Split('.')[0].Replace("*", ""), // Default file name
                    DefaultExt = ".lcdipt", // Default file extension
                    Filter = "LCDPix script |*.lcdipt" // Filter files by extension
                };

                // Show open file dialog box
                fileResult = dialog.ShowDialog();

                // Process open file dialog box results
                if (fileResult == true)
                {
                    // Open document
                    string filename = dialog.FileName;
                    filePath = filename;
                    Title = $"{System.IO.Path.GetFileName(filename)} - {filename} (LCDPix)";
                }
            }
            ClearUndo();
            if ((fileResult == false && runAfterSave == false)|| !File.Exists(filePath))
                return;
            TextRange range;
            FileStream fStream;
            range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            fStream = new FileStream(filePath, FileMode.Create);
            range.Save(fStream, DataFormats.Text);
            fStream.Close();
            if(runAfterSave)
                RunButton_Click(null, null);
        }
        static bool IsDigit(string num)
        {
            int i = 0;
            return int.TryParse(num, out i);
        }
        public static IEnumerable<TextRange> GetAllWordRanges(FlowDocument document)
        {
            //string pattern = @"[^\W\d](\w|[-']{1,2}(?=\w))*";
            string pattern = @"[^\W](\w|[-']{1,2}(?=\w))*";
            TextPointer pointer = document.ContentStart;
            while (pointer != null)
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    MatchCollection matches = Regex.Matches(textRun, pattern,RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        int startIndex = match.Index;
                        int length = match.Length;
                        TextPointer start = pointer.GetPositionAtOffset(startIndex);
                        TextPointer end = start.GetPositionAtOffset(length);
                        yield return new TextRange(start, end);
                    }
                }

                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
        internal void SetSelection(int line)
        {
            // Programmatically change the selection in the RichTextBox.
            Run r = ((Paragraph)richTextBox.Document.Blocks.ElementAt(0)).Inlines.ElementAt(line) as Run;
            TextPointer caretStart = r.ContentStart.GetLineStartPosition(0);
            var nextStart = r.ContentStart.GetLineStartPosition(1);
            richTextBox.Selection.Select(caretStart, nextStart);
        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            if (filePath != "" && !richTextBox.CanUndo && !((MainWindow)this.Owner).ScriptRunning)
            {
                ((MainWindow)this.Owner).StartScript(filePath);
                this.Owner.Activate();
            }
            else if(filePath != "" && !richTextBox.CanUndo && ((MainWindow)this.Owner).ScriptRunning)
            {
                ((MainWindow)this.Owner).StopScript();
                ((MainWindow)this.Owner).StartScript(filePath);
                this.Owner.Activate();
            }
            else
                PromptForSave(true);
        }
        
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Default", // Default file name
                DefaultExt = ".lcdipt", // Default file extension
                Filter = "LCDPix script |*.lcdipt" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                filePath = filename;
                string[] lines = File.ReadAllLines(filename);
                Paragraph para = new Paragraph();
                foreach (string line in lines)
                {
                    Run run = new Run(line + "\n");
                    para.Inlines.Add(run);
                }
                FlowDocument fd = new FlowDocument(para);
                richTextBox.Document = fd;
                Title = $"{System.IO.Path.GetFileName(filename)} - {filename} (LCDPix)";
                UpdateCounters();
                ClearUndo();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                PromptForSave();
            }
            else if(e.Key == Key.F5 && Keyboard.IsKeyDown(Key.LeftShift))
            {
                StopButon_Click(null, null);
            }
            else if(e.Key == Key.N && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                NewScript();
            }
            else if (e.Key == Key.F5)
                RunButton_Click(null, null);
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            NewScript();
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            PromptForSave();
        }
        internal void Unsubscribe()
        {
            richTextBox.TextChanged -= richTextBox_TextChanged;
            if (filePath == "")
                ColorBlack();
            else
            {
                PromptForSave();
                OpenScript();
            }
        }
        internal void Subscribe()
        {
            richTextBox.TextChanged += richTextBox_TextChanged;
            if (syntaxHighlight)
                ColorText(richTextBox);
        }
        private void richTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCounters();
        }
        void UpdateCounters()
        {
            LineCounter.Text = $"Line: {LineNumber()} ";
            CollumnCounter.Text = $"Collumn: {ColumnNumber()}";
        }
        private int LineNumber()
        {
            TextPointer caretLineStart = richTextBox.CaretPosition.GetLineStartPosition(0);
            TextPointer p = richTextBox.Document.ContentStart.GetLineStartPosition(0);
            int currentLineNumber = 0;

            while (true)
            {
                if (caretLineStart.CompareTo(p) < 0)
                {
                    break;
                }
                int result;
                p = p.GetLineStartPosition(1, out result);
                if (result == 0)
                {
                    break;
                }
                currentLineNumber++;
            }
            return currentLineNumber;
        }

        private int ColumnNumber()
        {
            TextPointer caretPos = richTextBox.CaretPosition;
            TextPointer p = richTextBox.CaretPosition.GetLineStartPosition(0);
            int currentColumnNumber = Math.Max(p.GetOffsetToPosition(caretPos) - 1, 0);

            return currentColumnNumber;
        }

        private void SyntaxHiglightCheck_Click(object sender, RoutedEventArgs e)
        {
            SyntaxHiglightCheck.IsChecked = !SyntaxHiglightCheck.IsChecked;
            syntaxHighlight = SyntaxHiglightCheck.IsChecked;
            if (filePath == "")
                ColorBlack();
            else 
            {
                PromptForSave();
                OpenScript();
            }
        }
        void ColorBlack()
        {
            TextRange textRange = new TextRange(
                // TextPointer to the start of content in the RichTextBox.
                richTextBox.Document.ContentStart,
                // TextPointer to the end of content in the RichTextBox.
                richTextBox.Document.ContentEnd
            );
            string tmp = textRange.Text;
            string[] lines = tmp.Split('\r');
            Paragraph para = new Paragraph();
            Run lastRun = new Run();
            foreach (string line in lines)
            {
                Run run = new Run(line);
                para.Inlines.Add(run);
                lastRun = run;
            }
            para.Inlines.Remove(lastRun);
            FlowDocument fd = new FlowDocument(para);
            richTextBox.Document = fd;
            UpdateCounters();
            ClearUndo();
        }

        private void StopButon_Click(object sender, RoutedEventArgs e)
        {
            if (((MainWindow)this.Owner).ScriptRunning)
                ((MainWindow)this.Owner).StopScript();
        }

        private void FollowScriptCheck_Click(object sender, RoutedEventArgs e)
        {
            FollowScriptCheck.IsChecked = !FollowScriptCheck.IsChecked;
            FollowScript = FollowScriptCheck.IsChecked;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainWindow)this.Owner).scriptEditorWin = null;
        }
    }
}
