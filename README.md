# ZFontConverter

This is a small program that converts old ZDoom fonts to GZDoom's new unicode font format. Supported formats are [FON1](https://zdoom.org/wiki/FON1), [FON2](https://zdoom.org/wiki/FON2), and [BMF](https://bmf.php5.cz).

To convert a font to GZDoom's new unicode font format, extract the font lumps from the WAD or PK3, and then open them in ZFontConverter. Once you have opened a font file, you should see a preview image of all the font characters below the buttons. To export the font, click on "Convert". You may optionally choose where to export the converted font to.

I wrote it in C# to make it easier for people to contribute, because C# apps run on both Windows and Linux so long as no platform-specific APIs are used, and because C# has built-in support for writing PNGs.
