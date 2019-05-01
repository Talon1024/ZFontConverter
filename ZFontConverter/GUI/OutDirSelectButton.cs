using System;
using System.Windows.Forms;
namespace ZFontConverter.GUI
{
    // Button used to select BMF/FON2 file
    public class OutDirSelectButton : Button
    {
        public string SelectedDirectory { get; private set; }
        public event EventHandler<string> DirectorySelected;
        FolderBrowserDialog fileDialog;

        public OutDirSelectButton()
        {
            fileDialog = new FolderBrowserDialog
            {
                Description = "The unicode font will be written to the fonts subfolder inside the given directory.",
                ShowNewFolderButton = false
            };
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            fileDialog.ShowDialog();
            SelectedDirectory = fileDialog.SelectedPath;
            if (SelectedDirectory.Length > 0)
            {
                DirectorySelected?.Invoke(this, SelectedDirectory);
                Console.WriteLine($"Selected file {SelectedDirectory}");
            }
        }
    }
}
