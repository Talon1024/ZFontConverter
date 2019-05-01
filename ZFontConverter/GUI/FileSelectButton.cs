using System;
using System.Windows.Forms;
namespace ZFontConverter.GUI
{
    // Button used to select BMF/FON2 file
    public class FileSelectButton : Button
    {
        public string SelectedFontFile { get; private set; }
        public event EventHandler<string> FontFileSelected;
        OpenFileDialog fileDialog;

        public FileSelectButton()
        {
            fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter =
                "Byte Map Font (*.bmf)|*.bmf|" +
                "FON2 (*.fon2)|*.fon2|" +
                "All files (*.*)|*.*",
                SupportMultiDottedExtensions = true,
            };
            // SelectedFontFile = "";
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            fileDialog.ShowDialog();
            SelectedFontFile = fileDialog.FileName;
            if (SelectedFontFile.Length > 0)
            {
                FontFileSelected?.Invoke(this, SelectedFontFile);
                Console.WriteLine($"Selected file {SelectedFontFile}");
            }
        }
    }
}
