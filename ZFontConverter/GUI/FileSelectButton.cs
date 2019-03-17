using System;
using System.Windows.Forms;
namespace ZFontConverter.GUI
{
    // Button used to select BMF/FON2 file
    public class FileSelectButton : Button
    {
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter =
                "Byte Map Font (*.bmf)|*.bmf|"+
                "FON2 (*.fon2)|*.fon2|"+
                "All files (*.*)|*.*"
            };
            fileDialog.ShowDialog();
            Console.WriteLine($"Selected file {fileDialog.SafeFileName}");
        }
    }
}
